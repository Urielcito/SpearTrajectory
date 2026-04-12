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
            Vec3d currentDir,    // dirección actual del jugador, usada para el yaw
            Entity target,
            TrajectoryPhysics physics,
            IPlayer player)
        {
            Vec3d targetPos = target.Pos.XYZ.AddCopy(0, target.SelectionBox.Y2 * 0.5, 0); // centro del mob

            // Yaw fijo apuntando horizontalmente al target
            double dx = targetPos.X - startPos.X;
            double dz = targetPos.Z - startPos.Z;
            double yaw = Math.Atan2(dx, dz);

            // Binary search sobre el pitch: entre -80° y +80°
            double pitchLow = -80.0 * Math.PI / 180.0;
            double pitchHigh = 80.0 * Math.PI / 180.0;
            Vec3d bestDir = null;
            double bestError = double.MaxValue;

            for (int i = 0; i < SearchIterations; i++)
            {
                double pitchMid = (pitchLow + pitchHigh) / 2.0;
                Vec3d testDir = PitchYawToDirection(pitchMid, yaw);

                TrajectoryResult testResult = TrajectoryCalculator.Simulate(
                    capi, startPos, testDir, physics, player);

                if (testResult.ImpactPoint == null) break;

                // Distancia horizontal entre impacto y target
                double errorX = testResult.ImpactPoint.X - targetPos.X;
                double errorZ = testResult.ImpactPoint.Z - targetPos.Z;
                double horizDist = Math.Sqrt(errorX * errorX + errorZ * errorZ);

                // Distancia total del target al origen del lanzamiento (horizontal)
                double targetDist = Math.Sqrt(dx * dx + dz * dz);
                // El impacto cayó cerca o lejos del target?
                double impactDist = Math.Sqrt(
                    (testResult.ImpactPoint.X - startPos.X) * (testResult.ImpactPoint.X - startPos.X) +
                    (testResult.ImpactPoint.Z - startPos.Z) * (testResult.ImpactPoint.Z - startPos.Z));

                if (horizDist < bestError)
                {
                    bestError = horizDist;
                    bestDir = testDir;
                }

                if (horizDist < AcceptableError) break;

                // Si el impacto quedó corto, subir el pitch (lanzar más alto)
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
