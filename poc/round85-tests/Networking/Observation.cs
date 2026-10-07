using System;
using System.Linq;
using Stardust.Core.Protocol;
public static class Networking85Observation
{
    private static object Text(ReplicationByteString value) => new { Hex=Convert.ToHexString(value.CopyBytes()), Extended=value.UsesExtendedLength };
    public static object Project(CreatureView4Baseline a, CreatureView6Baseline b) => new {
        Creo4=new { a.ObjectId,a.DeclaredCount,a.FloatBits,a.ListenTo,a.Encumbrances,
            SkillMods=new { a.SkillMods.Counter,Values=a.SkillMods.Values.Select(v=>new {v.RawCommand,Name=Text(v.Name),v.BaseValue,v.Bonus}).ToArray() },a.SpaceMissionObjects },
        Creo6=new { b.ObjectId,b.DeclaredCount,b.Passive,b.Defenders,b.Level,PerformanceAnimation=Text(b.PerformanceAnimation),MoodString=Text(b.MoodString),
            b.WeaponId,b.GroupId,b.InviterId,b.InviteCounter,b.Guild,b.TargetId,b.MoodId,b.PerformanceStart,b.PerformanceType,b.Ham,b.MaximumHam,
            Wearables=new { b.Wearables.Counter,Values=b.Wearables.Values.Select(v=>new {Customization=Text(v.Customization),v.Arrangement,v.ObjectId,v.TemplateCrc}).ToArray() },
            AlternateAppearance=Text(b.AlternateAppearance),b.Frozen }
    };
}
