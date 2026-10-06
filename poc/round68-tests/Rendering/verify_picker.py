"""Independent physical/logical key event and native-axis reference, generated before native execution."""
import argparse,hashlib,json,os,stat,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
def bits(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def fixtures():
 actions=[]
 def key(p,pressed=True,logical=None,echo=False):actions.append(dict(Type='Key',Physical=p,Logical=logical or p,Pressed=pressed,Echo=echo))
 def op(t,**kw):actions.append(dict(Type=t,**kw))
 key('W');key('W',echo=True);key('W');key('D');key('S');key('A');key('W',False);key('D',False);key('A',False);key('S',False)
 key('X',logical='W');key('None',logical='W');key('None',False,logical='W')
 op('Gui',Enabled=True);key('W');key('W',False);op('Gui',Enabled=False)
 key('W');op('Enable',Enabled=False);op('Enable',Enabled=False);key('D');op('Enable',Enabled=True);key('D',False)
 key('A');op('FocusOut');key('W');op('FocusIn');op('Enable',Enabled=True);key('S');op('Enable',Enabled=False)
 op('Gui',Enabled=True);key('A');key('A',False);op('Gui',Enabled=False);op('Enable',Enabled=True)
 key('W');key('S');op('Enable',Enabled=False);op('Enable',Enabled=True);op('Motion');key('D');op('Enable',Enabled=False)
 held=0;enabled=True;gui=False;accepted=handled=reads=calls=gui_count=0;rows=[];sinks=[];mask={'W':1,'S':2,'A':4,'D':8}
 def axis(h):return [(1 if h&8 else 0)-(1 if h&4 else 0),(1 if h&2 else 0)-(1 if h&1 else 0)]
 def submit(h,clock,kind):
  nonlocal accepted,reads,calls
  accepted+=1;reads+=1;calls+=1;sinks.append(dict(DirectionBits=[bits(x) for x in axis(h)],Clock=clock,Kind=kind))
 for i,a in enumerate(actions):
  clock=(i+1)*1000;consumed=None;t=a['Type']
  if t=='Key':
   consumed=False
   if gui:gui_count+=1;consumed=True
   elif enabled and not a['Echo']:
    code=a['Physical'] if a['Physical']!='None' else a['Logical'];bit=mask.get(code,0)
    next_held=(held|bit) if a['Pressed'] else (held&~bit)
    if bit and next_held!=held:submit(next_held,clock,'Key');held=next_held;handled+=1;consumed=True
  elif t=='Gui':gui=a['Enabled']
  elif t=='Motion':consumed=False
  elif t in ('Enable','FocusOut'):
   value=a['Enabled'] if t=='Enable' else False
   if enabled!=value:
    if not value and axis(held)!=[0,0]:submit(0,clock,'Disabled' if t=='Enable' else 'FocusLost')
    held=0;enabled=value
  rows.append(dict(Index=i,HeldMask=held,DirectionBits=[bits(x) for x in axis(held)],Enabled=enabled,Accepted=accepted,Handled=handled,ClockReads=reads,SinkCalls=calls,GuiCaptured=gui_count,Consumed=consumed))
 return dict(Actions=actions),dict(Rows=rows,Sinks=sinks,EventCount=len(actions),SinkCount=len(sinks))
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args();assert hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()==EXE
 out=Path(os.path.abspath(a.out));assert out.is_relative_to(ROOT/'local-private')
 for ancestor in (out,*out.parents):
  try:s=ancestor.lstat()
  except FileNotFoundError:continue
  assert not stat.S_ISLNK(s.st_mode) and not getattr(s,'st_file_attributes',0)&0x400 and stat.S_ISDIR(s.st_mode)
 f,e=fixtures()
 if not a.native:
  (out/'manual-fixtures.json').write_text(json.dumps(f));(out/'manual-expected.json').write_text(json.dumps(e));print('PASS independent timeline before native '+str(e['EventCount']));return
 n=json.loads(a.native.read_text());assert n['Rows']==e['Rows'];assert n['Sinks']==e['Sinks'];assert n['NativeChecks']>=30 and n['BorrowedSurvival'] and n['ClockPhasePrevented'] and n['SinkPrefixPreserved'] and n['GuiConsumed'] and n['FocusStopped'] and n['Cleanup']
 r=dict(Passed=True,IndependentExpectedBeforeNative=True,NativeSeededExpectedValues=False,ComparedEvents=e['EventCount'],ComparedSinkCalls=e['SinkCount'],NativeChecks=n['NativeChecks'],Gpu=n['Gpu'],AxesExactF32=True,ClockExactInteger=True)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))
if __name__=='__main__':main()
