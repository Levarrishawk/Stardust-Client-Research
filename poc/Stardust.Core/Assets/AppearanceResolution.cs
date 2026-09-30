using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public sealed record AppearanceResolution(string RequestedPath,string SelectedRootPath,string TerminalPath,
    bool UsedDefault,IReadOnlyList<AssetSourceIdentity> Sources,IffNode TerminalMetadata,ReadOnlyMemory<byte> TerminalBytes);

/// <summary>Separate strict and proven call-site default fetch boundaries. No loose-file or asynchronous discovery.</summary>
public static class AppearanceResolver
{
    public static AppearanceResolution FetchStrict(Func<string,TreLookupResult> resolve,string? path,string? expectedSelectedRootSha256=null)
        => Fetch(resolve,path,false,expectedSelectedRootSha256);

    public static AppearanceResolution FetchOrDefault(Func<string,TreLookupResult> resolve,string? path,string? expectedSelectedRootSha256=null)
        => Fetch(resolve,path,true,expectedSelectedRootSha256);

    private static AppearanceResolution Fetch(Func<string,TreLookupResult> resolve,string? path,bool allowDefault,string? expected)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        string requested=string.IsNullOrEmpty(path)?string.Empty:LegacyAssetPath.Normalize(path);
        TreLookupResult selected=requested.Length==0?new(TreLookupKind.Missing):resolve(requested);
        bool substituted=allowDefault && selected.Kind!=TreLookupKind.Present;
        string rootPath=substituted?"appearance/defaultappearance.apt":requested;
        if(substituted) selected=resolve(rootPath);
        byte[] Read(TreLookupResult value)
        {
            if(value.Kind!=TreLookupKind.Present || value.Payload is null || value.Payload.Length>16*1024*1024)
                throw new InvalidDataException("Appearance is missing, masked or over budget.");
            return value.Payload;
        }
        byte[] bytes=Read(selected);
        List<AssetSourceIdentity> sources=new();
        void Identity(string resource,TreLookupResult value,byte[] payload)
            => sources.Add(new(resource,value.Priority,payload.Length,Convert.ToHexString(SHA256.HashData(payload))));
        Identity(rootPath,selected,bytes);
        if(expected is not null && !sources[0].Sha256.Equals(expected,StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Selected appearance root identity mismatch.");
        IffNode metadata=IffDocument.Parse(bytes).Root;
        string terminal=rootPath;
        if(metadata.IsForm("APT "))
        {
            if(metadata.Children.Count!=1 || !metadata.Children[0].IsForm("0000") ||
                metadata.Children[0].Children.Count!=1 || metadata.Children[0].Children[0].Tag!="NAME")
                throw new InvalidDataException("Unsupported appearance redirect layout.");
            terminal=StaticEffectDecoder.ReadNamePrefix(metadata.Children[0].Children[0].Payload.Span,false);
            if(terminal.EndsWith(".apt",StringComparison.Ordinal)) throw new InvalidDataException("Appearance redirect to another APT is unsupported.");
            // Recursive targets use strict lookup; a live broken redirect never gets path-level default substitution.
            selected=resolve(terminal);bytes=Read(selected);Identity(terminal,selected,bytes);
            metadata=IffDocument.Parse(bytes).Root;
            if(metadata.IsForm("APT "))throw new InvalidDataException("Appearance redirect target is another redirect.");
        }
        return new(requested,rootPath,terminal,substituted,sources.AsReadOnly(),metadata,bytes.ToArray());
    }
}
