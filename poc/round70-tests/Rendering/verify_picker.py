"""Independent orbit clamp, binary32 pose, look basis and perspective ray model before native."""
import argparse,hashlib,json,math,os,stat,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
def f(v):return struct.unpack('<f',struct.pack('<f',v))[0]
def unit(v):n=math.hypot(*v);return [x/n for x in v]
def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def pose(y,p,d,pivot):
 pos=[f(pivot[0]+d*math.sin(y)*math.cos(p)),f(pivot[1]+d*math.sin(p)),f(pivot[2]+d*math.cos(y)*math.cos(p))]
 back=unit([pos[i]-pivot[i] for i in range(3)]);right=unit(cross([0,1,0],back));up=cross(back,right);return dict(Position=pos,Basis=[right,up,back])
def fixtures():
 actions=[]
 def motion(x,y,right=True):actions.append(dict(Type='Motion',X=x,Y=y,Right=right))
 def wheel(up=True,pressed=True):actions.append(dict(Type='Wheel',Up=up,Pressed=pressed))
 def op(t,**kw):actions.append(dict(Type=t,**kw))
 motion(30,-10,False);motion(12,-8);motion(0,0);motion(10,10,False);wheel(pressed=False);wheel();wheel(False);motion(-50,20);motion(0,1000);motion(0,50)
 for _ in range(7):wheel()
 for _ in range(15):wheel(False)
 op('Gui',Enabled=True);motion(20,8);wheel();op('Gui',Enabled=False);op('FocusOut');motion(20,8);op('FocusIn');op('Enable',Enabled=True);motion(30,-1000);wheel(False);op('Enable',Enabled=False);wheel();op('Enable',Enabled=True);wheel();motion(2000,40)
 pivot=[3,2,-4];policy=dict(MinimumPitchRadians=.1,MaximumPitchRadians=1.2,MinimumDistance=2,MaximumDistance=20,RadiansPerPixel=.01,WheelDistanceStep=1.5)
 y,p,d=3.,.4,8.;enabled=True;gui=False;accepted=handled=captured=0;rows=[]
 for i,a in enumerate(actions):
  t=a['Type'];consumed=None;ny,np,nd=y,p,d
  if t in ('Motion','Wheel'):
   consumed=False
   if gui:captured+=1;consumed=True
   elif enabled:
    if t=='Motion' and a['Right']:ny=math.remainder(y-f(a['X'])*policy['RadiansPerPixel'],2*math.pi);np=min(policy['MaximumPitchRadians'],max(policy['MinimumPitchRadians'],p+f(a['Y'])*policy['RadiansPerPixel']))
    elif t=='Wheel' and a['Pressed']:nd=min(policy['MaximumDistance'],max(policy['MinimumDistance'],d+(-1 if a['Up'] else 1)*policy['WheelDistanceStep']))
    if (ny,np,nd)!=(y,p,d):y,p,d=ny,np,nd;accepted+=1;handled+=1;consumed=True
  elif t=='Gui':gui=a['Enabled']
  elif t=='Enable':enabled=a['Enabled']
  elif t=='FocusOut':enabled=False
  rows.append(dict(Index=i,Yaw=y,Pitch=p,Distance=d,Enabled=enabled,Accepted=accepted,Handled=handled,Attempts=accepted+1,Revision=accepted+1,GuiCaptured=captured,Consumed=consumed,**pose(y,p,d,pivot)))
 return dict(Pivot=pivot,Policy=policy,Yaw=3.,Pitch=.4,Distance=8.,Actions=actions),dict(Rows=rows,Initial=pose(3,.4,8,pivot),EventCount=len(actions))
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args();assert hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()==EXE
 out=Path(os.path.abspath(a.out));assert out.is_relative_to(ROOT/'local-private')
 for ancestor in (out,*out.parents):
  try:s=ancestor.lstat()
  except FileNotFoundError:continue
  assert not stat.S_ISLNK(s.st_mode) and not getattr(s,'st_file_attributes',0)&0x400 and stat.S_ISDIR(s.st_mode)
 fixture,expected=fixtures()
 if not a.native:(out/'orbit-fixtures.json').write_text(json.dumps(fixture));(out/'orbit-expected.json').write_text(json.dumps(expected));return
 n=json.loads(a.native.read_text());assert len(n['Rows'])==len(expected['Rows']);raychecks=0
 for got,want in zip(n['Rows'],expected['Rows']):
  for key in ('Index','Enabled','Accepted','Handled','Attempts','Revision','GuiCaptured','Consumed'):assert got[key]==want[key],(key,got,want)
  for key in ('Yaw','Pitch','Distance'):assert abs(got[key]-want[key])<1e-12
  for x,y in zip(got['Position'],want['Position']):assert abs(x-y)<2e-5
  for gv,wv in zip(got['Basis'],want['Basis']):
   for x,y in zip(gv,wv):assert abs(x-y)<3e-5
  viewport=got['Viewport'];pixel=got['Pixel'];x=2*pixel[0]/viewport[0]-1;y=1-2*pixel[1]/viewport[1];t=math.tan(math.radians(50)/2);local=unit([x*viewport[0]/viewport[1]*t,y*t,-1]);direction=[sum(want['Basis'][j][i]*local[j] for j in range(3)) for i in range(3)]
  for v,w in zip(got['RayOrigin'],want['Position']):assert abs(v-w)<2e-5
  for v,w in zip(got['RayDirection'],direction):assert abs(v-w)<3e-5
  raychecks+=1
 assert all(n[k] for k in ('BorrowedSurvival','MutationPrefix','GuiConsumed','FocusDisabled','Cleanup')) and n['NativeChecks']>=30
 r=dict(Passed=True,IndependentBeforeNative=True,NativeSeededExpectedValues=False,ComparedEvents=len(n['Rows']),ComparedRays=raychecks,AcceptedChanges=expected['Rows'][-1]['Accepted'],NativeChecks=n['NativeChecks'],Gpu=n['Gpu'],PositionTolerance=2e-5,BasisRayTolerance=3e-5)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))
if __name__=='__main__':main()
