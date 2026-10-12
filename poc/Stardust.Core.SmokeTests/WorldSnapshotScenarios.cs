using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

internal static class WorldSnapshotScenarios
{
    internal static void Run()
    {
        byte[] Chunk(string t,byte[] data)=>MeshScenarios.Chunk(t,data);
        byte[] Form(string t,params byte[][] children)=>MeshScenarios.Form(t,children);
        var words=new List<byte>();void I(int v)=>words.AddRange(BitConverter.GetBytes(v));void F(float v)=>words.AddRange(BitConverter.GetBytes(v));
        I(123);I(0);I(0);I(0);F(.5f);F(.125f);F(.25f);F(.375f);F(10);F(20);F(-30);F(64);I(unchecked((int)0x89ABCDEF));
        byte[] node=Form("NODE",Form("0000",Chunk("DATA",words.ToArray())));
        byte[] names=BitConverter.GetBytes(1).Concat(Encoding.ASCII.GetBytes("object/test.iff\0")).ToArray();
        byte[] Snapshot(byte[] n,byte[]? table=null)=>Form("WSNP",Form("0001",Form("NODS",n),Chunk("OTNL",table??names)));
        byte[] raw=Snapshot(node);var decoded=WorldSnapshotInput.Decode(raw);var selected=decoded.Nodes.Single();
        if(selected.Rotation!=new Quaternion(.125f,.25f,.375f,.5f)||selected.Position!=new Vector3(10,20,-30)||selected.PortalLayoutCrc!=0x89ABCDEF||selected.Id!=123)throw new Exception("Independent asymmetric snapshot field order.");
        Array.Clear(raw);if(selected.Position.Y!=20)throw new Exception("Snapshot borrowed input.");
        void Reject(Action a){try{a();}catch(InvalidDataException){return;}throw new Exception("Malformed original scene input accepted.");}
        raw=Snapshot(node);for(int n=0;n<raw.Length;n++)Reject(()=>WorldSnapshotInput.Decode(raw.AsSpan(0,n)));
        Reject(()=>WorldSnapshotInput.Decode(Snapshot(node.Concat(node).ToArray())));
        Reject(()=>WorldSnapshotInput.Decode(Snapshot(node,BitConverter.GetBytes(-1))));
        var bad=(byte[])words.ToArray().Clone();BitConverter.GetBytes(float.NaN).CopyTo(bad,16);Reject(()=>WorldSnapshotInput.Decode(Snapshot(Form("NODE",Form("0000",Chunk("DATA",bad))))));
        byte[] Layer(string family,string version,byte[]? child=null)=>Form(family,child is null?new[]{Form(version,Chunk("PCNT",BitConverter.GetBytes(0)))}:new[]{Form(version,Chunk("PCNT",BitConverter.GetBytes(0))),child});
        byte[] shot=Layer("SHOT","0007");
        foreach(byte[] template in new[]{Layer("STAT","0000",shot),Layer("SBOT","0001",Layer("STOT","0007",shot))})
        {
            _=SharedObjectTemplateDecoder.Decode(template);for(int n=0;n<template.Length;n++)Reject(()=>SharedObjectTemplateDecoder.Decode(template.AsSpan(0,n)));
        }
        Reject(()=>SharedObjectTemplateDecoder.Decode(Layer("STAT","0001",shot)));
        Reject(()=>SharedObjectTemplateDecoder.Decode(Layer("SBOT","0001",shot)));
        Reject(()=>SharedObjectTemplateDecoder.Decode(Form("STAT",Form("0000",Chunk("PCNT",BitConverter.GetBytes(1))),shot)));
        var boxes=new MosTaikeObstacleBoxes(new[]{new MosTaikeObstacleBox(1,new(0,0,0),new(10,4,10))});
        if(!boxes.Blocks(new(-1,0,5),new(11,0,5))||boxes.Blocks(new(-1,0,10.36f),new(11,0,10.36f))||boxes.Blocks(new(-1,5,5),new(11,5,5)))throw new Exception("Swept obstruction/tangent/vertical separation.");
        float corrected=boxes.CameraDistance(new(-2,2,5),new(12,2,5));
        if(corrected>=2||corrected<.25f||boxes.CameraDistance(new(-2,5,12),new(12,5,12))!=14)throw new Exception("Camera segment obstacle separation.");
        Console.WriteLine("PASS: conservative town swept crossing rejection, tangent/open-height passage and camera obstruction distance");
        Console.WriteLine("PASS: original snapshot asymmetric WXYZ/XYZ fields, owned input and truncation/count/duplicate/finite gates; SBOT/STAT framing negatives");
    }
}
