using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public sealed record BlueprintFaceRasterLimits(int MaximumDimension=1024,int MaximumTriangles=1024,long MaximumPixelTests=33_554_432);
public sealed record BlueprintFaceLayer(uint ShaderIndex,uint VertexArrayIndex,string TexturePath,IReadOnlyList<int> MipLevels,
    int WrittenPixels,string RgbaSha256);
public sealed record BlueprintComposedFace(DdsImage Image,IReadOnlyList<BlueprintFaceLayer> Layers)
{
    public const string Policy="parallel-top-left-half-pixel-rgba8-normal-opacity-v1";
}

/// <summary>Bounded CPU face preview: explicit half-pixel raster, linear samples, point LOD and RGBA8 scratch policy.</summary>
public static class BlueprintFaceCompositor
{
    private const uint Main=0x4D41494E;
    private readonly record struct Vertex(double X,double Y,double U,double V);
    private sealed record Draw(uint Shader,uint Array,string Path,DdsImage Image,uint Factor,bool Blend,bool ClampU,bool ClampV,Vertex[] Vertices);

    public static BlueprintComposedFace Compose(BlueprintTextureRendererDescription blueprint,BlueprintFaceResources resources,
        BlueprintFacePreparation preparation,BlueprintFaceRasterLimits? limits=null,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(blueprint);ArgumentNullException.ThrowIfNull(resources);ArgumentNullException.ThrowIfNull(preparation);
        limits??=new();
        if(limits.MaximumDimension is <1 or >4096||limits.MaximumTriangles is <1 or >65536||limits.MaximumPixelTests<1)throw new ArgumentOutOfRangeException(nameof(limits));
        Need(blueprint.Width>0&&blueprint.Height>0&&blueprint.Width<=limits.MaximumDimension&&blueprint.Height<=limits.MaximumDimension,"Face destination budget.");
        Need(float.IsFinite(blueprint.CameraScalar)&&blueprint.CameraScalar>0,"Face camera invalid.");
        Need(preparation.ShaderSlots.Count==blueprint.ShaderPaths.Count,"Prepared face slot count differs.");
        int width=(int)blueprint.Width,height=(int)blueprint.Height,triangleCount=0;long pixelTests=0;
        var draws=new List<Draw>();
        foreach(var set in blueprint.RenderSets)
        {
            cancellation.ThrowIfCancellationRequested();
            Need(set.ShaderIndex<preparation.ShaderSlots.Count,"Face render shader out of range.");
            var slot=preparation.ShaderSlots[(int)set.ShaderIndex];
            Need(slot.Slot==set.ShaderIndex&&slot.ShaderPath==blueprint.ShaderPaths[(int)set.ShaderIndex],"Prepared shader identity differs.");
            Need(resources.EffectsByShader.TryGetValue(slot.ShaderPath,out var effect)&&resources.Shaders.TryGetValue(slot.ShaderPath,out _),"Face effect absent.");
            var passes=effect!.Implementations.Where(i=>i.Passes.Count==1).Select(i=>i.Passes[0]).Where(p=>p.PixelProgram is not null).ToArray();
            Need(passes.Length==1,"Face preview requires one programmable candidate.");var pass=passes[0];
            Need(pass.Version=="0007"&&pass.VertexProgram=="vertex_program/texren_copy_c1a1.vsh"&&pass.FixedStageCount==0,"Unsupported face vertex/pass policy.");
            uint Field(int offset){Need(pass.Fields.TryGetValue(offset,out uint value),"Face pass field absent.");return value;}
            Need(Field(0x19)==0&&Field(0x1A)==0&&Field(0x30)==0&&Field(0x4C)==0&&Field(0x3C)==15&&Field(0x24)==0&&Field(0x28)==4&&Field(0x2C)==5&&Field(0x40)==Main&&Field(0x44)==0&&Field(0x20)<=1,"Unsupported face blend/depth/stencil/factor policy.");
            Need(resources.PixelPrograms is not null&&resources.PixelPrograms.TryGetValue(pass.PixelProgram!,out var program)&&program.TextureStage==0&&program.FactorRegister==3,"Validated face pixel program required.");
            Need(pass.PixelTextures.Count==1&&pass.PixelTextures[0].Stage==0&&pass.PixelTextures[0].TextureTag==Main,"Unsupported face texture stage.");
            var pixel=pass.PixelTextures[0];var inherited=pixel.Metadata.Span;
            Need(pixel.Version=="0000"&&inherited.Length==16&&U32(inherited[5..])==Main&&inherited[9..].SequenceEqual(new byte[]{5,5,5,6,6,6,0}),"Unsupported face sampler inheritance.");
            var shader=resources.Shaders[slot.ShaderPath];var binding=shader.Textures.Where(t=>t.Tag==Main).ToArray();
            Need(binding.Length==1&&binding[0].Version=="0002"&&binding[0].BindingMetadata.Length==12,"Unsupported face texture binding.");
            var sampler=binding[0].BindingMetadata.Span;
            Need(sampler[5] is 0 or 2&&sampler[6] is 0 or 2&&sampler[7]==0&&sampler[8]==1&&sampler[9]==2&&sampler[10]==2&&sampler[11]==0,"Unsupported face sampler.");
            var coordinates=shader.Metadata.Children[0].Children.Where(n=>n.IsForm("TCSS")).ToArray();
            Need(coordinates.Length==1&&coordinates[0].Children.Count==1&&coordinates[0].Children[0].Tag=="0000","Face coordinates absent.");
            var map=coordinates[0].Children[0].Payload.Span;
            Need(map.Length==5&&U32(map)==Main&&map[4]==0,"Face preview requires MAIN/UV0.");
            Need(slot.TextureBindings.TryGetValue(Main,out string? path)&&resources.Textures.TryGetValue(path,out _),"Prepared MAIN texture absent.");
            Need(slot.TextureFactors.TryGetValue(Main,out uint factor),"Prepared MAIN factor absent.");var image=resources.Textures[path!];
            Need(image.Mips.Count is >0 and <=16&&image.ColorSpace!=DdsColorSpace.Srgb&&image.AlphaMode!=DdsAlphaMode.Premultiplied,"Unsupported face image policy.");
            for(int mip=0;mip<image.Mips.Count;mip++)
            {
                var level=image.Mips[mip];Need(level.Width>0&&level.Height>0&&level.Width<=8192&&level.Height<=8192&&level.Rgba.Length==(long)level.Width*level.Height*4,"Invalid face pixels.");
                Need(level.Width==Math.Max(1,image.Width>>mip)&&level.Height==Math.Max(1,image.Height>>mip),"Invalid face mip dimensions.");
            }
            foreach(uint index in set.VertexArrayIndices)
            {
                Need(index<blueprint.VertexArrays.Count,"Face vertex array out of range.");var node=blueprint.VertexArrays[(int)index];
                Need(node.IsForm("VTXA")&&node.Children.Count==1&&node.Children[0].IsForm("0003"),"Unsupported face vertices.");var chunks=node.Children[0].Children;
                Need(chunks.Count==2&&chunks[0].Tag=="INFO"&&chunks[0].Payload.Length==8&&U32(chunks[0].Payload.Span)==0x1101&&chunks[1].Tag=="DATA","Face vertex declaration invalid.");
                uint count=U32(chunks[0].Payload.Span[4..]);Need(count>=3&&count<=limits.MaximumTriangles+2&&chunks[1].Payload.Length==(long)count*20,"Face vertex/triangle budget.");
                var vertices=new Vertex[count];var bytes=chunks[1].Payload.Span;float z=F32(bytes[8..]);
                Need(float.IsFinite(z)&&z>=1&&z<=5000,"Face depth outside camera.");
                for(int v=0;v<vertices.Length;v++)
                {
                    var b=bytes[(v*20)..];float x=F32(b),y=F32(b[4..]),u=F32(b[12..]),vv=F32(b[16..]);
                    Need(float.IsFinite(x)&&float.IsFinite(y)&&float.IsFinite(u)&&float.IsFinite(vv)&&F32(b[8..])==z,"Face fan requires finite planar coordinates.");
                    double screenX=x/(double)blueprint.CameraScalar*width,screenY=y/(double)blueprint.CameraScalar*height;
                    Need(Math.Abs(screenX)<=1e6&&Math.Abs(screenY)<=1e6&&Math.Abs(u)<=1e4&&Math.Abs(vv)<=1e4,"Face coordinate budget.");vertices[v]=new(screenX,screenY,u,vv);
                }
                for(int t=1;t<vertices.Length-1;t++)
                {
                    Need(++triangleCount<=limits.MaximumTriangles,"Face triangle budget.");double area=Edge(vertices[0],vertices[t],vertices[t+1].X,vertices[t+1].Y);
                    Need(area>=0,"Unsupported face fan winding.");if(area==0)continue;
                    var box=Bounds(vertices[0],vertices[t],vertices[t+1],width,height);
                    pixelTests+=(long)Math.Max(0,box.Right-box.Left)*Math.Max(0,box.Bottom-box.Top);Need(pixelTests<=limits.MaximumPixelTests,"Face raster work budget.");
                }
                draws.Add(new(set.ShaderIndex,index,path!,image,factor,Field(0x20)!=0,sampler[5]==2,sampler[6]==2,vertices));
            }
        }
        cancellation.ThrowIfCancellationRequested();var rgba=new byte[width*height*4];var layers=new List<BlueprintFaceLayer>();
        foreach(var draw in draws)
        {
            int written=0;var mipLevels=new List<int>();
            for(int triangle=1;triangle<draw.Vertices.Length-1;triangle++)
            {
                Vertex a=draw.Vertices[0],b=draw.Vertices[triangle],c=draw.Vertices[triangle+1];double area=Edge(a,b,c.X,c.Y);if(area==0)continue;
                double dudx=((b.U-a.U)*(c.Y-a.Y)-(c.U-a.U)*(b.Y-a.Y))/area;
                double dudy=((b.X-a.X)*(c.U-a.U)-(c.X-a.X)*(b.U-a.U))/area;
                double dvdx=((b.V-a.V)*(c.Y-a.Y)-(c.V-a.V)*(b.Y-a.Y))/area;
                double dvdy=((b.X-a.X)*(c.V-a.V)-(c.X-a.X)*(b.V-a.V))/area;
                double footprint=Math.Max(Math.Sqrt(Math.Pow(dudx*draw.Image.Width,2)+Math.Pow(dvdx*draw.Image.Height,2)),Math.Sqrt(Math.Pow(dudy*draw.Image.Width,2)+Math.Pow(dvdy*draw.Image.Height,2)));
                int mip=(int)Math.Clamp(Math.Floor(Math.Log2(Math.Max(1,footprint))+.5),0,draw.Image.Mips.Count-1);mipLevels.Add(mip);
                var box=Bounds(a,b,c,width,height);var image=draw.Image.Mips[mip];
                for(int y=box.Top;y<box.Bottom;y++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    for(int x=box.Left;x<box.Right;x++)
                    {
                        double px=x+.5,py=y+.5,e0=Edge(b,c,px,py),e1=Edge(c,a,px,py),e2=Edge(a,b,px,py);
                        if(!Inside(e0,b,c)||!Inside(e1,c,a)||!Inside(e2,a,b))continue;
                        double u=(e0*a.U+e1*b.U+e2*c.U)/area,v=(e0*a.V+e1*b.V+e2*c.V)/area;
                        int at=(y*width+x)*4;double alpha=Sample(image,u,v,3,draw.ClampU,draw.ClampV),weight=draw.Blend?alpha/255:1;
                        for(int channel=0;channel<4;channel++)
                        {
                            double source=channel==3?alpha:Sample(image,u,v,channel,draw.ClampU,draw.ClampV)*((draw.Factor>>(16-8*channel))&255)/255;
                            rgba[at+channel]=Quantize(source*weight+rgba[at+channel]*(1-weight));
                        }
                        written++;
                    }
                }
            }
            layers.Add(new(draw.Shader,draw.Array,draw.Path,mipLevels.AsReadOnly(),written,Convert.ToHexString(SHA256.HashData(rgba))));
        }
        return new(new(DdsFormat.Rgba32,Array.AsReadOnly(new[]{new DdsMip(width,height,rgba)}),DdsColorSpace.Unspecified,DdsAlphaMode.Straight),layers.AsReadOnly());
    }

    private static (int Left,int Top,int Right,int Bottom) Bounds(Vertex a,Vertex b,Vertex c,int width,int height)
        =>(Math.Clamp((int)Math.Ceiling(Math.Min(a.X,Math.Min(b.X,c.X))-.5),0,width),Math.Clamp((int)Math.Ceiling(Math.Min(a.Y,Math.Min(b.Y,c.Y))-.5),0,height),
            Math.Clamp((int)Math.Floor(Math.Max(a.X,Math.Max(b.X,c.X))-.5)+1,0,width),Math.Clamp((int)Math.Floor(Math.Max(a.Y,Math.Max(b.Y,c.Y))-.5)+1,0,height));
    private static double Edge(Vertex a,Vertex b,double x,double y)=>(b.X-a.X)*(y-a.Y)-(b.Y-a.Y)*(x-a.X);
    private static bool Inside(double edge,Vertex a,Vertex b)=>edge>0||(edge==0&&(b.Y<a.Y||(b.Y==a.Y&&b.X>a.X)));
    private static double Sample(DdsMip image,double u,double v,int channel,bool clampU,bool clampV)
    {
        double x=u*image.Width-.5,y=v*image.Height-.5;int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);double tx=x-ix,ty=y-iy;
        int Address(int coordinate,int size,bool clamp)=>clamp?Math.Clamp(coordinate,0,size-1):((coordinate%size)+size)%size;
        double Pixel(int xx,int yy)=>image.Rgba.Span[(Address(yy,image.Height,clampV)*image.Width+Address(xx,image.Width,clampU))*4+channel];
        return (1-ty)*((1-tx)*Pixel(ix,iy)+tx*Pixel(ix+1,iy))+ty*((1-tx)*Pixel(ix,iy+1)+tx*Pixel(ix+1,iy+1));
    }
    private static byte Quantize(double value)=>(byte)Math.Clamp(Math.Floor(value+.5),0,255);
    private static uint U32(ReadOnlySpan<byte> bytes)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    private static float F32(ReadOnlySpan<byte> bytes)=>BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes));
    private static void Need(bool value,string message){if(!value)throw new InvalidDataException(message);}
}
