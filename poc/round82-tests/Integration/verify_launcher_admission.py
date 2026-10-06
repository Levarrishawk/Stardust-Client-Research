"""Four native launcher admission rejections; requires an already built offline host."""
import argparse,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]

def main(a):
    target=a.output.resolve()
    if target.exists() or not target.is_relative_to((ROOT/'local-private').resolve()):raise ValueError('Fresh private output required')
    original=a.scenario.read_bytes();value=json.loads(original)
    target.mkdir(parents=True)
    value['PointTargetProfiles']=[str((a.scenario.parent/p).resolve()) if not Path(p).is_absolute() else p for p in value['PointTargetProfiles']]
    value['Inputs']['asset-config']=str(target/'never-created.cfg')
    invalid=target/'missing-input.json';invalid.write_text(json.dumps(value,separators=(',',':'))+'\n')
    cases=[('missing-input',['--offline-scenario',str(invalid)]),
           ('mixed-overrides',['--offline-scenario',str(a.scenario.resolve()),'--offline-restart-arguments','ignored.json']),
           ('resource-uri',['--offline-scenario','res://authored-negative.json']),
           ('missing-path',['--offline-scenario'])]
    (target/'expected-before-native.json').write_text(json.dumps(dict(Cases=[c[0] for c in cases],ExitCode=1,NoPreviewAdmission=True,NativeOutputUsed=False))+'\n')
    results=[]
    for name,args in cases:
        log=target/(name+'.log');console=target/(name+'-console.log')
        with console.open('w',encoding='utf-8') as stream:
            result=subprocess.run([str(a.godot.resolve()),'--headless','--path',str(ROOT/'poc/Stardust.Godot'),
                '--log-file',str(log),str(ROOT/'poc/round81-tests/Integration/InteractivePlayground.tscn'),'--',*args],
                stdout=stream,stderr=subprocess.STDOUT,timeout=60)
        text=log.read_text(encoding='utf-8-sig')
        assert result.returncode==1 and 'Offline playground launcher:' in text
        assert all(marker not in text for marker in ('persistent_preview_admitted=','offline_fresh_preview_state=','soe_root_transport_ready=','point_target_ready='))
        results.append(dict(Case=name,ExitCode=1,NoPreviewAdmission=True))
    assert a.scenario.read_bytes()==original
    receipt=dict(Passed=True,ActualNativeLauncher=True,Cases=results,SourceScenarioUnchanged=True,OriginalExecution=False,SocketCreated=False)
    (target/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    for key in ('godot','scenario','output'):parser.add_argument('--'+key,type=Path,required=True)
    main(parser.parse_args())
