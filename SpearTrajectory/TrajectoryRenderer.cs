using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SpearTrajectory
{
    //where simulation results are stored
    public class TrajectoryResult
    {
        public List<Vec3d> Points { get; } = new();
        public Vec3d ImpactPoint { get; set; }
        public bool HitEntity { get; set; }
    }

    //phys params
    public class TrajectoryPhysics
    {
        //Spear stats
        public double GravityPerSecond = GlobalConstants.GravityPerSecond * 0.75;
        public double AirDragValue = 1 - (1 - GlobalConstants.AirDragAlways) * 0.25;
        public float Velocity = 1f;

        public float DeltaTime = 1f / 60f; 
        public int MaxSteps = 400;
    }

    public static class TrajectoryCalculator
    {
        public static TrajectoryResult Simulate(
            ICoreClientAPI capi,
            Vec3d startPos,
            Vec3d direction,       
            TrajectoryPhysics physics,
            IPlayer player)
        {
            var result = new TrajectoryResult();
            double velocityOffset = 0; // For fine adjustments between different projectiles
            double posOffsetZ = 0.2;

            Vec3d pos = startPos.Clone();
            
            ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
            Item activeItem = null;
            if (slot?.Itemstack?.Item is not null)
                activeItem = slot?.Itemstack?.Item;

            if(activeItem is not null)
            {
                if (activeItem is not ItemSpear && (activeItem.FirstCodePart(0) is "spear")) // Combat Overhaul Spear Velocity
                    velocityOffset = -0.085;
                if (activeItem.FirstCodePart(0) is "javelin")// Combat Overhaul Javelin Velocity
                    velocityOffset = 0.03;
                if (activeItem is not ItemBow && activeItem.FirstCodePart(0) is "bow")// Combat Overhaul Bow Velocity
                    velocityOffset = 0.85;
                if (activeItem is ItemBow && activeItem.FirstCodePart(0) is "bow")// Vanilla Bow Velocity
                    velocityOffset = 0.35;
            }
            pos.Z -= posOffsetZ;

            Vec3d motion = direction.Clone().Mul(physics.Velocity + velocityOffset);

            result.Points.Add(pos.Clone());

            float dt = physics.DeltaTime;
            float dtFactor = 60f * dt;  

            for (int i = 0; i < physics.MaxSteps; i++)
            {
                double drag = Math.Pow(physics.AirDragValue, dt * 33);
                motion.X *= drag;
                motion.Y *= drag;
                motion.Z *= drag;

                double gravityStrength = physics.GravityPerSecond / 60f * dtFactor;
                motion.Y -= gravityStrength;

                Vec3d nextPos = new Vec3d(
                    pos.X + motion.X * dtFactor,
                    pos.Y + motion.Y * dtFactor,
                    pos.Z + motion.Z * dtFactor);

                // block collision
                BlockPos bpos = nextPos.AsBlockPos;
                Block block = capi.World.BlockAccessor.GetBlock(bpos);

                if (block != null && block.BlockId != 0 && block.CollisionBoxes != null && block.CollisionBoxes.Length > 0)
                {
                    bool solidHit = false;
                    foreach (var box in block.CollisionBoxes)
                    {
                        Cuboidd worldBox = box.ToDouble().Translate(bpos.X, bpos.Y, bpos.Z);

                        Cuboidd projBox = new Cuboidd(
                            nextPos.X - 0.05, nextPos.Y - 0.05, nextPos.Z - 0.05,
                            nextPos.X + 0.05, nextPos.Y + 0.05, nextPos.Z + 0.05);

                        if (worldBox.IntersectsOrTouches(projBox))
                        {
                            solidHit = true;
                            break;
                        }
                    }

                    if (solidHit)
                    {
                        result.ImpactPoint = nextPos.Clone();
                        result.Points.Add(nextPos.Clone());
                        break;
                    }
                }

                // entity collision
                float vertThreshold = 1f;
                Entity[] nearby = capi.World.GetEntitiesAround(
                    nextPos, 0.5f, vertThreshold,
                    e => e != player.Entity && e.IsInteractable && e is EntityAgent);

                if (nearby?.Length > 0)
                {
                    result.ImpactPoint = nextPos.Clone();
                    result.HitEntity = true;
                    result.Points.Add(nextPos.Clone());
                    break;
                }

                pos = nextPos;
                result.Points.Add(pos.Clone());
            }

            return result;
        }
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

            Vec3d vd = new Vec3d(viewDirection.X, viewDirection.Y, viewDirection.Z).Normalize();
            Vec3d tempUp = Math.Abs(vd.Y) < 0.99 ? new Vec3d(0, 1, 0) : new Vec3d(1, 0, 0);
            Vec3d oRight = vd.Cross(tempUp).Normalize().Mul(outlineSize);
            Vec3d oUp = vd.Cross(oRight).Normalize().Mul(outlineSize);

            Vec3d originVec = origin.ToVec3d();
            for (int i = 1; i < points.Count; i++)
            {
                
                if (i <= SkipPoints) continue;

                int cycle = ((i - 1) + dashOffset) % (DashLength + (GapLength));
                if (cycle >= DashLength) continue;

                Vec3d a = points[i - 1] - originVec;
                Vec3d b = points[i] - originVec;

                DrawOutlinedLine(capi, origin, a, b, oRight, oUp, colorWhite, colorBlack);
            }
        }

        private static void DrawOutlinedLine(
            ICoreClientAPI capi,
            BlockPos origin,
            Vec3d a, Vec3d b,
            Vec3d oRight, Vec3d oUp,
            int colorWhite, int colorBlack)
        {
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

                Vec3d a = center + (right * Math.Cos(angleA) * radius) + (up * Math.Sin(angleA) * radius) - originVec;
                Vec3d b = center + (right * Math.Cos(angleB) * radius) + (up * Math.Sin(angleB) * radius) - originVec;

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

        private const float OutlineSize = 0.02f;
        private const float MinRadius = 0.5f;
        private const float MaxRadius = 3.5f;
        private const float CircleSpeed = 1.5f;
        private const float DashSpeed = 8f;   

        public double RenderOrder => 0.5;
        public int RenderRange => 999;

        private readonly ICoreClientAPI capi;
        private readonly TrajectoryPhysics physics = new();
        private float circleAngleOffset = 0f;
        private float dashAnimAccum = 0f;

        public TrajectoryRenderer(ICoreClientAPI capi) => this.capi = capi;

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {

            
            IPlayer player = capi.World.Player;
            if (player?.Entity == null) return;
            if (!capi.Input.MouseButton.Right) return;
            
            ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
            
            if (slot?.Itemstack?.Item?.FirstCodePart(0) is not "spear") //ugly, to-do: reformat ts
            {
                if (slot?.Itemstack?.Item?.FirstCodePart(0) is not "javelin")
                {
                    if (slot?.Itemstack?.Item?.FirstCodePart(0) is not "bow")
                    {
                        if (slot?.Itemstack?.Item?.FirstCodePart(0) is not "stone")
                            return;
                    }
                        
                }
            }
                
                   
            
            
            var (startPos, dirVec, speed) = PatchAimingData.GetRealProjectileDirection(
    player.Entity as EntityAgent);

            Vec3f dir = new Vec3f((float)dirVec.X, (float)dirVec.Y, (float)dirVec.Z);
            physics.Velocity = speed;

            float accuracy = player.Entity.Attributes.GetFloat("aimingAccuracy", 0f);
            float radius = TrajectoryModSystem.Config?.ImpactCircleRadius ?? 0.7f;
            int opacity = 255;

                TrajectoryResult result = TrajectoryCalculator.Simulate(
    capi, startPos, dirVec, physics, player);  // dirVec directamente

            // Para el renderer de líneas necesitás Vec3f del view vector solo para el outline
            Vec3f viewDir = player.Entity.SidedPos.GetViewVector();
            BlockPos origin = startPos.AsBlockPos;

            TrajectoryLineRenderer.Draw(
                capi, result.Points, origin, viewDir,
                OutlineSize, result.HitEntity, -(int)dashAnimAccum);

            AdvanceAnimations(deltaTime, result.HitEntity);

            if (result.ImpactPoint != null)
            {
                ImpactCircleRenderer.Draw(
                    capi, origin, result.ImpactPoint,
                    radius, player.Entity.LocalEyePos, player,
                    result.HitEntity, circleAngleOffset, OutlineSize, opacity);
            }
            // Buscar entidad más cercana al impact point, no al jugador
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

            // Si hay target, calcular y dibujar trayectoria sugerida
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
                        OutlineSize, -(int)dashAnimAccum);
                    
                }
            }
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