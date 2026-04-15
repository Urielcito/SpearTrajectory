using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Vintagestory.API.Common;

namespace SpearTrajectory.Config
{
    public class SpearTrajectoryConfig : IModConfig //settings (GUI stuff)
    {
        public const string ConfigName = "SpearTrajectoryConfig.json";
        [JsonProperty(Order = 1)]
        public bool ToggleTrajectoryLine { get; set; } = true;
        [JsonProperty(Order = 2)]
        public bool ToggleTrajectoryCircle { get; set; } = true;
        [JsonProperty(Order = 3)]
        public bool EnableAimAssist { get; set; } = true;

        [JsonProperty(Order = 4)]
        public string EntityHitColor { get; set; } = "#FF0000";

        [JsonProperty(Order = 5)]
        public string AimAssistColor { get; set; } = "#FF6600";

        [JsonProperty(Order = 6)]
        public float AimAssistSearchRadius { get; set; } = 2f;

        [JsonProperty(Order = 7)]
        public float ImpactCircleRadius { get; set; } = 0.3f;


        [JsonProperty(Order = 8)]
        public float OutlineSize { get; set; } = 0.02f;

        [JsonProperty(Order = 9)]
        public bool ToggleImpactParticle { get; set; } = true;

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
            ToggleImpactParticle = previous.ToggleImpactParticle;
        }
    }
}