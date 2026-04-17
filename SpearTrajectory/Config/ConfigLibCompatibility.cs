using ConfigLib;
using ImGuiNET;
using SpearTrajectory.Systems;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace SpearTrajectory.Config
{
    public class ConfigLibCompatibility // settings (GUI stuff)
    {
        private static readonly SpearTrajectoryConfig _defaults = new SpearTrajectoryConfig();

        public ConfigLibCompatibility(ICoreClientAPI api)
        {
            api.ModLoader.GetModSystem<ConfigLibModSystem>()
                .RegisterCustomConfig("speartrajectory", (id, buttons) => EditConfig(id, buttons, api));
        }

        private void EditConfig(string id, ControlButtons buttons, ICoreClientAPI api)
        {
            if (buttons.Save)
                ModConfig.WriteConfig(api, SpearTrajectoryConfig.ConfigName, TrajectoryModSystem.Config);
            if (buttons.Defaults)
                TrajectoryModSystem.Config = new SpearTrajectoryConfig(api, null);

            Edit(TrajectoryModSystem.Config, id);
        }

        private void Edit(SpearTrajectoryConfig config, string id)
        {
            
            if (ImGui.Button($"~##trajectoryLine{id}"))
                config.ToggleTrajectoryLine = SpearTrajectoryConfig.DefaultToggleTrajectoryLine;
            ImGui.SameLine();
            bool trajectoryLine = config.ToggleTrajectoryLine;
            ImGui.Checkbox($"Toggle Trajectory Line##{id}", ref trajectoryLine);
            config.ToggleTrajectoryLine = trajectoryLine;

            ImGui.Separator();

            
            
            if (ImGui.Button($"~##trajectoryCircle{id}"))
                config.ToggleTrajectoryCircle = SpearTrajectoryConfig.DefaultToggleTrajectoryCircle; 
            ImGui.SameLine();
            bool trajectoryCircle = config.ToggleTrajectoryCircle;
            ImGui.Checkbox($"Toggle Trajectory Circle##{id}", ref trajectoryCircle);
            config.ToggleTrajectoryCircle = trajectoryCircle;

            ImGui.Separator();

            if (ImGui.Button($"~##aimAssist{id}"))
                config.EnableAimAssist = SpearTrajectoryConfig.DefaultEnableAimAssist;
            ImGui.SameLine();
            bool aimAssist = config.EnableAimAssist;
            ImGui.Checkbox($"Enable Aim Assist Ghost##{id}", ref aimAssist);
            config.EnableAimAssist = aimAssist;

            ImGui.Separator();

            
            
            if (ImGui.Button($"~##entityHitColor{id}"))
                config.EntityHitColor = SpearTrajectoryConfig.DefaultEntityHitColor;
            ImGui.SameLine();
            // Entity collision color
            double[] hitRgb = ColorUtil.Hex2Doubles(config.EntityHitColor ?? "#FF0000");
            Vector3 hitColor = new((float)hitRgb[0], (float)hitRgb[1], (float)hitRgb[2]);
            ImGui.ColorEdit3($"Entity Hit Color##{id}", ref hitColor, ImGuiColorEditFlags.DisplayHex);
            config.EntityHitColor = ColorUtil.Doubles2Hex(new double[] { hitColor.X, hitColor.Y, hitColor.Z });


            
            
            if (ImGui.Button($"~##aimAssistColor{id}"))
                config.AimAssistColor = SpearTrajectoryConfig.DefaultAimAssistColor;
            ImGui.SameLine();
            // Ghost assist color
            double[] assistRgb = ColorUtil.Hex2Doubles(config.AimAssistColor ?? "#FF6600");
            Vector3 assistColor = new((float)assistRgb[0], (float)assistRgb[1], (float)assistRgb[2]);
            ImGui.ColorEdit3($"Aim Assist Ghost Color##{id}", ref assistColor, ImGuiColorEditFlags.DisplayHex);
            config.AimAssistColor = ColorUtil.Doubles2Hex(new double[] { assistColor.X, assistColor.Y, assistColor.Z });
            ImGui.Separator();

            
            
            if (ImGui.Button($"~##searchRadius{id}"))
                config.AimAssistSearchRadius = SpearTrajectoryConfig.DefaultAimAssistSearchRadius;
            ImGui.SameLine();
            // Ghost assist search radius
            float searchRadius = config.AimAssistSearchRadius;
            ImGui.SliderFloat($"Aim Assist Search Radius##{id}", ref searchRadius, 0.5f, 10f);
            config.AimAssistSearchRadius = searchRadius;
            ImGui.Separator();

            
            
            if (ImGui.Button($"~##circleRadius{id}"))
                config.ImpactCircleRadius = SpearTrajectoryConfig.DefaultImpactCircleRadius;
            ImGui.SameLine();
            // Circle radius
            float circleRadius = config.ImpactCircleRadius;
            ImGui.SliderFloat($"Impact Circle Radius##{id}", ref circleRadius, 0.1f, 5f);
            config.ImpactCircleRadius = circleRadius;
            ImGui.Separator();

            
            
            if (ImGui.Button($"~##outlineSize{id}"))
                config.OutlineSize = SpearTrajectoryConfig.DefaultOutlineSize;
            ImGui.SameLine();
            float outlineSize = config.OutlineSize;
            ImGui.SliderFloat($"Outline Size##{id}", ref outlineSize, 0.005f, 0.1f);
            config.OutlineSize = outlineSize;
            


            
            
            if (ImGui.Button($"~##solidLine{id}"))
                config.SolidLine = SpearTrajectoryConfig.DefaultSolidLine;
            ImGui.SameLine();
            bool solidLine = config.SolidLine;
            ImGui.Checkbox($"Solid Line##{id}", ref solidLine);
            config.SolidLine = solidLine;
            ImGui.Separator();

            
            
            if (ImGui.Button($"~##toggleParticle{id}"))
                config.ToggleImpactParticle = SpearTrajectoryConfig.DefaultToggleImpactParticle;
            ImGui.SameLine();
            bool toggleParticle = config.ToggleImpactParticle;
            ImGui.Checkbox($"Toggle Impact Particle##{id}", ref toggleParticle);
            config.ToggleImpactParticle = toggleParticle;


            
            if (ImGui.Button($"~##particleColor{id}"))
                config.ImpactParticleColor = SpearTrajectoryConfig.DefaultImpactParticleColor;
            ImGui.SameLine();
            double[] particleRgb = ColorUtil.Hex2Doubles(config.ImpactParticleColor ?? "#f9e909");
            Vector3 particleColor = new((float)particleRgb[0], (float)particleRgb[1], (float)particleRgb[2]);
            ImGui.ColorEdit3($"Impact Particle Color##{id}", ref particleColor, ImGuiColorEditFlags.DisplayHex);
            config.ImpactParticleColor = ColorUtil.Doubles2Hex(new double[] { particleColor.X, particleColor.Y, particleColor.Z });


            
            
            if (ImGui.Button($"~##particleSize{id}"))
                config.ImpactParticleSize = SpearTrajectoryConfig.DefaultImpactParticleSize;
            ImGui.SameLine();
            float particleSize = config.ImpactParticleSize;
            ImGui.SliderFloat($"Impact Particle Size##{id}", ref particleSize, 0.06f, 0.2f);
            config.ImpactParticleSize = particleSize;
        }
    }
}