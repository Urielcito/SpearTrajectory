using SpearTrajectory.Patches;
using SpearTrajectory.Solver;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using SpearTrajectory.Physics;
using SpearTrajectory.Systems;

namespace SpearTrajectory.Rendering
{
    public class TrajectoryRenderer : IRenderer
    {
        private const float CircleSpeed = 3f;
        private const float DashSpeed = 8f;
        private float _circlePulseAccum = 5f;
        private const float PulseSpeed = 5f;
        private const float PulseAmount = 0.25f; //25%

        public double RenderOrder => 0.5;
        public int RenderRange => 999;

        private readonly ICoreClientAPI capi;
        private float circleAngleOffset = 0f;
        private float dashAnimAccum = 0f;

        public TrajectoryRenderer(ICoreClientAPI capi) => this.capi = capi;

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            IPlayer player = capi.World.Player;
            if (player?.Entity == null) return;

            ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
            Item activeItem = slot?.Itemstack?.Item;

            var bridge = TrajectoryModSystem.COBridge;
            bool isCOItem = bridge != null && bridge.IsCOItem(activeItem);
            float distanceFactor = 0f;
            if (bridge != null)
                distanceFactor = bridge.GetSpearsThrownDistance();
            if (isCOItem)
            {
                //to only draw when aiming is possible through stances (damn you spear)
                if (!bridge.IsAiming()) return;
            }
            else
            {
                // vanilla
                if (!capi.Input.MouseButton.Right) return;
                string code = activeItem?.FirstCodePart(0);
                if (code is not "spear" and not "javelin" and not "bow" and not "stone") //nifty right
                    return;
            }
            if (bridge != null && bridge.IsReticleVisible())
                bridge.SetReticleVisible(false);
            var (startPos, dirVec, speed) = PatchAimingData.GetRealProjectileDirection(
                player.Entity as EntityAgent);

            var physics = TrajectoryPhysics.For(activeItem, isCOItem, distanceFactor);
            float outlineSize = TrajectoryModSystem.Config?.OutlineSize ?? 0.02f;
            float radius = TrajectoryModSystem.Config?.ImpactCircleRadius ?? 0.7f;
            int opacity = 255;

            TrajectoryResult result = TrajectoryCalculator.Simulate(
                capi, startPos, dirVec, physics, player);

            Vec3f viewDir = player.Entity.SidedPos.GetViewVector();

            BlockPos origin = startPos.AsBlockPos;

            if (TrajectoryModSystem.Config?.ToggleTrajectoryLine == true)
            {
                TrajectoryLineRenderer.Draw(
                    capi, result.Points, origin, viewDir,
                    outlineSize, result.HitEntity, -(int)dashAnimAccum);
            }

            AdvanceAnimations(deltaTime, result.HitEntity);

            if (result.ImpactPoint != null && TrajectoryModSystem.Config?.ToggleTrajectoryCircle == true)
            {
                float pulseMultiplier = (1f + PulseAmount * (float)Math.Sin(_circlePulseAccum) * 2f) * 2f;
                float pulsedRadius = radius;
                if (result.HitEntity)
                    pulsedRadius *= pulseMultiplier;
                ImpactCircleRenderer.Draw(
                    capi, origin, result.ImpactPoint,
                    pulsedRadius, player.Entity.LocalEyePos, player,
                    result.HitEntity, circleAngleOffset, outlineSize, opacity);
            }
            if (result.ImpactPoint != null && TrajectoryModSystem.Config?.ToggleImpactParticle == true)
            {
                SpawnImpactParticle(result);
            }

            Entity nearestTarget = null;

            if (result.ImpactPoint != null)
            {
                double nearestDist = double.MaxValue;
                float searchRadius = TrajectoryModSystem.Config?.AimAssistSearchRadius ?? 2f;
                Entity[] candidates = capi.World.GetEntitiesAround(
                    result.ImpactPoint, searchRadius, searchRadius,
                    e => e != player.Entity && e.IsInteractable && e is EntityAgent);

                if (candidates != null)
                {
                    foreach (Entity e in candidates)
                    {
                        double dist = result.ImpactPoint.SquareDistanceTo(e.Pos.XYZ);
                        if (dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearestTarget = e;
                        }
                    }
                }
            }

            // ghost assist
            if (nearestTarget != null && TrajectoryModSystem.Config?.EnableAimAssist == true)
            {
                Vec3d solvedDir = TrajectoryAimSolver.SolveForTarget(
                    capi, startPos, dirVec, nearestTarget, physics, player);

                if (solvedDir != null)
                {
                    TrajectoryResult suggestedResult = TrajectoryCalculator.Simulate(
                        capi, startPos, solvedDir, physics, player);

                    SuggestedTrajectoryRenderer.Draw(
                        capi, suggestedResult.Points, origin, viewDir,
                        outlineSize, -(int)dashAnimAccum);
                }
            }
        }

        private void SpawnImpactParticle(TrajectoryResult result)
        {
            var cfg = TrajectoryModSystem.Config;
            double[] rgb = ColorUtil.Hex2Doubles(cfg.ImpactParticleColor ?? "#f9e909");
            int[] hsv = ColorUtil.RgbToHsvInts(
                (int)(rgb[0] * 255),
                (int)(rgb[1] * 255),
                (int)(rgb[2] * 255)
            );

            AdvancedParticleProperties props = new AdvancedParticleProperties();
            props.basePos = result.ImpactPoint;
            props.Quantity = NatFloat.createUniform(0, 15);
            props.LifeLength = NatFloat.createUniform(0.2f, 0.05f);
            props.Size = NatFloat.createUniform(cfg.ImpactParticleSize, 0.03f);
            props.GravityEffect = NatFloat.createUniform(0.3f, 0.2f);
            props.Velocity = new NatFloat[]
            {
                NatFloat.createUniform(-0.4f, 0.8f),
                NatFloat.createUniform(0.2f, 0.5f),
                NatFloat.createUniform(-0.4f, 0.8f)
            };
            props.HsvaColor = new NatFloat[]
            {
                NatFloat.createUniform(hsv[0], 15f),  // H
                NatFloat.createUniform(hsv[1], 35f),  // S
                NatFloat.createUniform(hsv[2], 0f),   // V
                NatFloat.createUniform(220f, 35f)     // A fijo
            };
            props.VertexFlags = 128;
            props.ParticleModel = EnumParticleModel.Quad;
            props.TerrainCollision = false;

            capi.World.SpawnParticles(props);
        }

        private void AdvanceAnimations(float deltaTime, bool hitEntity)
        {
            circleAngleOffset += deltaTime * CircleSpeed;
            if (circleAngleOffset > Math.PI * 2)
                circleAngleOffset -= (float)(Math.PI * 2);

            if (hitEntity)
            {
                _circlePulseAccum += deltaTime * PulseSpeed;
                if (_circlePulseAccum > Math.PI * 4)
                    _circlePulseAccum -= (float)(Math.PI * 4);
                dashAnimAccum += deltaTime * DashSpeed;
                if (dashAnimAccum >= 6f)
                    dashAnimAccum -= 6f;
            }
        }

        public void Dispose() { }
    }
}