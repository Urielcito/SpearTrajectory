using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Vintagestory.API.Common;

namespace SpearTrajectory.Config
{
    public class SpearTrajectoryConfig : IModConfig //settings (logic stuff)
    {
        public const string ConfigName = "SpearTrajectoryConfig.json";
        public const bool DefaultToggleTrajectoryLine = false;
        public const bool DefaultToggleTrajectoryCircle = true;
        public const bool DefaultEnableAimAssist = true;
        public const string DefaultEntityHitColor = "#FF0000";
        public const string DefaultAimAssistColor = "#FF6600";
        public const float DefaultAimAssistSearchRadius = 2f;
        public const float DefaultImpactCircleRadius = 0.3f;
        public const float DefaultOutlineSize = 0.02f;
        public const bool DefaultSolidLine = false;
        public const bool DefaultToggleImpactParticle = true;
        public const string DefaultImpactParticleColor = "#f9e909";
        public const float DefaultImpactParticleSize = 0.06f;

        [JsonProperty(Order = 1)]
        public bool ToggleTrajectoryLine { get; set; } = DefaultToggleTrajectoryLine;
        [JsonProperty(Order = 2)]
        public bool ToggleTrajectoryCircle { get; set; } = DefaultToggleTrajectoryCircle;
        [JsonProperty(Order = 3)]
        public bool EnableAimAssist { get; set; } = DefaultEnableAimAssist;

        [JsonProperty(Order = 4)]
        public string EntityHitColor { get; set; } = DefaultEntityHitColor;

        [JsonProperty(Order = 5)]
        public string AimAssistColor { get; set; } = DefaultAimAssistColor;

        [JsonProperty(Order = 6)]
        public float AimAssistSearchRadius { get; set; } = DefaultAimAssistSearchRadius;

        [JsonProperty(Order = 7)]
        public float ImpactCircleRadius { get; set; } = DefaultImpactCircleRadius;

        [JsonProperty(Order = 8)]
        public float OutlineSize { get; set; } = DefaultOutlineSize;

        [JsonProperty(Order = 9)]
        public bool SolidLine { get; set; } = DefaultSolidLine;

        [JsonProperty(Order = 10)]
        public bool ToggleImpactParticle { get; set; } = DefaultToggleImpactParticle;

        [JsonProperty(Order = 11)]
        public string ImpactParticleColor { get; set; } = DefaultImpactParticleColor; // chispa

        [JsonProperty(Order = 12)]
        public float ImpactParticleSize { get; set; } = DefaultImpactParticleSize;

        public SpearTrajectoryConfig() { }

        public SpearTrajectoryConfig(ICoreAPI api, SpearTrajectoryConfig previous)
        {
            if (previous == null) return;
            EnableAimAssist = previous.EnableAimAssist;
            EntityHitColor = previous.EntityHitColor;
            AimAssistColor = previous.AimAssistColor;
            AimAssistSearchRadius = previous.AimAssistSearchRadius;
            ImpactCircleRadius = previous.ImpactCircleRadius;
            OutlineSize = previous.OutlineSize;
            SolidLine = previous.SolidLine;
            ToggleImpactParticle = previous.ToggleImpactParticle;
            ImpactParticleColor = previous.ImpactParticleColor;
            ImpactParticleSize = previous.ImpactParticleSize;
        }
    }
}