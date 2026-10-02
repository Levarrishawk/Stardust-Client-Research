using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.SmokeTests;
byte[] wire = File.ReadAllBytes(args[0]);
CreatureView3BaselineScenarios.Run(wire);
var limits = new CreatureView3BaselineLimits(wire.Length - 23, 3, 3, 3, 119);
var v = CreatureView3BaselineCodec.Decode(wire, limits);
Directory.CreateDirectory(args[1]);
File.WriteAllBytes(Path.Combine(args[1], "baseline.bin"), CreatureView3BaselineCodec.Encode(v, limits));
BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(23), 65535);
File.WriteAllBytes(Path.Combine(args[1], "baseline-count65535.bin"), CreatureView3BaselineCodec.Encode(CreatureView3BaselineCodec.Decode(wire, limits), limits));
File.WriteAllText(Path.Combine(args[1], "snapshot.json"), JsonSerializer.Serialize(new {
    v.ObjectId, v.DeclaredCount, ComplexityBits = (uint)BitConverter.SingleToInt32Bits(v.Complexity),
    NameFile = Convert.ToHexString(v.NameFile.CopyBytes()), NameFileExtended = v.NameFile.UsesExtendedLength, v.NameSpacer,
    NameKey = Convert.ToHexString(v.NameKey.CopyBytes()), NameKeyExtended = v.NameKey.UsesExtendedLength,
    v.CustomNameUnits, v.Volume, Customization = Convert.ToHexString(v.Customization.CopyBytes()), CustomizationExtended = v.Customization.UsesExtendedLength,
    ComponentCounter = v.Components.Counter, Components = v.Components.Values, v.Options, v.UseCount, v.ConditionDamage, v.MaximumCondition,
    v.Visibility, v.Scalars.Posture, v.Scalars.Rank, v.Scalars.LinkId, HeightBits = (uint)BitConverter.SingleToInt32Bits(v.Scalars.Height),
    v.Scalars.Shock, v.Scalars.States, WoundCounter = v.Wounds.Counter, Wounds = v.Wounds.Values, v.DecodedBytes
}));
