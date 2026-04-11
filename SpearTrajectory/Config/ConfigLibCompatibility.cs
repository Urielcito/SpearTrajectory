using ConfigLib;
using ImGuiNET;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace SpearTrajectory.Config
{
    public class ConfigLibCompatibility
    {
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
            bool trajectoryLine = config.ToggleTrajectoryLine;
            ImGui.Checkbox($"Toggle Trajectory Line##{id}", ref trajectoryLine);
            config.ToggleTrajectoryLine = trajectoryLine;
            ImGui.Separator();
            bool trajectoryCircle = config.ToggleTrajectoryCircle;
            ImGui.Checkbox($"Toggle Trajectory Circle##{id}", ref trajectoryCircle);
            config.ToggleTrajectoryCircle = trajectoryCircle;
            ImGui.Separator();
            // Toggle aim assist
            bool aimAssist = config.EnableAimAssist;
            ImGui.Checkbox($"Enable Aim Assist Ghost##{id}", ref aimAssist);
            config.EnableAimAssist = aimAssist;

            ImGui.Separator();

            // Color línea/círculo al golpear entidad
            double[] hitRgb = ColorUtil.Hex2Doubles(config.EntityHitColor ?? "#FF0000");
            Vector3 hitColor = new((float)hitRgb[0], (float)hitRgb[1], (float)hitRgb[2]);
            ImGui.ColorEdit3($"Entity Hit Color##{id}", ref hitColor, ImGuiColorEditFlags.DisplayHex);
            config.EntityHitColor = ColorUtil.Doubles2Hex(new double[] { hitColor.X, hitColor.Y, hitColor.Z });

            // Color ghost assist
            double[] assistRgb = ColorUtil.Hex2Doubles(config.AimAssistColor ?? "#FF6600");
            Vector3 assistColor = new((float)assistRgb[0], (float)assistRgb[1], (float)assistRgb[2]);
            ImGui.ColorEdit3($"Aim Assist Ghost Color##{id}", ref assistColor, ImGuiColorEditFlags.DisplayHex);
            config.AimAssistColor = ColorUtil.Doubles2Hex(new double[] { assistColor.X, assistColor.Y, assistColor.Z });

            ImGui.Separator();

            // Radio de búsqueda del ghost assist
            float searchRadius = config.AimAssistSearchRadius;
            ImGui.SliderFloat($"Aim Assist Search Radius##{id}", ref searchRadius, 0.5f, 10f);
            config.AimAssistSearchRadius = searchRadius;

            ImGui.Separator();

            float circleRadius = config.ImpactCircleRadius;
            ImGui.SliderFloat($"Impact Circle Radius##{id}", ref circleRadius, 0.1f, 5f);
            config.ImpactCircleRadius = circleRadius;


        }
    }
}