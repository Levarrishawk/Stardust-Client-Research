using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
try {
    using var document=JsonDocument.Parse(File.ReadAllText(args[0]));var root=document.RootElement;
    var observed=new List<object>();int checks=0,negatives=0;
    void Reject(byte[] raw,ObjectControllerTransformDirection direction) {
        try { _=ObjectControllerTransformCodec.Decode(raw,direction); }
        catch(InvalidDataException) { negatives++;return; }
        throw new InvalidDataException("Malformed transform admitted");
    }
    void Decode(string hex,ObjectControllerTransformDirection direction) {
        var raw=Convert.FromHexString(hex);var decoded=ObjectControllerTransformCodec.Decode(raw,direction);
        if(!ObjectControllerTransformCodec.Encode(decoded).SequenceEqual(raw))throw new InvalidDataException("Exact codec roundtrip");checks++;
        var create=SceneObjectLifecycleCodec.DecodeCreate(Convert.FromHexString(root.GetProperty("Applications").GetProperty("Create").GetString()!));
        var placement=SceneCreatePlacementBuilder.Create(create with {QuaternionX=decoded.QuaternionX,QuaternionY=decoded.QuaternionY,QuaternionZ=decoded.QuaternionZ,QuaternionW=decoded.QuaternionW,
            PositionX=decoded.PositionX,PositionY=decoded.PositionY,PositionZ=decoded.PositionZ},SceneCreateZeroQuaternionPolicy.Reject);
        var t=placement.Transform;
        observed.Add(new {Hex=hex,Direction=direction.ToString(),decoded.Flags,decoded.ObjectId,decoded.OuterWord,decoded.MovementCounter,
            Position=new[]{t.Translation.X,t.Translation.Y,t.Translation.Z},Rows=new[]{t.RowX.X,t.RowX.Y,t.RowX.Z,t.RowY.X,t.RowY.Y,t.RowY.Z,t.RowZ.X,t.RowZ.Y,t.RowZ.Z},decoded.Speed});
        for(int length=0;length<raw.Length;length++)Reject(raw[..length],direction);
        Reject(raw.Concat(new byte[]{0}).ToArray(),direction);
        foreach(int offset in new[]{30,34,38,42,46,50,54,58}) {
            var bad=(byte[])raw.Clone();System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(offset),0x7FC00001);Reject(bad,direction);
        }
    }
    foreach(var app in root.GetProperty("ExpectedMovementApplications").EnumerateArray())Decode(app.GetString()!,ObjectControllerTransformDirection.ClientToServer);
    var corrections=root.GetProperty("CorrectionApplications").EnumerateArray().ToArray();
    if(root.GetProperty("Variant").GetString()=="malformed-correction")Reject(Convert.FromHexString(corrections[0].GetString()!),ObjectControllerTransformDirection.ServerToClient);
    else for(int i=0;i<4;i++)Decode(corrections[i].GetString()!,ObjectControllerTransformDirection.ServerToClient);
    Decode(corrections[4].GetString()!,ObjectControllerTransformDirection.ServerToClient);
    // Parent layout offsets differ; validate exact decode/reencode separately.
    var parent=Convert.FromHexString(corrections[5].GetString()!);
    var parentValue=ObjectControllerTransformCodec.Decode(parent,ObjectControllerTransformDirection.ServerToClient);
    if(parentValue.Kind!=ObjectControllerTransformKind.WithParent||!ObjectControllerTransformCodec.Encode(parentValue).SequenceEqual(parent))throw new InvalidDataException("Parent codec mismatch");checks++;
    Reject(Convert.FromHexString(corrections[6].GetString()!),ObjectControllerTransformDirection.ServerToClient);
    File.WriteAllText(args[1],JsonSerializer.Serialize(new{Passed=true,Checks=checks,Negatives=negatives,Observed=observed,SessionJoinExecuted=false}));
    Console.WriteLine($"PASS independent native codec: {checks} checks/{negatives} negatives; session join separate");
} catch(Exception error) {Console.Error.WriteLine(error);Environment.ExitCode=1;}
