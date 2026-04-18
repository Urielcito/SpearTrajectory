using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace SpearTrajectory.Rendering
{
    public class TrajectoryResult
    {
        public List<Vec3d> Points { get; } = new();
        public Vec3d ImpactPoint { get; set; }
        public bool HitEntity { get; set; }
    }
}