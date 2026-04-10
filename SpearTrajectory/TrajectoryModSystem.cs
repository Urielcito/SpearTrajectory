using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace SpearTrajectory
{
    public class TrajectoryModSystem : ModSystem
    {

        private Harmony? harmony;
        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            Mod.Logger.Notification("Hello from template mod: " + api.Side);
            
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("trajectory:hello"));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("trajectory:hello"));
            api.Event.RegisterRenderer(new TrajectoryRenderer(api), EnumRenderStage.AfterFinalComposition);
            harmony = new Harmony(Mod.Info.ModID);
            harmony.PatchAll();
        }

        public override void Dispose()
        {
                harmony?.UnpatchAll(Mod.Info.ModID);
        }

    }
}
