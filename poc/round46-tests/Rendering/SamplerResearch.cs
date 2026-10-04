using System.Numerics;
using System.Text.Json;
using Number=Stardust.Core.Geometry.TerrainCircleArithmetic.Number;
internal static class SamplerResearch
{
 static Number N(uint b)=>Number.FromBits(b);
 static uint Store(Number n){uint b=n.ToFloatBits();Need((b&0x7f800000)!=0x7f800000);return b;}
 static void Need(bool v){if(!v)throw new InvalidDataException("Modern research eligibility");}
 static string Fraction(Number n){BigInteger top=n.Mantissa,bot=BigInteger.One;if(n.Exponent>=0)top<<=n.Exponent;else bot<<=-n.Exponent;BigInteger g=BigInteger.GreatestCommonDivisor(BigInteger.Abs(top),bot);return $"{top/g}/{bot/g}";}
 public static List<object> Run(string path)
 {
  using var document=JsonDocument.Parse(File.ReadAllText(path));var results=new List<object>();
  foreach(var f in document.RootElement.EnumerateArray())
  {
   string name=f.GetProperty("Name").GetString()!;int p=f.GetProperty("Precision").GetInt32();
   if(p is not (24 or 53 or 64)){results.Add(new{Name=name,Reject="Options"});continue;}
   uint Bits(string k)=>f.GetProperty(k).GetUInt32();Number Get(string k)=>N(Bits(k));int Int(string k)=>f.GetProperty(k).GetInt32();
   try
   {
    string kind=f.GetProperty("Kind").GetString()!;
    if(kind=="Probe")
    {
     int stride=Int("Stride"),rows=Int("Rows"),col=Int("Column"),row=Int("Row"),valid=Int("Valid");Need(stride is >=1 and <=128&&rows is >=1 and <=128&&stride*rows<=1024&&col>=0&&col<stride&&row>=0&&row<rows&&valid is >=0 and <=255);
     Number x=Get("X"),z=Get("Z"),e=N(0x3727c5ac);bool hit=false;
     if(valid!=0){Number cx=Get("CachedX"),cz=Get("CachedZ");hit=Number.Compare(Number.Subtract(cx,e,p),x)<=0&&Number.Compare(x,Number.Add(cx,e,p))<=0&&Number.Compare(Number.Subtract(cz,e,p),z)<=0&&Number.Compare(z,Number.Add(cz,e,p))<=0;}
     results.Add(new{Name=name,Reject="None",Hit=hit,CellOffset=16*(row*stride+col),Value=hit?(uint?)Store(Get("CachedValue")):null});
    }
    else if(kind=="Grid")
    {
     int ow=Int("OldStride"),oh=Int("OldRows"),w=Int("RequestedStride"),h=Int("RequestedRows"),col=Int("Column"),row=Int("Row");Need(ow is >=0 and <=128&&oh is >=0 and <=128&&(ow==0)==(oh==0)&&w is >=1 and <=128&&h is >=1 and <=128&&ow*oh<=1024&&w*h<=1024&&col>=0&&col<w&&row>=0&&row<h);
     bool growth=w>ow||h>oh;int sw=growth?w:ow,sh=growth?h:oh;
     results.Add(new{Name=name,Reject="None",Growth=growth,Stride=sw,Rows=sh,ClearBytes=growth?16*sw*sh:0,CellOffset=16*(row*sw+col)});
    }
    else if(kind=="Coordinates")
    {
     uint x=Store(Number.Multiply(Get("X"),Get("ScaleX"),p)),z=Store(Number.Multiply(Get("Z"),Get("ScaleZ"),p));uint sx=Store(Number.Add(N(x),Get("OffsetX"),p)),sz=Store(Number.Add(N(z),Get("OffsetZ"),p));
     results.Add(new{Name=name,Reject="None",ScaledX=x,ScaledZ=z,ShiftedX=sx,ShiftedZ=sz,NoiseX=Store(Number.Multiply(N(sx),Get("Frequency"),p)),NoiseZ=Store(Number.Multiply(N(sz),Get("Frequency"),p))});
    }
    else
    {
     int cp=Int("Combine");Need(cp is 24 or 53 or 64);Number u=Number.Multiply(Get("A"),Get("B"),p);uint cached=Store(u);Number miss=Number.Multiply(u,Get("Scale"),cp),hit=Number.Multiply(N(cached),Get("Scale"),cp),h=Get("Height"),w=Get("Weight");
     results.Add(new{Name=name,Reject="None",Retained=Fraction(u),NegativeZero=u.NegativeZero,Cached=cached,AhfrMiss=Store(Number.Add(h,Number.Multiply(miss,w,cp),cp)),AhfrHit=Store(Number.Add(h,Number.Multiply(hit,w,cp),cp)),FfraMiss=Store(miss),FfraHit=Store(hit)});
    }
   }
   catch(InvalidDataException){results.Add(new{Name=name,Reject="Input"});}
  }
  return results;
 }
}
