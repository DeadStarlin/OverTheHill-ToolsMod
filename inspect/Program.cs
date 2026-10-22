using System;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using Iced.Intel;

using var stream = File.OpenRead(@"D:\SteamLibrary\steamapps\common\over the hill\GameAssembly.dll");
using var pe = new PEReader(stream);
if (args.Length == 2)
{
    int rva = Convert.ToInt32(args[0], 16), size = Convert.ToInt32(args[1], 16);
    var decoder = Decoder.Create(64, new ByteArrayCodeReader(pe.GetSectionData(rva).GetContent(0, size).ToArray()));
    decoder.IP = 0x180000000UL + (uint)rva;
    while (decoder.IP < 0x180000000UL + (uint)(rva + size))
    {
        decoder.Decode(out var instruction);
        Console.WriteLine($"{instruction.IP:X}: {instruction}");
    }
    return;
}
// Addresses are from the supplied dump; rerun against this exact game build.
foreach (var (rva, size, target) in new[] {
    (0xD2DE30, 0x70, 0xD2EF60),
    (0xD2DEA0, 0x60, 0xD2EF60),
    (0xD2FBF0, 0x240, 0xD2EF60),
    (0xD2EF60, 0x520, 0xD2CBC0) })
{
    var bytes = pe.GetSectionData(rva).GetContent(0, size).ToArray();
    var decoder = Decoder.Create(64, new ByteArrayCodeReader(bytes));
    decoder.IP = 0x180000000UL + (uint)rva;
    bool found = false;
    while (decoder.IP < 0x180000000UL + (uint)rva + (uint)size)
    {
        decoder.Decode(out var instruction);
        if ((instruction.Mnemonic == Mnemonic.Call || instruction.Mnemonic == Mnemonic.Jmp) &&
            instruction.NearBranchTarget == 0x180000000UL + (uint)target)
            found = true;
    }
    if (!found) throw new Exception($"Expected native call {rva:X} -> {target:X} missing.");
    Console.WriteLine($"PASS: native call {rva:X} -> {target:X}");
}

using var modStream = File.OpenRead(Path.Combine(AppContext.BaseDirectory,
    "../../../../bin/Release/net6.0/OTHUnlimitedTools.dll"));
using var mod = new PEReader(modStream);
var metadata = mod.GetMetadataReader();
var patchType = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition)
    .Single(t => metadata.GetString(t.Name) == "KeepEquippedTools");
var prefix = patchType.GetMethods().Select(metadata.GetMethodDefinition)
    .Single(m => metadata.GetString(m.Name) == "Prefix");
var settingsType = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition)
    .Single(t => metadata.GetString(t.Name) == "UnlimitedTools");
var unlimitedField = settingsType.GetFields()
    .Single(f => metadata.GetString(metadata.GetFieldDefinition(f).Name) == "UnlimitedUse");
byte[] fieldRead = new byte[] { 0x7E }.Concat(BitConverter.GetBytes(
    System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(unlimitedField))).ToArray();
byte[] body = mod.GetMethodBody(prefix.RelativeVirtualAddress).GetILBytes();
if (!Enumerable.Range(0, body.Length - fieldRead.Length + 1)
    .Any(i => body.Skip(i).Take(fieldRead.Length).SequenceEqual(fieldRead)))
    throw new Exception("Consumption prefix must read the UnlimitedUse preference.");
Console.WriteLine("PASS: compiled Harmony prefix reads the UnlimitedUse preference.");
