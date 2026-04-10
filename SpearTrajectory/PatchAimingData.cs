using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SpearTrajectory
{
    [HarmonyPatch]
    public static class PatchAimingData
    {
        [HarmonyPatch(typeof(EntityBehaviorAimingAccuracy), "OnAimingChanged")]
        [HarmonyPostfix]
        static void PostfixOnAimingChanged(EntityBehaviorAimingAccuracy __instance)
        {
            // WatchedAttributes ya sincronizados, no necesitamos hacer nada aquí
            // El postfix existe por si en el futuro querés cachear o notificar
        }

        public static (Vec3d startPos, Vec3d direction, float speed) GetRealProjectileDirection(
    EntityAgent entity)
        {
            const double heightOffset = 0.0;
            const double horizontalOffset = 0.3;
            const double behindDistance = 0.15;

            // Punto de mira — desde el ojo, hacia adelante
            Vec3d eyePos = entity.Pos.XYZ.Add(0, entity.LocalEyePos.Y, 0);
            Vec3d aimPoint = eyePos.AheadCopy(100.0, entity.Pos.Pitch, entity.Pos.Yaw);

            // Posición real de spawn de la lanza (brazo derecho)
            Vec3d startPos = entity.Pos.BehindCopy(behindDistance).XYZ.Add(
                entity.LocalEyePos.X - GameMath.Cos(entity.Pos.Yaw) * horizontalOffset,
                entity.LocalEyePos.Y + heightOffset,
                entity.LocalEyePos.Z + GameMath.Sin(entity.Pos.Yaw) * horizontalOffset);

            // Dirección desde el brazo hacia el punto de mira
            Vec3d direction = (aimPoint - startPos).Normalize();

            float speed = (float)(0.65 * entity.Stats.GetBlended("bowDrawingStrength"));

            return (startPos, direction, speed);
        }
    }
}