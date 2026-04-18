using SpearTrajectory.Systems;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SpearTrajectory.Rendering
{
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
}