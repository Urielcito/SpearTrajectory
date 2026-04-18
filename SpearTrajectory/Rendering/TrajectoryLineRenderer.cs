using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using SpearTrajectory.Systems;

namespace SpearTrajectory.Rendering
{
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
                int LineGapLength = (TrajectoryModSystem.Config?.SolidLine == true) ? 0 : GapLength;
                int cycle = (i - 1 + dashOffset) % (DashLength + LineGapLength);
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
            Vec3d viewDir,
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
                capi.Render.RenderLine(origin, (float)(a.X + off.X), (float)(a.Y + off.Y), (float)(a.Z + off.Z), (float)(b.X + off.X), (float)(b.Y + off.Y), (float)(b.Z + off.Z), colorBlack);
            }

            capi.Render.RenderLine(origin, (float)a.X, (float)a.Y, (float)a.Z, (float)b.X, (float)b.Y, (float)b.Z, colorWhite);
        }
    }
}