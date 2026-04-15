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

            Vec3d pos = startPos.Clone();
            Vec3d motion = direction.Clone().Mul(physics.Velocity);

            result.Points.Add(pos.Clone());

            float dt = physics.DeltaTime;

            for (int i = 0; i < physics.MaxSteps; i++)
            {
                int loops = motion.Length() > 0.1 ? 10 : 1;
                float subDt = dt / loops;
                float subDtFactor = 60f * subDt;

                bool hitBlock = false;
                bool hitEntity = false;
                Vec3d hitPos = null;

                for (int s = 0; s < loops; s++)
                {
                    motion.Scale(Math.Pow(physics.AirDragValue, subDt * 33));

                    double gravityStrength = (physics.GravityPerSecond / 60f * subDtFactor);
                    if (physics.UseCOPhysics is false)
                        gravityStrength += Math.Max(0, -0.015f * motion.Y * subDtFactor);
                    motion.Y -= gravityStrength;

                    Vec3d nextPos = new Vec3d(
                        pos.X + motion.X * subDtFactor,
                        pos.Y + motion.Y * subDtFactor,
                        pos.Z + motion.Z * subDtFactor);

                    // Detección de bloques
                    BlockPos bpos = nextPos.AsBlockPos;
                    Block block = capi.World.BlockAccessor.GetBlock(bpos);

                    if (block != null && block.BlockId != 0 && block.CollisionBoxes != null && block.CollisionBoxes.Length > 0)
                    {
                        foreach (var box in block.CollisionBoxes)
                        {
                            Cuboidd worldBox = box.ToDouble().Translate(bpos.X, bpos.Y, bpos.Z);
                            Cuboidd projBox = new Cuboidd(
                                nextPos.X - 0.05, nextPos.Y - 0.05, nextPos.Z - 0.05,
                                nextPos.X + 0.05, nextPos.Y + 0.05, nextPos.Z + 0.05);

                            if (worldBox.IntersectsOrTouches(projBox))
                            {
                                hitBlock = true;
                                hitPos = nextPos.Clone();
                                break;
                            }
                        }
                    }

                    if (hitBlock) break;

                    // Detección de entidades
                    Entity[] nearby = capi.World.GetEntitiesAround(
                        nextPos, 4f, 4f,
                        e => e != player.Entity && e.IsInteractable && e is EntityAgent);

                    if (nearby?.Length > 0)
                    {
                        foreach (Entity entity in nearby)
                        {
                            Cuboidf cb = entity.CollisionBox;
                            Vec3d ePos = entity.Pos.XYZ;

                            Cuboidd worldBox = new Cuboidd(
                                ePos.X + cb.X1,
                                ePos.Y + cb.Y1,
                                ePos.Z + cb.Z1,
                                ePos.X + cb.X2,
                                ePos.Y + cb.Y2,
                                ePos.Z + cb.Z2);

                            Cuboidd projBox = new Cuboidd(
                                nextPos.X - 0.05, nextPos.Y - 0.05, nextPos.Z - 0.05,
                                nextPos.X + 0.05, nextPos.Y + 0.05, nextPos.Z + 0.05);

                            if (worldBox.IntersectsOrTouches(projBox))
                            {
                                hitEntity = true;
                                hitPos = nextPos.Clone();
                                break;
                            }
                        }
                    }

                    if (hitEntity) break;

                    pos = nextPos;
                }

                // Agregar punto después de todos los substeps
                result.Points.Add(pos.Clone());

                if (hitBlock || hitEntity)
                {
                    result.ImpactPoint = hitPos;
                    result.HitEntity = hitEntity;
                    result.Points.Add(hitPos);
                    break;
                }
            }

            return result;
        }
    }
}
