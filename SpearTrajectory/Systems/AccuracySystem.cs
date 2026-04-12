using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SpearTrajectory.Systems
{
    public abstract class AccuracyModifier
    {
        protected EntityAgent Entity;
        protected AimingSystem AimingSystem;
        protected long AimStartMs;

        protected AccuracyModifier(EntityAgent entity, AimingSystem aimingSystem)
        {
            Entity = entity;
            AimingSystem = aimingSystem;
        }

        public virtual void BeginAim() => AimStartMs = Entity.World.ElapsedMilliseconds;
        public virtual void EndAim() { }
        public virtual void OnHurt(float damage) { }
        public virtual void Update(float dt, AimingSystem system) { }
    }

    public class MyMovingAccuracy : AccuracyModifier
    {
        private float _walkPenalty, _sprintPenalty;

        private float WalkMax = 2f;
        private float SprintMax = 4f;
        private float RiseRate = 4f;
        private float DropRate = 1.5f;
        private float DriftMod = 2f;
        private float TwitchMod = 1.5f;

        public MyMovingAccuracy(EntityAgent entity, AimingSystem system) : base(entity, system) { }

        public override void BeginAim()
        {
            base.BeginAim();
            _walkPenalty = 0f;
            _sprintPenalty = 0f;
        }

        public override void Update(float dt, AimingSystem system)
        {
            _walkPenalty = GameMath.Clamp(
                Entity.Controls.TriesToMove
                    ? _walkPenalty + dt * RiseRate
                    : _walkPenalty - dt * DropRate,
                0, WalkMax);

            _sprintPenalty = GameMath.Clamp(
                Entity.Controls.TriesToMove && Entity.Controls.Sprint
                    ? _sprintPenalty + dt * RiseRate
                    : _sprintPenalty - dt * DropRate,
                0, SprintMax);

            float total = _walkPenalty + _sprintPenalty;
            system.DriftMultiplier += total * DriftMod;
            system.TwitchMultiplier += total * TwitchMod;
        }
    }

    public class MyOnHurtAccuracy : AccuracyModifier
    {
        private float _penalty;

        public MyOnHurtAccuracy(EntityAgent entity, AimingSystem system) : base(entity, system) { }

        public override void Update(float dt, AimingSystem system)
        {
            _penalty = GameMath.Clamp(_penalty - dt / 3f, 0, 0.4f);
            system.DriftMultiplier += _penalty * 3f;
            system.TwitchMultiplier += _penalty * 5f;
        }

        public override void OnHurt(float damage)
        {
            if (damage > 3) _penalty = 0.4f;
        }
    }
}
