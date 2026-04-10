using Vintagestory.Client.NoObf;
using HarmonyLib;
using Vintagestory.GameContent;
//patches the crosshairs, removes it when aiming with spears
namespace SpearTrajectory
{
    [HarmonyPatch]
    public static class PatchDrawAim
    {
        [HarmonyPatch(typeof(SystemRenderAim), "DrawAim")]
        [HarmonyPrefix]
        static bool PrefixDrawAim(ClientMain game)
        {
            var item = game.Player?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Item;
            bool isSpear = item is ItemSpear;
            bool isAiming = game.EntityPlayer?.Attributes?.GetInt("aiming", 0) == 1;

            if (isSpear && isAiming)
                return false;

            return true;
        }
    }
}
