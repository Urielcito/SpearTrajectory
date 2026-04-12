using SpearTrajectory.Rendering;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SpearTrajectory.Physics
{
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
            double posOffsetZ = 0.2;

            Vec3d pos = startPos.Clone();


            pos.Z -= posOffsetZ; //todo: compensate player rotation in world, as it currently shifts the actual landing spot by a bit when the player is near to completing a 360 turn

            Vec3d motion = direction.Clone().Mul(physics.Velocity);

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

                float vertThreshold = 1f;
                Entity[] nearby = capi.World.GetEntitiesAround(
                    nextPos, 1f, vertThreshold,
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
}
