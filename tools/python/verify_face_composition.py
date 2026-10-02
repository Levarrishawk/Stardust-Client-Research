"""Independent rectangular original-face preview reference; explicit modern policy, not driver parity."""
import hashlib
import io
import math
import struct
import numpy as np
from PIL import Image
from verify_static_mesh_slice import nodes


def mip_pixels(data,level):
    height,width=struct.unpack_from('<2I',data,12);offset=128
    compressed=bool(struct.unpack_from('<I',data,80)[0]&4)
    block=8 if data[84:88]==b'DXT1' else 16
    def extent(w,h):return ((w+3)//4)*((h+3)//4)*block if compressed else w*h*4
    for mip in range(level):offset+=extent(max(1,width>>mip),max(1,height>>mip))
    w,h=max(1,width>>level),max(1,height>>level);size=extent(w,h)
    header=bytearray(data[:128]);struct.pack_into('<2I',header,12,h,w);struct.pack_into('<I',header,20,size);struct.pack_into('<I',header,28,1)
    with Image.open(io.BytesIO(bytes(header)+data[offset:offset+size])) as decoded:return np.asarray(decoded.convert('RGBA')).copy()


def verify(raw,blueprint,prepared,native):
    assert native['policy']=='parallel-top-left-half-pixel-rgba8-normal-opacity-v1'
    width,height=blueprint['Width'],blueprint['Height'];assert (native['Width'],native['Height'])==(width,height)
    root,=nodes(raw[native['path']]);version,=root[1];parts=dict(version[1]);vertices=parts[b'VBS '][1:]
    destination=np.zeros((height,width,4),dtype=np.uint8);layers=[];main=str(0x4d41494e)
    for render in blueprint['RenderSets']:
        slot=prepared['ShaderSlots'][render['ShaderIndex']];shader_root,=nodes(raw[slot['ShaderPath']]);shader_version,=shader_root[1];shader=dict(shader_version[1])
        texture_map,=shader[b'TXMS'];texture_version,=texture_map[1];binding=dict(texture_version[1])[b'DATA'];assert binding[8:11]==bytes((1,2,2))
        effect_version,=shader[b'EFCT'];impl=effect_version[1][1];impl_version,=impl[1];pass_tag,pass_children=impl_version[1][-1];assert pass_tag==b'PASS'
        pass_version,=pass_children;state=dict(pass_version[1])[b'DATA'];blend=bool(state[7]);assert state[7] in (0,1)
        factor=slot['TextureFactors'][main];factor_rgb=np.array([(factor>>16)&255,(factor>>8)&255,factor&255],dtype=np.float64)
        path=slot['TextureBindings'][main];source=raw[path];source_h,source_w=struct.unpack_from('<2I',source,12);mip_count=struct.unpack_from('<I',source,28)[0]
        for index in render['VertexArrayIndices']:
            tag,vertex=vertices[index];assert tag==b'VTXA';vertex_version,=vertex;data=dict(vertex_version[1])[b'DATA'];values=list(struct.iter_unpack('<5f',data))
            assert len(values)==4 and all(v[2]==3 for v in values)
            left,top=values[0][:2];right,bottom=values[2][:2]
            assert values[1][:2]==(right,top) and values[3][:2]==(left,bottom)
            assert [v[3:] for v in values]==[(0,0),(1,0),(1,1),(0,1)]
            scalar=blueprint['CameraScalar'];left,right=left/scalar*width,right/scalar*width;top,bottom=top/scalar*height,bottom/scalar*height
            xs=np.arange(width,dtype=np.float64)+.5;ys=np.arange(height,dtype=np.float64)+.5
            columns=np.where((xs>=left)&(xs<right))[0];rows=np.where((ys>=top)&(ys<bottom))[0]
            u=(xs[columns]-left)/(right-left);v=(ys[rows]-top)/(bottom-top)
            lod=min(mip_count-1,max(0,math.floor(math.log2(max(1,source_w/(right-left),source_h/(bottom-top)))+.5)))
            pixels=mip_pixels(source,lod);h,w=pixels.shape[:2];sx=u*w-.5;sy=v*h-.5;ix=np.floor(sx).astype(np.int64);iy=np.floor(sy).astype(np.int64);tx=sx-ix;ty=sy-iy
            def address(value,size,mode):
                assert mode in (0,2)
                return np.clip(value,0,size-1) if mode==2 else value%size
            x0,x1=address(ix,w,binding[5]),address(ix+1,w,binding[5]);y0,y1=address(iy,h,binding[6]),address(iy+1,h,binding[6])
            tx=tx[None,:,None];ty=ty[:,None,None]
            sample=(1-ty)*((1-tx)*pixels[y0[:,None],x0[None,:]]+tx*pixels[y0[:,None],x1[None,:]])+ty*((1-tx)*pixels[y1[:,None],x0[None,:]]+tx*pixels[y1[:,None],x1[None,:]])
            tinted=sample.copy();tinted[:,:,:3]=sample[:,:,:3]*factor_rgb/255
            weight=sample[:,:,3:4]/255 if blend else 1
            previous=destination[rows[:,None],columns[None,:]]
            destination[rows[:,None],columns[None,:]]=np.clip(np.floor(tinted*weight+previous*(1-weight)+.5),0,255).astype(np.uint8)
            digest=hashlib.sha256(destination.tobytes()).hexdigest().upper()
            layers.append(dict(ShaderIndex=render['ShaderIndex'],VertexArrayIndex=index,TexturePath=path,MipLevels=[lod,lod],WrittenPixels=len(rows)*len(columns),RgbaSha256=digest))
    for expected,actual in zip(layers,native['Layers']):assert actual==expected,(native['requested'],expected,actual)
    assert len(layers)==len(native['Layers']) and native['sha256']==layers[-1]['RgbaSha256']
    return dict(original_face_composition=True,requested=native['requested'],layers=len(layers),pixels=width*height,rgba_bytes=destination.size,agreement=True,legacy_driver_parity=False)


def verify_bindings(raw,mesh_paths,compositions,native):
    expected=[]
    for path in mesh_paths:
        root,=nodes(raw[path]);version,=root[1];shaders=[dict(parts)[b'NAME'][:-1].decode('ascii').lower().replace('\\','/') for tag,parts in version[1] if tag==b'PSDT']
        for tag,parts in version[1]:
            if tag!=b'TRTS':continue
            for chunk,data in parts[1:]:
                assert chunk==b'TRT ';end=data.index(0);renderer=data[:end].decode('ascii').lower().replace('\\','/');count=struct.unpack_from('<I',data,end+1)[0]
                assert len(data)==end+5+count*8
                digest,=[row['sha256'] for row in compositions if row['path']==renderer and row['requested']==0]
                for shader,texture in struct.iter_unpack('<2I',data[end+5:]):
                    assert texture==0x4d41494e
                    expected.append(dict(MeshPath=path,ShaderIndex=shader,TextureTag=texture,RendererPath=renderer,ShaderPath=shaders[shader],sha256=digest))
    assert sorted(native,key=lambda row:(row['MeshPath'],row['ShaderIndex'],row['TextureTag']))==sorted(expected,key=lambda row:(row['MeshPath'],row['ShaderIndex'],row['TextureTag']))
    return dict(original_generated_face_bindings=len(expected),raw_trts_agreement=True)
