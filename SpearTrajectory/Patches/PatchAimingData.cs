using HarmonyLib;
using SpearTrajectory.Systems;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SpearTrajectory.Patches
{
    [HarmonyPatch]
    public static class PatchAimingData
    {
        [HarmonyPatch(typeof(EntityBehaviorAimingAccuracy), "OnAimingChanged")]
        [HarmonyPostfix]
        static void PostfixOnAimingChanged(EntityBehaviorAimingAccuracy __instance) { }

        public static (Vec3d startPos, Vec3d direction, float speed) GetRealProjectileDirection(
    EntityAgent entity)
        {
            const double heightOffset = 0.0;
            const double horizontalOffset = 0.3;
            const double behindDistance = 0.15;

            Vec3d eyePos = entity.Pos.XYZ.Add(0, entity.LocalEyePos.Y, 0);
            Vec3d startPos = entity.Pos.BehindCopy(behindDistance).XYZ.Add(
                entity.LocalEyePos.X - GameMath.Cos(entity.Pos.Yaw) * horizontalOffset,
                entity.LocalEyePos.Y + heightOffset,
                entity.LocalEyePos.Z + GameMath.Sin(entity.Pos.Yaw) * horizontalOffset);

            var bridge = TrajectoryModSystem.COBridge;
            if (bridge != null && bridge.IsPresent && bridge.IsAiming())
            {
                Vec3d coDir = bridge.GetTargetVec();
                if (coDir != null)
                {
                    Vec3d coStartPos = entity.Pos.XYZ.Add(0, entity.LocalEyePos.Y, 0);
                    float speed = (float)(0.65 * entity.Stats.GetBlended("bowDrawingStrength"));
                    return (coStartPos, ApplyDispersion(entity, coDir.Normalize()), speed);
                }
            }

            Vec3d aimPoint = eyePos.AheadCopy(500.0, entity.Pos.Pitch, entity.Pos.Yaw);
            Vec3d direction = ApplyDispersion(entity, (aimPoint - startPos).Normalize());
            float vanillaSpeed = (float)(0.65 * entity.Stats.GetBlended("bowDrawingStrength"));
            return (startPos, direction, vanillaSpeed);
        }

        private static Vec3d ApplyDispersion(EntityAgent entity, Vec3d baseDir)
        {
            double pitchOffset, yawOffset;
            if (TrajectoryModSystem.COBridge?.IsCOPresent == true)
                return baseDir;

            var aimSys = TrajectoryModSystem.Instance?.aimingSystem;
            if (aimSys != null && aimSys.IsAiming)
            {
                pitchOffset = aimSys.CurrentPitchOffset;
                yawOffset = aimSys.CurrentYawOffset;
            }
            else
            {
                // Fallback vanilla
                float accuracy = entity.Attributes.GetFloat("aimingAccuracy", 0f);
                float dispersion = Math.Max(0.001f, 1f - accuracy);
                pitchOffset = entity.WatchedAttributes.GetDouble("aimingRandPitch", 1) * dispersion * 0.75;
                yawOffset = entity.WatchedAttributes.GetDouble("aimingRandYaw", 1) * dispersion * 0.75;
            }

            double currentPitch = Math.Atan2(-baseDir.Y,
                Math.Sqrt(baseDir.X * baseDir.X + baseDir.Z * baseDir.Z));
            double currentYaw = Math.Atan2(baseDir.X, baseDir.Z);

            double newPitch = currentPitch + pitchOffset;
            double newYaw = currentYaw + yawOffset;
            double cosPitch = Math.Cos(newPitch);

            return new Vec3d(
                cosPitch * Math.Sin(newYaw),
               -Math.Sin(newPitch),
                cosPitch * Math.Cos(newYaw)
            ).Normalize();
        }
    }
}