using HarmonyLib;
using SpearTrajectory.Bridge;
using SpearTrajectory.Config;
using SpearTrajectory.Rendering;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace SpearTrajectory.Systems
{
    public class TrajectoryModSystem : ModSystem
    {
        public static SpearTrajectoryConfig Config { get; set; }

        public static CombatOverhaulBridge COBridge { get; private set; }

        public AimingSystem? aimingSystem { get; private set; }
        public static TrajectoryModSystem? Instance { get; private set; }

        private Harmony harmony;

        public override void Start(ICoreAPI api)
        {
            Mod.Logger.Notification("Hello from template mod: " + api.Side);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from template mod server side: "
                + Lang.Get("speartrajectory:hello"));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Instance = this;
            aimingSystem = new AimingSystem(api);
            Config = ModConfig.ReadConfig<SpearTrajectoryConfig>(api, SpearTrajectoryConfig.ConfigName)
                     ?? new SpearTrajectoryConfig(api, null);

            if (api.ModLoader.IsModEnabled("configlib"))
                _ = new ConfigLibCompatibility(api);

            api.Logger.Debug("[ST] Antes de construir bridge");
            COBridge = new CombatOverhaulBridge(api);
            api.Logger.Debug($"[ST] Bridge construido, IsPresent={COBridge.IsPresent}");


            api.Event.RegisterRenderer(
                new TrajectoryRenderer(api),
                EnumRenderStage.AfterFinalComposition);

            harmony = new Harmony(Mod.Info.ModID);
            harmony.PatchAll();

            Mod.Logger.Notification("Hello from template mod client side: "
                + Lang.Get("speartrajectory:hello"));
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(Mod.Info.ModID);
            Instance = null;
        }
    }
}