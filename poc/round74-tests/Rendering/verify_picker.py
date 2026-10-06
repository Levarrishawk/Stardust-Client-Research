"""Independent fixed-slot traces, counts and failure prefixes before native; frozen actual pump inputs."""
import argparse,hashlib,json,os,stat,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
KINDS=['bad-count-negative','bad-count-large','bad-delta','schedule-throw','schedule-reentry','continue-reentry','before-reentry','observer-reentry','before-mutation','schedule-close','continue-close','before-close','pump-close','observer-close','before-throw','pump-throw','observer-throw','parent-change','node-exit','off-thread','second-before-throw','second-observer-cancel','null-configure-reentry','node-free','pump-reentry']
def fixtures():
 f=dict(Counts=[0,1,2,8],Deltas=[0,.016,.032,.2]);trace=[];rows=[];completed=0
 for index,count in enumerate(f['Counts']):
  trace.append('schedule:'+str(index))
  for _ in range(count):completed+=1;trace+=['continue','before:0.010000','pump:0.010000','after:'+str(completed)]
  rows.append(dict(Index=index,Completed=completed,StepCalls=completed,ActualPumpCalls=completed,LastSeconds=.01 if completed else None))
 failures=[]
 for kind in KINDS:
  schedule=0 if kind in ('bad-delta','off-thread') else 1
  before=1 if kind in ('before-reentry','observer-reentry','before-mutation','before-close','pump-close','observer-close','before-throw','pump-throw','observer-throw') else 0
  steps=1 if kind in ('observer-reentry','pump-close','observer-close','pump-throw','observer-throw') else 0
  actual=steps if kind!='pump-throw' else 0
  after=1 if kind in ('observer-reentry','observer-close','observer-throw') else 0
  failed=kind not in ('schedule-close','continue-close','before-close','pump-close','observer-close','parent-change','node-exit','node-free','off-thread')
  if kind=='second-before-throw':before=2;steps=actual=after=1
  if kind=='second-observer-cancel':before=steps=actual=after=2
  if kind=='pump-reentry':before=steps=actual=1
  failures.append(dict(Kind=kind,ScheduleCalls=schedule,Before=before,After=after,StepCalls=steps,ActualPumpCalls=actual,Completed=after,Failed=failed))
 return f,dict(Rows=rows,Trace=trace,Failures=failures)
def stage(out):
 frozen=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round74/frozen');raw=(frozen/'baseline.json').read_bytes();assert hashlib.sha256(raw).hexdigest()=='85118db10a4fc52080f514e0262201a8bdedc3200f6c7809b1b2d103e2040f1e';archive=frozen/'baseline.zip';assert hashlib.sha256(archive.read_bytes()).hexdigest()=='3ffc59188b6813da65e21b078f78fc71cbac75142f0787877f0e3a8672c6bbc6';rows=json.loads(raw)['files'];assert len(rows)==4404
 borrowers={'poc/Stardust.Godot/SceneWorldPresentationHostPreview.cs','poc/preview-profiles/scene-object-catalog.json','poc/preview-profiles/human-locomotion.json'};identities=[]
 with zipfile.ZipFile(archive) as z:
  assert len(z.namelist())==len(set(z.namelist()))==len({n.casefold() for n in z.namelist()})==4404 and set(z.namelist())=={r['path'] for r in rows}
  for row in rows:
   name=row['path'];assert ':' not in name and '\\' not in name and all(v not in ('','.','..') for v in name.split('/'))
   if not(name.startswith('poc/Stardust.Core/') or name in borrowers):continue
   entry=z.getinfo(name);assert not entry.is_dir() and ((entry.external_attr>>16)&0o170000)!=0o120000
   raw=z.read(name);assert hashlib.sha256(raw).hexdigest()==row['sha256'] and len(raw)==row['bytes']
   if name.startswith('poc/Stardust.Core/'):dest=out/'frozen-core'/Path(name).relative_to('poc/Stardust.Core')
   elif name.startswith('poc/preview-profiles/'):dest=out/'preview-profiles'/Path(name).name
   else:dest=out/Path(name).name
   dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(raw);identities.append(row)
 (out/'frozen-source-identity.json').write_text(json.dumps(identities,indent=2)+'\n')
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args();assert hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()==EXE
 out=Path(os.path.abspath(a.out));assert out.is_relative_to(ROOT/'local-private')
 for ancestor in (out,*out.parents):
  try:i=ancestor.lstat()
  except FileNotFoundError:continue
  assert not stat.S_ISLNK(i.st_mode) and not(getattr(i,'st_file_attributes',0)&0x400) and stat.S_ISDIR(i.st_mode)
 if not a.native:
  stage(out);f,e=fixtures();(out/'orbit-fixtures.json').write_text(json.dumps(f,indent=2)+'\n');(out/'fixed-expected.json').write_text(json.dumps(e,indent=2)+'\n');print('Independent fixed-slot expectations before native');return
 if a.native.suffix=='.log':
  lines=a.native.read_text().splitlines();line,=[v for v in lines if v.startswith('PASS: host proof ')];n=json.loads(line.split('PASS: host proof ',1)[1]);assert n['checks']>50 and n['pumps']==n['closes']==n['disposals'] and n['originalReads']==0 and n['concretePump'] is False
  r=dict(Passed=True,DefaultRegression=True,Checks=n['checks'],Pumps=n['pumps'],Steps=n['steps'],Closes=n['closes'],Disposals=n['disposals'],OriginalReads=0);a.native.with_name('regression-receipt.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r));return
 n=json.loads(a.native.read_text());e=json.loads((out/'fixed-expected.json').read_text());assert n['Passed'] and n['ActualPump'] and n['ConfigureRejections']==6 and n['EndpointSteps']==2 and n['EndpointGateCalls']==3
 for key in ('Rows','Trace','Failures'):assert n[key]==e[key],(key,n[key],e[key])
 r=dict(Passed=True,Checks=n['Checks'],Gpu=n['Gpu'],IndependentBeforeNative=True,NativeSeededExpectedValues=False,ComparedSchedules=4,ActualPumpSteps=11,FailurePrefixes=25,ConfigureRejections=6,EndpointSteps=2)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))
if __name__=='__main__':main()
