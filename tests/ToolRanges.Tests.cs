using System;
using OverTheHill;

static void Equal(float expected, float actual)
{
    if (Math.Abs(expected - actual) > 0.0001f)
        throw new Exception($"Expected {expected}, got {actual}.");
}

var presets = new[] { ToolRange.x1, ToolRange.x1_25, ToolRange.x1_5, ToolRange.x2, ToolRange.x4, ToolRange.x10 };
var factors = new[] { 1f, 1.25f, 1.5f, 2f, 4f, 10f };
var ranges = new ToolRanges();
float winch = 20f, placement = 7.5f;
ranges.Track(new IntPtr(1), scale => winch = 20f * scale);
for (int i = 0; i < presets.Length; i++)
{
    ranges.SetRange(presets[i]);
    Equal(factors[i], ranges.Multiplier);
    Equal(20f * factors[i], winch);
}

// An object opened/spawned again must retain its first baseline.
ranges.Track(new IntPtr(1), scale => winch = 200f * scale);
ranges.SetRange(ToolRange.x2);
Equal(40f, winch);
ranges.Track(new IntPtr(2), scale => placement = 7.5f * scale);
Equal(15f, placement);
ranges.SetRange(ToolRange.x10);
Equal(200f, winch);
Equal(75f, placement);
ranges.SetRange(ToolRange.x1);
Equal(20f, winch);
Equal(7.5f, placement);

// Closing and reopening a placement state must not compound the multiplier.
ranges.SetRange(ToolRange.x4);
ranges.Remove(new IntPtr(2), restore: true);
Equal(7.5f, placement);
float baseline = placement;
ranges.Track(new IntPtr(2), scale => placement = baseline * scale);
Equal(30f, placement);

// Destroyed objects are never touched; reused native pointers can register anew.
ranges.Remove(new IntPtr(1));
ranges.SetRange(ToolRange.x2);
Equal(80f, winch);
ranges.Track(new IntPtr(1), scale => winch = 30f * scale);
Equal(60f, winch);

ranges.SetRange((ToolRange)999);
Equal(1f, ranges.Multiplier);
Equal(30f, winch);
Equal(7.5f, placement);
ranges.SetRange(ToolRange.x10);
ranges.RestoreAll();
Equal(30f, winch);
Equal(7.5f, placement);
ranges.SetRange(ToolRange.x4);
Equal(30f, winch);
Equal(7.5f, placement);
Console.WriteLine("PASS: six presets, invalid value, live updates, no compounding, reopen, destruction and restoration.");
