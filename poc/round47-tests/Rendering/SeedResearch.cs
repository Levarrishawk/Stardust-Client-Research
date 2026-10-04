using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Number=Stardust.Core.Geometry.TerrainCircleArithmetic.Number;
internal static class SeedResearch
{
 static Number I(long v)=>new(new BigInteger(v),0);
 static Number N(uint bits)=>Number.FromBits(bits);
 static void Need(bool value){if(!value)throw new InvalidDataException("Modern research eligibility");}
 static uint Store(Number n){uint b=n.ToFloatBits();Need((b&0x7f800000)!=0x7f800000);return b;}
 static BigInteger Trunc(Number n)=>n.Exponent>=0?n.Mantissa<<n.Exponent:n.Mantissa/(BigInteger.One<<-n.Exponent);
 static int Floor(Number n){BigInteger t=Trunc(n);if(n.Mantissa.Sign<0&&n.Exponent<0&&n.Mantissa%(BigInteger.One<<-n.Exponent)!=0)t--;return checked((int)t);}
 static string Fraction(Number n){BigInteger top=n.Mantissa,bot=BigInteger.One;if(n.Exponent>=0)top<<=n.Exponent;else bot<<=-n.Exponent;BigInteger g=BigInteger.GreatestCommonDivisor(BigInteger.Abs(top),bot);return $"{top/g}/{bot/g}";}
 static string Hash(IEnumerable<uint> words){uint[] values=words.ToArray();byte[] bytes=new byte[4*values.Length];for(int i=0;i<values.Length;i++)BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4*i),values[i]);return Convert.ToHexString(SHA256.HashData(bytes));}
 static int Index(int iy,int p)=>checked((int)Trunc(Number.Divide(I(iy),I(6669205),p)));
 sealed class Rng
 {
  internal int Seed,Iy,Steps,Draws;internal int[] Table=new int[322];readonly int p;
  internal Rng(uint seed,int precision){Seed=unchecked(-(int)seed);p=precision;}
  void Step(){int q=Seed/127773;long next=16807L*(Seed-q*127773L)-2836L*q;if(next<0)next+=2147483647;Seed=checked((int)next);Steps++;}
  internal int Next()
  {
   if(Seed<=0||Iy==0){Seed=Math.Max(1,unchecked(-Seed));for(int j=329;j>=0;j--){Step();if(j<322)Table[j]=Seed;}Iy=Table[0];}
   Step();int idx=Index(Iy,p);Need(idx>=0&&idx<322);int value=Table[idx];Table[idx]=Seed;Iy=value;Draws++;return value;
  }
 }
 static uint[] Normalize(uint xb,uint yb,int p)
 {
  Number x=N(xb),y=N(yb);Number length=Number.SquareRoot(Number.Add(Number.Multiply(y,y,p),Number.Multiply(x,x,p),p),p),inv=Number.Divide(I(1),length,p);
  return [Store(Number.Multiply(inv,x,p)),Store(Number.Multiply(inv,y,p))];
 }
 sealed record TableData(List<uint> Perm,List<uint> G1,List<uint[]> G2,Rng Generator);
 static readonly Dictionary<(uint,int),TableData> tableCache=new();
 static TableData Tables(uint seed,int p)
 {
  if(tableCache.TryGetValue((seed,p),out var prior))return prior;
  var rng=new Rng(seed,p);var perm=Enumerable.Range(0,256).Select(x=>(uint)x).ToList();var g1=new List<uint>();var g2=new List<uint[]>();
  for(int i=0;i<256;i++)
  {
   g1.Add(Store(Number.Multiply(I(rng.Next()%512-256),N(0x3b800000),p)));
   uint x=Store(Number.Multiply(I(rng.Next()%512-256),N(0x3b800000),p)),y=Store(Number.Multiply(I(rng.Next()%512-256),N(0x3b800000),p));g2.Add(Normalize(x,y,p));
  }
  for(int i=255;i>=1;i--){int j=rng.Next()%256;(perm[i],perm[j])=(perm[j],perm[i]);}
  for(int i=0;i<258;i++){perm.Add(perm[i]);g1.Add(g1[i]);g2.Add((uint[])g2[i].Clone());}
  var result=new TableData(perm,g1,g2,rng);tableCache.Add((seed,p),result);return result;
 }
 static uint[] Parse(byte[] data)
 {
  Need(data.Length==84);
  Need(data.AsSpan(0,4).SequenceEqual("FORM"u8)&&BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4))==76&&data.AsSpan(8,4).SequenceEqual("MFRC"u8)&&data.AsSpan(12,4).SequenceEqual("FORM"u8)&&BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(16))==64&&data.AsSpan(20,4).SequenceEqual("0001"u8)&&data.AsSpan(24,4).SequenceEqual("DATA"u8)&&BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(28))==52);
  uint[] words=new uint[13];for(int i=0;i<13;i++)words[i]=BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(32+4*i));return words;
 }
 static (BigInteger value,uint low,uint high) Helper(Number n)
 {
  BigInteger v=Trunc(n);Need(v>=-(BigInteger.One<<63)&&v<(BigInteger.One<<63));BigInteger raw=v&((BigInteger.One<<64)-1);return (v,(uint)(raw&uint.MaxValue),(uint)(raw>>32));
 }
 static object Noise(string name,uint xb,uint zb,uint seed,int tp,int p)
 {
  var t=Tables(seed,tp);Number Add(Number x,Number y)=>Number.Add(x,y,p);Number Mul(Number x,Number y)=>Number.Multiply(x,y,p);
  Number bx=Add(N(xb),I(4096)),bz=Add(N(zb),I(4096));var tx=Helper(bx).value;var tz=Helper(bz).value;Need(tx>=int.MinValue&&tx<=int.MaxValue&&tz>=int.MinValue&&tz<=int.MaxValue);
  int ix=Floor(bx),iz=Floor(bz);Number fx=N(Store(Add(bx,I(-(long)ix)))),fz=Add(bz,I(-(long)iz)),dx=Add(fx,I(-1)),dz=Add(fz,I(-1));int x0=ix&255,x1=(ix+1)&255,z0=iz&255,z1=(iz+1)&255;
  int[] corners=[(int)t.Perm[(int)t.Perm[x0]+z0],(int)t.Perm[(int)t.Perm[x1]+z0],(int)t.Perm[(int)t.Perm[x0]+z1],(int)t.Perm[(int)t.Perm[x1]+z1]];
  Number Dot(int i,Number x,Number z)=>Add(Mul(z,N(t.G2[i][1])),Mul(x,N(t.G2[i][0])));
  Number Fade(Number v)=>Mul(Mul(Add(I(3),Add(v,v).Negate()),v),v);
  Number Lerp(Number f,Number a,Number b)=>Add(Mul(Add(b,a.Negate()),f),a);
  Number sx=Fade(fx),sz=Fade(fz);Number value=Lerp(sz,Lerp(sx,Dot(corners[0],fx,fz),Dot(corners[1],dx,fz)),Lerp(sx,Dot(corners[2],fx,dz),Dot(corners[3],dx,dz)));
  return new{Name=name,Reject="None",LatticeX=ix,LatticeZ=iz,FractionX=Store(fx),FractionZ=Fraction(fz),Corners=corners,Retained=Fraction(value),NegativeZero=value.NegativeZero,Projection=Store(value)};
 }
 internal static List<object> Run(string path)
 {
  using var doc=JsonDocument.Parse(File.ReadAllText(path));var results=new List<object>();
  foreach(var f in doc.RootElement.EnumerateArray())
  {
   string name=f.GetProperty("Name").GetString()!;int p=f.GetProperty("Precision").GetInt32();if(p is not (24 or 53 or 64)){results.Add(new{Name=name,Reject="Options"});continue;}
   uint Bits(string key)=>f.GetProperty(key).GetUInt32();int Int(string key)=>f.GetProperty(key).GetInt32();
   try
   {
    string kind=f.GetProperty("Kind").GetString()!;
    if(kind=="Raw")results.Add(new{Name=name,Reject="None",Words=Parse(Convert.FromHexString(f.GetProperty("Hex").GetString()!))});
    else if(kind=="Index"){int iy=Int("Iy");results.Add(new{Name=name,Reject="None",Index=Index(iy,p),IntegerDivision=iy/6669205});}
    else if(kind=="Rng")
    {
     var rng=new Rng(Bits("Seed"),p);int[] values=Enumerable.Range(0,Int("Draws")).Select(_=>rng.Next()).ToArray();
     results.Add(new{Name=name,Reject="None",SequenceSha=Hash(values.Select(x=>(uint)x)),StateSha=Hash(new[]{rng.Seed,rng.Iy}.Concat(rng.Table).Select(x=>unchecked((uint)x))),rng.Steps,rng.Draws});
    }
    else if(kind=="Normalize")results.Add(new{Name=name,Reject="None",Words=Normalize(Bits("X"),Bits("Z"),p)});
    else if(kind=="Tables")
    {
     var t=Tables(Bits("Seed"),p);results.Add(new{Name=name,Reject="None",PermutationSha=Hash(t.Perm),Gradient1Sha=Hash(t.G1),Gradient2Sha=Hash(t.G2.SelectMany(x=>x)),RngDraws=t.Generator.Draws,RngSteps=t.Generator.Steps,FinalSeed=t.Generator.Seed,FinalIy=t.Generator.Iy});
    }
    else if(kind=="Helper"){var h=Helper(N(Bits("X")));results.Add(new{Name=name,Reject="None",Low=h.low,High=h.high,Value=h.value.ToString()});}
    else results.Add(Noise(name,Bits("X"),Bits("Z"),Bits("Seed"),Int("TablePrecision"),p));
   }
   catch(InvalidDataException){results.Add(new{Name=name,Reject="Input"});}
   catch(OverflowException){results.Add(new{Name=name,Reject="Input"});}
  }
  return results;
 }
}
