using HarmonyLib;
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

            // Intentar obtener la dirección real del cursor de CO
            var bridge = TrajectoryModSystem.COBridge;
            if (bridge != null && bridge.IsPresent && bridge.IsAiming())
            {
                Vec3d coDir = bridge.GetTargetVec();

                if (coDir != null)
                {
                    // Usar el mismo origen que CO: LocalEyePos + Pos.XYZ
                    Vec3d coStartPos = entity.Pos.XYZ.Add(0, entity.LocalEyePos.Y, 0);
                    float speed = (float)(0.65 * entity.Stats.GetBlended("bowDrawingStrength"));
                    return (coStartPos, coDir.Normalize(), speed);
                }
            }

            // Fallback vanilla: dirección desde el brazo hacia el punto de mira
            Vec3d aimPoint = eyePos.AheadCopy(500.0, entity.Pos.Pitch, entity.Pos.Yaw);
            Vec3d direction = (aimPoint - startPos).Normalize();
            float vanillaSpeed = (float)(0.65 * entity.Stats.GetBlended("bowDrawingStrength"));

            return (startPos, direction, vanillaSpeed);
        }
    }
}