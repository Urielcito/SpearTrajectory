using HarmonyLib;
using System.Reflection;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
//Patches the reticle, removes it when holding spears
namespace SpearTrajectory
{
    [HarmonyPatch(typeof(SystemRenderPlayerAimAcc), "OnRenderFrame2DOverlay")]
    public static class PatchDrawReticle
    {
        static FieldInfo gameField = typeof(SystemRenderPlayerAimAcc)
            .BaseType
            .GetField("game", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPrefix]
        static bool Prefix(SystemRenderPlayerAimAcc __instance)
        {
            var game = gameField.GetValue(__instance);

            var player = game?.GetType().GetProperty("Player")?.GetValue(game);
            var invManager = player?.GetType().GetProperty("InventoryManager")?.GetValue(player);
            var slot = invManager?.GetType().GetProperty("ActiveHotbarSlot")?.GetValue(invManager);
            var stack = slot?.GetType().GetProperty("Itemstack")?.GetValue(slot);
            var item = stack?.GetType().GetProperty("Item")?.GetValue(stack);

            var entityPlayer = game?.GetType().GetProperty("EntityPlayer")?.GetValue(game);
            var attributes = entityPlayer?.GetType().GetProperty("Attributes")?.GetValue(entityPlayer);
            var getIntMethod = attributes?.GetType().GetMethod("GetInt", new[] { typeof(string), typeof(int) });
            int aimingValue = 0;
            if (getIntMethod != null && attributes != null)
            {
                aimingValue = (int)getIntMethod.Invoke(attributes, new object[] { "aiming", 0 });
            }
            bool isAiming = aimingValue == 1;
            bool isSpear = item is ItemSpear;
            if (isSpear)
                return false;

            return true;
        }
    }
}