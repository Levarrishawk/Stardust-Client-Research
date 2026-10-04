"""Four synthetic padded coordinate chunks with a supplied exact dyadic slope."""
import json,struct,sys
from fractions import Fraction
from pathlib import Path

def word(value):return struct.unpack('<I',struct.pack('<f',float(value)))[0]
OFFSETS=[(1,1),(0,1),(0,2),(1,2),(2,2),(2,1),(2,0),(1,0),(0,0)]
INDICES=[0,1,2,0,2,3,0,3,4,0,4,5,0,5,6,0,6,7,0,7,8,0,8,1]
def generate():
 chunks=[];expected=[]
 for first,second in [(-1,-1),(0,-1),(-1,0),(0,0)]:
  request=dict(ChunkWidthBits=0x41000000,Tiles=2,Margin=3,First=first,Second=second,Scale=1)
  # Explicit24 profiles and these exact small operands need no inexact arithmetic.
  count=10;xs=[Fraction(first*8-6+2*i) for i in range(count)];zs=[Fraction(second*8-6+2*i) for i in range(count)]
  heights=[word(z/4) for z in zs for _ in xs]
  normal=[word(0),word(1),word(0)]
  chunks.append(dict(Request=request,HeightBits=heights,NormalBits=[normal[:] for _ in heights],Argb=[0xff0040c0]*len(heights),SeamMask=0,UvScale=1))
  patches=[]
  for row in range(2):
   for column in range(2):
    selected=[(3+2*row+dz)*count+3+2*column+dx for dx,dz in OFFSETS]
    positions=[[word(xs[k%count]),heights[k],word(zs[k//count])] for k in selected]
    patches.append(dict(Column=column,Row=row,Pattern=0,SourceIndices=selected,Positions=positions,NormalBits=[normal[:] for _ in selected],Argb=[0xff0040c0]*9,Indices=INDICES))
  expected.append(dict(Request=request,Count=count,PositionFirst=[word(x) for x in xs],PositionSecond=[word(z) for z in zs],Patches=patches))
 return dict(Version=1,Synthetic=True,Profiles=[24,24,24,24],HeightPolicy='supplied_native_z_divided_by_four',FootOffsetBits=0,Chunks=chunks,Expected=expected)
if __name__=='__main__':
 destination=Path(sys.argv[1]);destination.write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8')
 print(json.dumps(dict(Synthetic=True,Chunks=4,FullSamples=400,Patches=16,ActiveDomain=[-8,8],OriginalTerrainParity=False)))
