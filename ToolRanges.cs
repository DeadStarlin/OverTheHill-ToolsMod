using System;
using System.Collections.Generic;

namespace OverTheHill;

public enum ToolRange
{
    x1 = 100,
    x1_25 = 125,
    x1_5 = 150,
    x2 = 200,
    x4 = 400,
    x10 = 1000
}

internal sealed class ToolRanges
{
    private readonly Dictionary<IntPtr, Action<float>> targets = new();
    internal float Multiplier { get; private set; } = 1f;

    internal static float GetMultiplier(ToolRange range) => range switch
    {
        ToolRange.x1 => 1f,
        ToolRange.x1_25 => 1.25f,
        ToolRange.x1_5 => 1.5f,
        ToolRange.x2 => 2f,
        ToolRange.x4 => 4f,
        ToolRange.x10 => 10f,
        _ => 1f
    };

    internal void SetRange(ToolRange range)
    {
        Multiplier = GetMultiplier(range);
        foreach (var apply in targets.Values)
            apply(Multiplier);
    }

    internal void Track(IntPtr pointer, Action<float> apply)
    {
        if (targets.ContainsKey(pointer)) return;
        targets.Add(pointer, apply);
        apply(Multiplier);
    }

    internal void Remove(IntPtr pointer, bool restore = false)
    {
        if (targets.Remove(pointer, out var apply) && restore)
            apply(1f);
    }

    internal void RestoreAll()
    {
        foreach (var apply in targets.Values)
            apply(1f);
        targets.Clear();
    }
}
