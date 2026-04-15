using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using SpearTrajectory.Rendering;
using SpearTrajectory.Physics;
using SpearTrajectory.Systems;

namespace SpearTrajectory.Solver
{
    // Calcula la dirección necesaria para alcanzar un target usando binary search sobre el pitch
    public static class TrajectoryAimSolver
    {
        private const int SearchIterations = 24;     // más = más preciso, más caro
        private const double AcceptableError = 0.4;  // distancia en bloques considerada "hit"

        // Retorna la dirección ajustada, o null si no encontró solución
        public static Vec3d SolveForTarget(
    ICoreClientAPI capi,
    Vec3d startPos,
    Vec3d currentDir,
    Entity target,
    TrajectoryPhysics physics,
    IPlayer player)
        {
            // Centro de la hitbox del target como punto de referencia para el yaw
            Cuboidf cb = target.CollisionBox;
            Vec3d ePos = target.Pos.XYZ;
            Vec3d targetCenter = new Vec3d(
                ePos.X + (cb.X1 + cb.X2) * 0.5,
                ePos.Y + (cb.Y1 + cb.Y2) * 0.5,
                ePos.Z + (cb.Z1 + cb.Z2) * 0.5
            );

            double dx = targetCenter.X - startPos.X;
            double dz = targetCenter.Z - startPos.Z;
            double yaw = Math.Atan2(dx, dz);
            double targetDist = Math.Sqrt(dx * dx + dz * dz);

            double pitchLow = -80.0 * Math.PI / 180.0;
            double pitchHigh = 80.0 * Math.PI / 180.0;
            Vec3d bestDir = null;
            double bestError = double.MaxValue;

            // Hitbox mundial del target, expandida ligeramente para que se sienta justo
            const double HitboxPadding = 0.05;
            Cuboidd worldBox = new Cuboidd(
                ePos.X + cb.X1 - HitboxPadding,
                ePos.Y + cb.Y1 - HitboxPadding,
                ePos.Z + cb.Z1 - HitboxPadding,
                ePos.X + cb.X2 + HitboxPadding,
                ePos.Y + cb.Y2 + HitboxPadding,
                ePos.Z + cb.Z2 + HitboxPadding
            );

            for (int i = 0; i < SearchIterations; i++)
            {
                double pitchMid = (pitchLow + pitchHigh) / 2.0;
                Vec3d testDir = PitchYawToDirection(pitchMid, yaw);

                TrajectoryResult testResult = TrajectoryCalculator.Simulate(
                    capi, startPos, testDir, physics, player);

                if (testResult.ImpactPoint == null) break;

                // Hit directo contra la hitbox: solución encontrada
                if (testResult.HitEntity)
                {
                    Cuboidd projBox = new Cuboidd(
                        testResult.ImpactPoint.X - 0.05, testResult.ImpactPoint.Y - 0.05, testResult.ImpactPoint.Z - 0.05,
                        testResult.ImpactPoint.X + 0.05, testResult.ImpactPoint.Y + 0.05, testResult.ImpactPoint.Z + 0.05
                    );
                    if (worldBox.IntersectsOrTouches(projBox))
                        return testDir; // solución exacta, salimos ya
                }

                // Error: distancia horizontal entre impacto y centro del target
                double errorX = testResult.ImpactPoint.X - targetCenter.X;
                double errorZ = testResult.ImpactPoint.Z - targetCenter.Z;
                double horizDist = Math.Sqrt(errorX * errorX + errorZ * errorZ);

                if (horizDist < bestError)
                {
                    bestError = horizDist;
                    bestDir = testDir;
                }

                if (horizDist < AcceptableError) break;

                // Ajustar pitch: si cayó corto, lanzar más alto
                double impactDist = Math.Sqrt(
                    (testResult.ImpactPoint.X - startPos.X) * (testResult.ImpactPoint.X - startPos.X) +
                    (testResult.ImpactPoint.Z - startPos.Z) * (testResult.ImpactPoint.Z - startPos.Z));

                if (impactDist < targetDist)
                    pitchHigh = pitchMid;
                else
                    pitchLow = pitchMid;
            }

            return bestError < AcceptableError * 4 ? bestDir : null;
        }

        private static Vec3d PitchYawToDirection(double pitch, double yaw)
        {
            // Mismo sistema de coordenadas que VS
            double cosPitch = Math.Cos(pitch);
            return new Vec3d(
                cosPitch * Math.Sin(yaw),
               -Math.Sin(pitch),
                cosPitch * Math.Cos(yaw)
            ).Normalize();
        }
    }

    // Dibuja la trayectoria sugerida con 50% de opacidad
    public static class SuggestedTrajectoryRenderer
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
            int dashOffset = 0)
        {
            double[] assistRgb = ColorUtil.Hex2Doubles(TrajectoryModSystem.Config?.AimAssistColor ?? "#FF6600");
            int colorFill = ColorUtil.ToRgba(50,
                (int)(assistRgb[2] * 255),
                (int)(assistRgb[1] * 255),
                (int)(assistRgb[0] * 255));
            int colorOutline = ColorUtil.ToRgba(50, 0, 0, 0);

            Vec3d vd = new Vec3d(viewDirection.X, viewDirection.Y, viewDirection.Z).Normalize();
            Vec3d tempUp = Math.Abs(vd.Y) < 0.99 ? new Vec3d(0, 1, 0) : new Vec3d(1, 0, 0);
            Vec3d oRight = vd.Cross(tempUp).Normalize().Mul(outlineSize);
            Vec3d oUp = vd.Cross(oRight).Normalize().Mul(outlineSize);

            Vec3d originVec = origin.ToVec3d();

            for (int i = 1; i < points.Count; i++)
            {
                if (i <= SkipPoints) continue;

                int cycle = (i - 1 + dashOffset) % (DashLength + GapLength);
                if (cycle >= DashLength) continue;

                Vec3d a = points[i - 1] - originVec;
                Vec3d b = points[i] - originVec;

                // Outline
                Vec3d[] offsets = { oRight, oRight.Clone().Mul(-1), oUp, oUp.Clone().Mul(-1) };
                foreach (Vec3d off in offsets)
                {
                    capi.Render.RenderLine(origin,
                        (float)(a.X + off.X), (float)(a.Y + off.Y), (float)(a.Z + off.Z),
                        (float)(b.X + off.X), (float)(b.Y + off.Y), (float)(b.Z + off.Z),
                        colorOutline);
                }

                // Línea principal
                capi.Render.RenderLine(origin,
                    (float)a.X, (float)a.Y, (float)a.Z,
                    (float)b.X, (float)b.Y, (float)b.Z,
                    colorFill);
            }
        }
    }
}
