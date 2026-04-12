using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace SpearTrajectory.Systems
{
    public class AimingSystem
    {
        public float CurrentPitchOffset { get; private set; }  
        public float CurrentYawOffset { get; private set; }  
        public bool IsAiming { get; private set; }

        private readonly ICoreClientAPI _api;
        private readonly NormalizedSimplexNoise _noise;
        private readonly Random _random = new();

        private float _aimDrift = 150f;
        private float _aimTwitch = 40f;
        private float _driftFreq = 0.001f;
        private int _twitchDuration = 300;

        private float _offsetPitch;
        private float _offsetYaw;
        private long _twitchLastChangeMs;
        private long _twitchLastStepMs;
        private float _twitchDirPitch;
        private float _twitchDirYaw;

        public float DriftMultiplier { get; set; } = 1f;
        public float TwitchMultiplier { get; set; } = 1f;

        public AimingSystem(ICoreClientAPI api)
        {
            _api = api;
            _noise = NormalizedSimplexNoise.FromDefaultOctaves(4, 1.0, 0.9, 123L);
        }

        public void StartAim(float aimDrift, float aimTwitch, float driftFreq, int twitchDuration)
        {
            _aimDrift = aimDrift;
            _aimTwitch = aimTwitch;
            _driftFreq = driftFreq;
            _twitchDuration = twitchDuration;
            _offsetPitch = 0;
            _offsetYaw = 0;
            DriftMultiplier = 1f;
            TwitchMultiplier = 1f;
            IsAiming = true;
        }

        public void StopAim()
        {
            IsAiming = false;
            CurrentPitchOffset = 0;
            CurrentYawOffset = 0;
        }

        public void Update(float dt)
        {
            if (!IsAiming) return;

            long now = _api.World.ElapsedMilliseconds;

            float xNoise = ((float)_noise.Noise(now * _driftFreq, 1000f) - 0.5f);
            float yNoise = ((float)_noise.Noise(-1000f, now * _driftFreq) - 0.5f);

            float maxDrift = Math.Max(_aimDrift * 1.1f * DriftMultiplier, 1f);

            _offsetYaw += (xNoise - _offsetYaw / maxDrift) * _aimDrift * DriftMultiplier * dt;
            _offsetPitch += (yNoise - _offsetPitch / maxDrift) * _aimDrift * DriftMultiplier * dt;

            float fovFactor = GameMath.Tan(GameMath.DEG2RAD * 35f);
            float pixelsToRad = fovFactor / (_api.Render.FrameHeight / 2f);
            float rangedAcc = Math.Max(_api.World.Player.Entity.Stats.GetBlended("rangedWeaponsAcc"), 0.001f);
            float difficulty = 1f / rangedAcc;

            CurrentYawOffset = _offsetYaw * difficulty * pixelsToRad;
            CurrentPitchOffset = _offsetPitch * difficulty * pixelsToRad;
        }
    }
}
