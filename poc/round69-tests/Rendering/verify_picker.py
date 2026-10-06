"""Independent three-location Shift ownership and aggregate-boolean timeline, before native."""
import argparse,hashlib,json,os,stat
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
def fixtures():
 actions=[]
 def key(location,pressed=True,physical='Shift',logical=None,echo=False):actions.append(dict(Type='Key',Location=location,Physical=physical,Logical=logical or physical,Pressed=pressed,Echo=echo))
 def op(t,**kw):actions.append(dict(Type=t,**kw))
 key('Left');key('Right');key('Left',False);key('Unspecified');key('Right',False);key('Unspecified',False);key('Right',False);key('Left',echo=True);key('Left',physical='W',logical='Shift')
 key('Left',physical='None',logical='Shift');key('Right',physical='None',logical='Shift');key('Left',False,physical='None',logical='Shift');key('Right',False,physical='None',logical='Shift')
 op('Gui',Enabled=True);key('Left');key('Left',False);op('Gui',Enabled=False)
 key('Left');op('Enable',Enabled=False);key('Right');op('Enable',Enabled=False);op('Enable',Enabled=True);key('Right',False)
 key('Unspecified');op('FocusOut');op('FocusIn');op('Enable',Enabled=True);key('Right');key('Left');key('Unspecified',False);key('Right',False);op('FocusOut')
 op('Gui',Enabled=True);key('Right');key('Right',False);op('Gui',Enabled=False);op('Enable',Enabled=True);key('Left');key('Right');op('Enable',Enabled=False);op('Enable',Enabled=True);key('Left',False);op('Motion');key('Unspecified');key('Unspecified',False);op('Enable',Enabled=False)
 held=0;enabled=True;gui=False;accepted=handled=reads=calls=gui_count=0;rows=[];sinks=[];locations={'Left':1,'Right':2,'Unspecified':4}
 def submit(next_held,clock,kind):
  nonlocal accepted,reads,calls
  accepted+=1
  if bool(next_held)!=bool(held):reads+=1;calls+=1;sinks.append(dict(Running=bool(next_held),Clock=clock,Kind=kind))
 for i,a in enumerate(actions):
  clock=(i+1)*1000;consumed=None;t=a['Type']
  if t=='Key':
   consumed=False
   if gui:gui_count+=1;consumed=True
   elif enabled and not a['Echo']:
    code=a['Physical'] if a['Physical']!='None' else a['Logical']
    if code=='Shift':
     bit=locations[a['Location']];next_held=(held|bit) if a['Pressed'] else held&~bit
     if next_held!=held:submit(next_held,clock,'Key');held=next_held;handled+=1;consumed=True
  elif t=='Gui':gui=a['Enabled']
  elif t=='Motion':consumed=False
  elif t in ('Enable','FocusOut'):
   value=a['Enabled'] if t=='Enable' else False
   if enabled!=value:
    if not value and held:submit(0,clock,'Disabled' if t=='Enable' else 'FocusLost')
    held=0;enabled=value
  rows.append(dict(Index=i,HeldMask=held,Running=bool(held),Enabled=enabled,Accepted=accepted,Handled=handled,ClockReads=reads,SinkCalls=calls,GuiCaptured=gui_count,Consumed=consumed))
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
  (out/'run-fixtures.json').write_text(json.dumps(f));(out/'run-expected.json').write_text(json.dumps(e));print('PASS independent timeline before native '+str(e['EventCount']));return
 n=json.loads(a.native.read_text());assert n['Rows']==e['Rows'];assert n['Sinks']==e['Sinks'];assert n['NativeChecks']>=30 and all(n[x] for x in ('BorrowedSurvival','ClockPhasePrevented','SinkPrefixPreserved','GuiConsumed','FocusStopped','Cleanup'))
 r=dict(Passed=True,IndependentExpectedBeforeNative=True,NativeSeededExpectedValues=False,ComparedEvents=e['EventCount'],ComparedSinkCalls=e['SinkCount'],ComparedMaskTransitions=e['Rows'][-1]['Accepted'],HandledKeys=e['Rows'][-1]['Handled'],NativeChecks=n['NativeChecks'],Gpu=n['Gpu'],ClockExactInteger=True,IndependentLocations=True)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))
if __name__=='__main__':main()
