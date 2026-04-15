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
    //where simulation results are stored
    public class TrajectoryResult
    {
        public List<Vec3d> Points { get; } = new();
        public Vec3d ImpactPoint { get; set; }
        public bool HitEntity { get; set; }
    }
    //draws the trajectory lines
    public static class TrajectoryLineRenderer
    {
        private const int DashLength = 3;
        private const int GapLength = 3;
        private const int SkipPoints = 3;

        public static void Draw(
    ICoreClientAPI capi,
    List<Vec3d> points,
    BlockPos origin,
    Vec3f viewDirection,
    float outlineSize,
    bool hitEntity = false,
    int dashOffset = 0)
        {
            double[] entityRgb = ColorUtil.Hex2Doubles(TrajectoryModSystem.Config?.EntityHitColor ?? "#FF0000");
            int colorWhite = hitEntity
                ? ColorUtil.ToRgba(255, (int)(entityRgb[2] * 255), (int)(entityRgb[1] * 255), (int)(entityRgb[0] * 255))
                : ColorUtil.ToRgba(255, 255, 255, 255);
            int colorBlack = ColorUtil.ToRgba(255, 0, 0, 0);

            Vec3d vd = new Vec3d(viewDirection.X, viewDirection.Y, viewDirection.Z); // ya no se usa para offset

            Vec3d originVec = origin.ToVec3d();

            for (int i = 0; i < points.Count; i++)
            {
                if (i <= SkipPoints) continue;

                int cycle = (i - 1 + dashOffset) % (DashLength + GapLength);
                if (cycle >= DashLength) continue;

                Vec3d a = points[i - 1] - originVec;
                Vec3d b = points[i] - originVec;

                DrawOutlinedLine(capi, origin, a, b, vd, colorWhite, colorBlack, outlineSize);
            }
        }

        private static void DrawOutlinedLine(
    ICoreClientAPI capi,
    BlockPos origin,
    Vec3d a, Vec3d b,
    Vec3d viewDir,        // reemplaza oRight/oUp
    int colorWhite, int colorBlack,
    float outlineSize)
        {
            Vec3d segDir = (b - a).Normalize();
            Vec3d tempUp = Math.Abs(segDir.Y) < 0.99 ? new Vec3d(0, 1, 0) : new Vec3d(1, 0, 0);
            Vec3d oRight = segDir.Cross(tempUp).Normalize().Mul(outlineSize);
            Vec3d oUp = segDir.Cross(oRight).Normalize().Mul(outlineSize);

            Vec3d[] offsets = { oRight, oRight.Clone().Mul(-1), oUp, oUp.Clone().Mul(-1) };
            foreach (Vec3d off in offsets)
            {
                capi.Render.RenderLine(origin,
                    (float)(a.X + off.X), (float)(a.Y + off.Y), (float)(a.Z + off.Z),
                    (float)(b.X + off.X), (float)(b.Y + off.Y), (float)(b.Z + off.Z),
                    colorBlack);
            }

            capi.Render.RenderLine(origin,
                (float)a.X, (float)a.Y, (float)a.Z,
                (float)b.X, (float)b.Y, (float)b.Z,
                colorWhite);
        }
    }

    //draws the impact circle
    public static class ImpactCircleRenderer
    {
        private const int DashLength = 4;
        private const int GapLength = 4;
        private const int Segments = 64;

        public static void Draw(
            ICoreClientAPI capi,
            BlockPos origin,
            Vec3d impactPoint,
            float radius,
            Vec3d eyePos,
            IPlayer player,
            bool hitEntity,
            float angleOffset,
            float outlineSize,
            int opacity)
        {
            Vec3d camPos = player.Entity.Pos.XYZ.AddCopy(0, eyePos.Y, 0);
            Vec3d toImpact = impactPoint.SubCopy(camPos).Normalize();
            Vec3d worldUp = new Vec3d(0, 1, 0);
            Vec3d billRight = toImpact.Cross(worldUp).Normalize();
            Vec3d billUp = billRight.Cross(toImpact).Normalize();

            double[] entityRgb = ColorUtil.Hex2Doubles(TrajectoryModSystem.Config?.EntityHitColor ?? "#FF0000");
            int colorFill = hitEntity
                ? ColorUtil.ToRgba(opacity, (int)(entityRgb[2] * 255), (int)(entityRgb[1] * 255), (int)(entityRgb[0] * 255))
                : ColorUtil.ToRgba(opacity, 255, 255, 255);
            int colorBlack = ColorUtil.ToRgba(opacity, 0, 0, 0);

            float usedAngleOffset = hitEntity ? angleOffset : 0f;

            DrawDottedCircle(capi, origin, impactPoint, radius,
                billUp, billRight, colorFill, colorBlack, outlineSize, usedAngleOffset);

        }

        private static void DrawDottedCircle(
            ICoreClientAPI capi,
            BlockPos origin,
            Vec3d center,
            float radius,
            Vec3d up, Vec3d right,
            int colorFill, int colorBlack,
            float outlineSize,
            float angleOffset)
        {
            Vec3d originVec = origin.ToVec3d();

            for (int i = 1; i <= Segments; i++)
            {
                int cycle = (i - 1) % (DashLength + GapLength);
                if (cycle >= DashLength) continue;

                float angleA = (float)((i - 1) * 2 * Math.PI / Segments) + angleOffset;
                float angleB = (float)(i * 2 * Math.PI / Segments) + angleOffset;

                Vec3d a = center + right * Math.Cos(angleA) * radius + up * Math.Sin(angleA) * radius - originVec;
                Vec3d b = center + right * Math.Cos(angleB) * radius + up * Math.Sin(angleB) * radius - originVec;

                Vec3d tangent = (b - a).Normalize();
                Vec3d towardCenter = (center - originVec - a).Normalize();
                Vec3d outlineOff = tangent.Cross(towardCenter).Normalize().Mul(outlineSize);

                capi.Render.RenderLine(origin,
                    (float)(a.X + outlineOff.X), (float)(a.Y + outlineOff.Y), (float)(a.Z + outlineOff.Z),
                    (float)(b.X + outlineOff.X), (float)(b.Y + outlineOff.Y), (float)(b.Z + outlineOff.Z),
                    colorBlack);
                capi.Render.RenderLine(origin,
                    (float)(a.X - outlineOff.X), (float)(a.Y - outlineOff.Y), (float)(a.Z - outlineOff.Z),
                    (float)(b.X - outlineOff.X), (float)(b.Y - outlineOff.Y), (float)(b.Z - outlineOff.Z),
                    colorBlack);

                capi.Render.RenderLine(origin,
                    (float)a.X, (float)a.Y, (float)a.Z,
                    (float)b.X, (float)b.Y, (float)b.Z,
                    colorFill);
            }
        }
    }

    //main class
    public class TrajectoryRenderer : IRenderer
    {
        private const float CircleSpeed = 1.5f;
        private const float DashSpeed = 8f;

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

            
            var (startPos, dirVec, speed) = PatchAimingData.GetRealProjectileDirection(
    player.Entity as EntityAgent);

            var physics = TrajectoryPhysics.For(activeItem, isCOItem);
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
                ImpactCircleRenderer.Draw(
                    capi, origin, result.ImpactPoint,
                    radius, player.Entity.LocalEyePos, player,
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
            AdvancedParticleProperties props = new AdvancedParticleProperties();
            props.basePos = result.ImpactPoint;
            props.Quantity = NatFloat.createUniform(0, 15);
            props.LifeLength = NatFloat.createUniform(0.2f, 0.05f);
            props.Size = NatFloat.createUniform(0.06f, 0.03f);
            props.GravityEffect = NatFloat.createUniform(0.3f, 0.2f);
            props.Velocity = new NatFloat[]
            {
                NatFloat.createUniform(-0.4f, 0.8f),
                NatFloat.createUniform(0.2f, 0.5f),
                NatFloat.createUniform(-0.4f, 0.8f)
            };
            props.HsvaColor = new NatFloat[]
            {
                NatFloat.createUniform(30, 15),   
                NatFloat.createUniform(220, 35),
                NatFloat.createUniform(255, 0),
                NatFloat.createUniform(220, 35)
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
                dashAnimAccum += deltaTime * DashSpeed;
                if (dashAnimAccum >= 6f)
                    dashAnimAccum -= 6f;
            }
        }

        public void Dispose() { }
    }
}