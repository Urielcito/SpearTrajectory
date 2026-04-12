using SpearTrajectory.Systems;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;

public class AimingAccuracyBehavior : EntityBehavior
{
    public override string PropertyName() => "speartrajectory:aimingaccuracy";

    private readonly EntityAgent _player;
    private readonly AimingSystem _aimingSystem;
    private readonly List<AccuracyModifier> _modifiers = new();
    private readonly bool _coPresent;
    private bool _isAiming;

    public AimingAccuracyBehavior(Entity entity) : base(entity)
    {
        var capi = entity.Api as ICoreClientAPI;
        _coPresent = TrajectoryModSystem.COBridge?.IsCOPresent == true;

        if (_coPresent) return;

        _player = (EntityAgent)entity;
        _aimingSystem = capi.ModLoader.GetModSystem<TrajectoryModSystem>().aimingSystem;

        _modifiers.Add(new MyMovingAccuracy(_player, _aimingSystem));
        _modifiers.Add(new MyOnHurtAccuracy(_player, _aimingSystem));
    }

    public override void OnGameTick(float deltaTime)
    {
        if (_coPresent) return;

        bool nowAiming = entity.Attributes.GetInt("aiming") > 0;

        if (nowAiming != _isAiming)
        {
            _isAiming = nowAiming;
            if (_isAiming)
            {
                _aimingSystem.StartAim(60f, 0f, 0.0004f, 0);
                foreach (var m in _modifiers) m.BeginAim();
            }
            else
            {
                _aimingSystem.StopAim();
                foreach (var m in _modifiers) m.EndAim();
            }
        }

        if (!_isAiming) return;

        _aimingSystem.DriftMultiplier = 1f;
        _aimingSystem.TwitchMultiplier = 1f;
        foreach (var m in _modifiers) m.Update(deltaTime, _aimingSystem);
        _aimingSystem.Update(deltaTime);
    }

    public override void OnEntityReceiveDamage(DamageSource src, ref float damage)
    {
        if (_coPresent) return;
        if (src.Type == EnumDamageType.Heal) return;
        foreach (var m in _modifiers) m.OnHurt(damage);
    }
}