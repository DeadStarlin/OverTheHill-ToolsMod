using HarmonyLib;
using MelonLoader;

[assembly: MelonInfo(typeof(OverTheHill.UnlimitedTools), "Unlimited Tools", "1.1.0", "DeadStarlin")]
[assembly: MelonGame("Funselektor", "over the hill")]

namespace OverTheHill;

public sealed class UnlimitedTools : MelonMod
{
    internal static MelonPreferences_Entry<bool> UnlimitedUse;
    internal static readonly ToolRanges Ranges = new();

    public override void OnInitializeMelon()
    {
        var category = MelonPreferences.CreateCategory("OTHUnlimitedTools", "Unlimited Tools");
        UnlimitedUse = category.CreateEntry("UnlimitedUse", true, "Unlimited tool use",
            "Keep equipped tools after use. Disable to restore normal consumption.");
        var range = category.CreateEntry("RangeMultiplier", ToolRange.x1, "Tool range multiplier",
            "x1, x1_25 (1.25x), x1_5 (1.5x), x2, x4, x10. Affects winch, repair and tool placement reach.");
        Ranges.SetRange(range.Value);
        range.OnEntryValueChanged.Subscribe((previous, current) => Ranges.SetRange(current));
        category.SaveToFile(false);
        LoggerInstance.Msg($"Unlimited use: {UnlimitedUse.Value}; tool range: {Ranges.Multiplier}x.");
    }

    public override void OnDeinitializeMelon() => Ranges.RestoreAll();
}

// Both consumption requests and the network receiver call this method.
[HarmonyPatch(typeof(Il2Cpp.CarInventory), nameof(Il2Cpp.CarInventory.RPC_ConsumeItem))]
internal static class KeepEquippedTools
{
    private static bool Prefix() => UnlimitedTools.UnlimitedUse?.Value != true;
}

// Scale the backing field: the native game inlines MaxWinchRange in several callers.
[HarmonyPatch(typeof(Il2Cpp.VehicleWinchSystem), "Spawned")]
internal static class WinchRange
{
    private static void Prefix(Il2Cpp.VehicleWinchSystem __instance)
    {
        float original = __instance.maxWinchRange;
        UnlimitedTools.Ranges.Track(__instance.Pointer, scale => __instance.maxWinchRange = original * scale);
    }
}

[HarmonyPatch(typeof(Il2Cpp.VehicleWinchSystem), "OnDestroy")]
internal static class ForgetWinch
{
    private static void Prefix(Il2Cpp.VehicleWinchSystem __instance) =>
        UnlimitedTools.Ranges.Remove(__instance.Pointer);
}

[HarmonyPatch(typeof(Il2Cpp.StateTerrainObjectPlacement), "Open")]
internal static class PlacementRange
{
    private static void Prefix(Il2Cpp.StateTerrainObjectPlacement __instance)
    {
        float distance = __instance.MaxPlacementDistance;
        // Anchor placement copies the scaled winch reach; use its current base to avoid scaling twice.
        if (__instance.PlacementUsesWinchSystem)
            distance = __instance.VehicleWinchSystem.MaxWinchRange / UnlimitedTools.Ranges.Multiplier;
        float height = __instance.MaxPlacementHeightDifference;
        float projection = __instance.CursorProjectionDepth;
        UnlimitedTools.Ranges.Track(__instance.Pointer, scale =>
        {
            __instance.MaxPlacementDistance = distance * scale;
            __instance.MaxPlacementHeightDifference = height * scale;
            __instance.CursorProjectionDepth = projection * scale;
        });
    }
}

[HarmonyPatch(typeof(Il2Cpp.StateTerrainObjectPlacement), "Close")]
internal static class ForgetPlacement
{
    private static void Finalizer(Il2Cpp.StateTerrainObjectPlacement __instance) =>
        UnlimitedTools.Ranges.Remove(__instance.Pointer, restore: true);
}

[HarmonyPatch(typeof(Il2Cpp.StateCarRepair), "Open")]
internal static class RepairSelectionRange
{
    private static void Prefix(Il2Cpp.StateCarRepair __instance)
    {
        float original = __instance.maxRepairRange;
        UnlimitedTools.Ranges.Track(__instance.Pointer, scale => __instance.maxRepairRange = original * scale);
    }
}

[HarmonyPatch(typeof(Il2Cpp.StateCarRepair), "Close")]
internal static class ForgetRepairSelection
{
    private static void Finalizer(Il2Cpp.StateCarRepair __instance) =>
        UnlimitedTools.Ranges.Remove(__instance.Pointer, restore: true);
}
