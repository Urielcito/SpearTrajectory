using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

[HarmonyPatch]
public static class PatchAimingAccuracy
{
    private static bool IsHoldingSpear(AccuracyModifier instance)
    {
        var entityField = typeof(AccuracyModifier)
            .GetField("entity", System.Reflection.BindingFlags.NonPublic
                              | System.Reflection.BindingFlags.Instance);
        var entity = entityField?.GetValue(instance) as EntityAgent;
        return entity?.RightHandItemSlot?.Itemstack?.Item is ItemSpear;
    }

    [HarmonyPatch(typeof(BaseAimingAccuracy), "Update")]
    [HarmonyPostfix]
    static void PostfixBase(AccuracyModifier __instance, float dt, ref float accuracy)
    {
        if (IsHoldingSpear(__instance)) accuracy = 1f;
    }

    [HarmonyPatch(typeof(MovingAimingAccuracy), "Update")]
    [HarmonyPostfix]
    static void PostfixMoving(AccuracyModifier __instance, float dt, ref float accuracy)
    {
        if (IsHoldingSpear(__instance)) accuracy = 1f;
    }

    [HarmonyPatch(typeof(SprintAimingAccuracy), "Update")]
    [HarmonyPostfix]
    static void PostfixSprint(AccuracyModifier __instance, float dt, ref float accuracy)
    {
        if (IsHoldingSpear(__instance)) accuracy = 1f;
    }

    [HarmonyPatch(typeof(OnHurtAimingAccuracy), "Update")]
    [HarmonyPostfix]
    static void PostfixOnHurt(AccuracyModifier __instance, float dt, ref float accuracy)
    {
        if (IsHoldingSpear(__instance)) accuracy = 1f;
    }
}