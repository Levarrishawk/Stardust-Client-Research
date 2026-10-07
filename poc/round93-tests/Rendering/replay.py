"""Explicit original-resource production-host controls with authored offline peers; no physical OS input."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import struct
import sys

ROOT = Path(__file__).resolve().parents[3]


def main():
    spec = importlib.util.spec_from_file_location('bounded_runner', ROOT / 'tools/python/run_client_regressions.py')
    runner = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(runner)
    p = argparse.ArgumentParser()
    for name in ('dotnet', 'godot', 'packages', 'asset-root', 'config', 'output', 'python'):
        p.add_argument('--' + name, required=True)
    p.add_argument('--gpu',action='store_true')
    a = p.parse_args()
    paths = {name: runner.local_path(getattr(a, name.replace('-', '_'))) for name in ('dotnet', 'godot', 'packages', 'asset-root', 'config', 'python')}
    for path in paths.values():
        runner.no_links(path)
        if not path.exists():
            raise ValueError('Explicit input missing')
    out = runner.fresh_output(a.output, ROOT)
    out.mkdir(parents=True)
    stage = out / 'source'
    listing = subprocess.check_output(['git', '-C', str(ROOT), 'ls-files', '-co', '--exclude-standard', '-z']).decode().split('\0')
    hashes = {}
    prefixes = ('poc/Stardust.Core/', 'poc/Stardust.Godot/', 'tools/',
                'poc/round14-tests/Networking/', 'poc/round15-tests/Networking/', 'poc/round83-tests/Runtime/', 'poc/round83-tests/Assets/',
                'poc/round85-tests/Networking/', 'poc/round84-tests/Networking/', 'poc/round25-tests/Networking/', 'poc/round32-tests/Networking/', 'poc/round38-tests/Networking/', 'poc/round41-tests/Networking/', 'poc/round42-tests/Runtime/', 'poc/round37-tests/Networking/', 'poc/round84-tests/Assets/', 'poc/round90-tests/Rendering/', 'poc/round92-tests/Integration/', 'poc/round92-tests/Rendering/', 'poc/round93-tests/Rendering/', 'poc/round86-tests/Networking/', 'poc/round87-tests/Networking/')
    for name in sorted(set(listing)):
        if not name.startswith(prefixes) or any(part in ('bin', 'obj', '.godot', '__pycache__') for part in Path(name).parts):
            continue
        source = ROOT / name
        runner.no_links(source)
        if not source.is_file():
            raise ValueError('Source missing')
        raw = source.read_bytes()
        hashes[name] = hashlib.sha256(raw).hexdigest()
        target = stage / name
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(raw)
    project = stage / 'poc/Stardust.Godot'
    for directory,names in (('poc/round93-tests/Rendering',('StandaloneVerification.cs','StandaloneVerification.tscn')),('poc/round92-tests/Rendering',('BootstrapVerification.cs','BootstrapVerification.tscn'))):
        for name in names:
            (project/name).write_bytes((stage/directory/name).read_bytes())
    observation = stage / 'poc/round85-tests/Networking/Observation.cs'
    # The emitter only projects observed values; Python owns the expectations.
    (project / 'Networking85Observation.cs').write_bytes(observation.read_bytes())
    (project / 'Networking86Observation.cs').write_bytes((stage / 'poc/round86-tests/Networking/Observation.cs').read_bytes())
    packages = out / 'packages'
    for child in paths['packages'].rglob('*'):
        runner.no_links(child)
    shutil.copytree(paths['packages'], packages)
    sdk_source = paths['godot'].parent / 'GodotSharp/Tools/nupkgs'
    if not sdk_source.is_dir():
        raise ValueError('Bundled Godot SDK packages missing')
    config = out / 'NuGet.Config'
    from xml.sax.saxutils import escape
    config.write_text('<configuration><packageSources><clear/><add key="bundled" value="' + escape(str(sdk_source), {'"': '&quot;'}) + '"/></packageSources></configuration>\n')
    appdata = out / 'appdata/NuGet'
    appdata.mkdir(parents=True)
    shutil.copyfile(config, appdata / 'NuGet.Config')
    env = os.environ.copy()
    env.update(APPDATA=str(out / 'appdata'), DOTNET_CLI_HOME=str(out / 'dotnet-home'), NUGET_PACKAGES=str(packages),
               DOTNET_GENERATE_ASPNET_CERTIFICATE='false', DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',
               MSBUILDDISABLENODEREUSE='1', PYTHONUTF8='1', PYTHONDONTWRITEBYTECODE='1')
    commands = []
    (out / 'source-manifest.json').write_text(json.dumps(hashes, indent=2) + '\n')

    def run(command, name, expected_exit=0):
        result = runner.run_command([str(x) for x in command], stage, env, out / (name + '.log'), 180)
        commands.append(result)
        (out / 'commands.json').write_text(json.dumps(commands, indent=2) + '\n')
        result['ExpectedExitCode']=expected_exit
        result['ExpectedOutcomePassed']=not result['timedOut'] and result['exitCode']==expected_exit
        (out / 'commands.json').write_text(json.dumps(commands, indent=2) + '\n')
        if not result['ExpectedOutcomePassed']:
            raise RuntimeError(name + ': ' + result['status'])

    object_path = 'object/creature/player/shared_human_male.iff'
    # Use the independently authored preparation peer's CRC implementation, not Core.
    peer_spec = importlib.util.spec_from_file_location('installation_peer', stage / 'poc/round84-tests/Assets/peer.py')
    peer = importlib.util.module_from_spec(peer_spec)
    peer_spec.loader.exec_module(peer)
    template_crc = peer.preflight.crc(object_path.encode())
    run([paths['python'], stage / 'poc/round84-tests/Assets/peer.py', 'generate', stage / 'local-private/installation-oracle',
         '--asset-root', paths['asset-root'], '--config', paths['config']], 'installation-oracle')
    expected_preparation = json.loads((stage / 'local-private/installation-oracle/fixtures.json').read_text())['Original']['Expected']
    run([paths['python'], stage / 'poc/round92-tests/Rendering/produce.py',
         '--template-crc', hex(template_crc), '--object-id', '0x1122334455667788',
         '--object-path', object_path, '--terrain-path', 'terrain/tatooine.trn',
         '--output', stage / 'local-private/transcript'], 'producer')
    profile = dict(Version=1, AssetRoot=str(paths['asset-root']), ConfigurationPath=str(paths['config']),
                   ObjectPath=object_path,
                   Idle=dict(TablePath='appearance/lat/all_m.lat',LogicalName='emt_standing_idle_ambient_1_ag'),
                   Walk=dict(TablePath='appearance/lat/all_m.lat',LogicalName='loop_standing',ExplicitChildRoute=[0,1]),
                   Run=dict(TablePath='appearance/lat/all_m.lat',LogicalName='loop_standing',ExplicitChildRoute=[0,2]))
    generated = json.loads((stage/'local-private/transcript/transcript.json').read_text())
    profile = dict(Version=1, Profile=profile, ClientVersion='20260817-SD:01',
                   Login=dict(Hostname='login.invalid',Ipv4Address=0xC0000201,Port=44452),
                   Zone=dict(Hostname='zone.invalid',Ipv4Address=0xC0000202,Port=44453),
                   GalaxyId=7,ApprovedCharacterId=0x1122334455667788,LoginConnectionId=generated['LoginId'],ZoneConnectionId=generated['ZoneId'],
                   PhaseBudgetMilliseconds=30000,OverallBudgetMilliseconds=120000,DisconnectTimeoutMilliseconds=1000,
                   MinimumReadinessApproved=True,MovementPrerequisites=dict(FreshStampZero=True,MatchingValidatedPosition=True,MovementPrerequisitesValidated=True))
    profile_path = stage / 'local-private/controlled-launch.json'
    profile_path.write_text(json.dumps(profile,indent=2)+'\n',encoding='utf-8')
    profile_hash = hashlib.sha256(profile_path.read_bytes()).hexdigest().upper()
    transcript = stage / 'local-private/transcript/transcript.json'
    run([paths['dotnet'], 'build', project / 'Stardust.Godot.csproj', '--configfile', config,
         '-p:NuGetAudit=false', '-p:UseSharedCompilation=false', '-p:UseAppHost=false', '-p:TreatWarningsAsErrors=true', '--nologo'], 'build')
    run([paths['godot'], '--headless', '--path', project, '--editor', '--import', '--quit'], 'import')
    # Real UDP branch is compiled/source-reviewed only. Every admitted native scene uses the mandatory spy seam.
    source=(project/'ReplacementClientBootstrap.cs').read_text()
    if source.count('new UdpDatagramPort()')!=1 or 'if (syntheticStandalone) return factory!(connection, peer);' not in source:
        raise ValueError('Reviewed exclusive UDP/fake branch shape changed')
    for label,args in (('default',[str(profile_path)]),('missing',[]),('relative',['relative.json']),('extra',[str(profile_path),'extra'])):
        run([paths['godot'],'--headless','--path',project,'--quit-after','20','res://ReplacementClientBootstrap.tscn','--',*args], 'direct-'+label)
        expected='RefusedNoTransport' if label=='default' else 'InvalidInput'
        if 'bootstrap_state='+expected not in (out/('direct-'+label+'.log')).read_text():
            raise ValueError('Direct refusal/rejection missing')
    modes=('success','movement-disabled','list-phase-timeout','readiness-phase-timeout','overall-timeout','startup-rollback','startup-rollback-fault','send-error','cleanup-fault','diagnostic-write-failure','cancel','window-close')
    receipts=[]
    expectations=json.loads(transcript.read_text())
    cases=[(mode,mode,True) for mode in modes]
    if a.gpu:cases.append(('success-gpu','success',False))
    for name,mode,headless in cases:
        target=out/name
        target.mkdir()
        case_profile=profile_path
        if mode=='movement-disabled':
            disabled=dict(profile)
            disabled.pop('MovementPrerequisites')
            case_profile=stage/'local-private/no-movement.json'
            case_profile.write_text(json.dumps(disabled))
        if mode=='success':
            def psquote(value):
                return "'"+str(value).replace("'","''")+"'"
            run(['powershell.exe','-NoProfile','-NonInteractive','-Command',
                 'New-Item -ItemType Junction -Path '+psquote(target/'linked')+' -Target '+psquote(target)+' | Out-Null'], 'private-junction-'+name)
        expected_exit=1 if mode in ('startup-rollback','startup-rollback-fault','send-error','cleanup-fault','diagnostic-write-failure') else 0
        run([paths['godot'],*(['--headless'] if headless else []),'--path',project,'res://StandaloneVerification.tscn','--',paths['asset-root'],paths['config'],transcript,target,mode,case_profile],name,expected_exit)
        observation=json.loads((target/'observer.json').read_text())
        if not observation['Passed'] or not observation['SyntheticOffline'] or observation['CompletionBeforeClose'] or observation['NegativeCases']<12:
            raise ValueError('Spy/pre-acquisition/owner proof failed')
        if not headless and (not observation['Screenshot'] or not (target/'standalone.png').is_file()):
            raise ValueError('Actual GPU screenshot missing')
        if mode=='success' and observation['NegativeCases']!=13:
            raise ValueError('Actual linked parent rejection missing')
        if mode=='diagnostic-write-failure':
            if (target/'attempt.json').read_text()!='sentinel':
                raise ValueError('Diagnostic destination overwritten')
            log=(out/(mode+'.log')).read_text()
            if 'bootstrap_state=Finalized diagnostic_state=Failed local_drain_completed=True' not in log:
                raise ValueError('Diagnostic/run/drain distinction missing')
            receipts.append(dict(Mode=mode,DiagnosticFailed=True,LocalDrainCompleted=True,Outcome='Finalized'))
            continue
        raw=(target/'attempt.json').read_bytes()
        receipt=json.loads(raw)
        if len(raw)>65536 or receipt['Version']!=1 or receipt['SyntheticOffline'] is not True or receipt['LocalDrainCompleted'] is not True:
            raise ValueError('Actual completed bounded receipt missing')
        for forbidden in ('synthetic','fixture','login.invalid','zone.invalid','192.0.2','C:/','C:\\','1122334455667788','Account','Password','Token','Hostname','ObjectId','StackTrace'):
            if forbidden in raw.decode():
                raise ValueError('Private receipt contains forbidden identity')
        if receipt['ControlledLaunchSha256']!=hashlib.sha256(case_profile.read_bytes()).hexdigest().upper() or receipt['ConfigurationSha256']!=hashlib.sha256(paths['config'].read_bytes()).hexdigest().upper():
            raise ValueError('Original input hashes mismatch')
        outcome={'startup-rollback':'StartupRejected','startup-rollback-fault':'RollbackFault','send-error':'RunFault','cleanup-fault':'CleanupFault'}.get(mode,'Finalized')
        if receipt['Outcome']!=outcome:
            raise ValueError('Typed outcome mismatch')
        if mode.startswith('startup-'):
            if not observation['RollbackPending'] or not receipt['StartupRollbackCompleted'] or receipt['StartupRollbackSucceeded']!=(mode=='startup-rollback'):
                raise ValueError('Actual delayed rollback completion mismatch')
        else:
            if not receipt['ApplicationFinalized'] or receipt['LocalDrainSucceeded']!=(mode!='cleanup-fault'):
                raise ValueError('Run/cleanup finalization mismatch')
            if mode=='send-error' and (receipt['OperationTaskSucceeded'] or receipt['CleanupFailureTypes']):
                raise ValueError('Protocol failure conflated with cleanup')
            if mode=='cleanup-fault' and receipt['CleanupFailureTypes']!=['IOException','InvalidOperationException','InvalidOperationException']:
                raise ValueError('Cleanup fault observation changed')
            if mode!='list-phase-timeout' and not observation['WrongSupportedIdRejected']:
                raise ValueError('Approved identity rejection missing')
        if mode.endswith('phase-timeout') and receipt['Attempt']['StopReason']!='PhaseTimeout':
            raise ValueError('Phase stop missing')
        if mode=='overall-timeout' and receipt['Attempt']['StopReason']!='OverallTimeout':
            raise ValueError('Overall stop missing')
        if mode=='list-phase-timeout' and observation['ZoneFactoryCalls']!=0:
            raise ValueError('List timeout acquired zone')
        expected_roots=3 if mode in ('success','send-error','cleanup-fault','window-close') else 0
        if receipt['RootsQueued']!=expected_roots or len(receipt['Stages'])>9 or len(receipt['OriginalResourceSha256'])>256:
            raise ValueError('Bounded roots/stages/resources mismatch')
        if mode=='movement-disabled' and not receipt['PlayerPublished']:
            raise ValueError('Missing approval case did not reach actual publication')
        if expected_roots:
            if [struct.pack('<3f',*v) for v in observation['Positions']] != [struct.pack('<3f',*v) for v in expectations['ExpectedActorLocalPositions']]:
                raise ValueError('Actual original actor positions mismatch')
            if len(observation['Times'])!=3 or any(b-a<400 for a,b in zip(observation['Times'],observation['Times'][1:])):
                raise ValueError('Actual controlled cadence mismatch')
        for key,digest in observation['WireDigests'].items():
            wanted=expectations['ExpectedMovementWires'][int(key[8:])] if key.startswith('Movement') else expectations['Expected'+key]
            if digest!=hashlib.sha256(bytes.fromhex(wanted)).hexdigest().upper():
                raise ValueError('Independent protected-wire mismatch')
        receipts.append(dict(Mode=mode,Case=name,GPU=not headless,**receipt))
    # One unchanged injected92 replay checks the existing caller-owned quit API.
    injected=out/'injected92'
    run([paths['godot'],'--headless','--path',project,'res://BootstrapVerification.tscn','--',paths['asset-root'],paths['config'],transcript,injected,'success',profile_path], 'injected92')
    injected_receipt=json.loads((injected/'receipt.json').read_text())
    if not injected_receipt['Passed']:
        raise ValueError('Accepted injected API regressed')
    sys.path.insert(0,str(stage/'tools/python'))
    from verify_textured_mesh_bundle import Index
    from pst_reference_audit import configured_archives
    import contextlib
    success=next(r for r in receipts if r['Mode']=='success')
    resources=json.loads((injected/'resources.json').read_text())
    if resources['Preparation']!=expected_preparation:
        raise ValueError('Independent original preparation mismatch')
    with contextlib.ExitStack() as stack:
        indexes=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in configured_archives(paths['asset-root'],paths['config'])]
        def read(logical):
            for priority,index in indexes:
                value=index.lookup(logical)
                if value is not None:return value,priority
            raise ValueError('Original source absent')
        original_hashes=set()
        for logical,identity in resources['ClosureSources'].items():
            raw,priority=read(logical)
            sha=hashlib.sha256(raw).hexdigest().upper();original_hashes.add(sha)
            if identity!=dict(Path=logical,Priority=priority,Bytes=len(raw),Sha256=sha):
                raise ValueError('Original loaded closure mismatch')
        original_hashes.update(identity['Sha256'] for identity in resources['Preparation']['Sources'].values())
        for actual_receipt in (r for r in receipts if r['Mode']=='success'):
            if set(actual_receipt['OriginalResourceSha256'])!=original_hashes:
                raise ValueError('Standalone headless/GPU redacted closure hashes mismatch')
        terrain,_=read('terrain/tatooine.trn')
        for actual_receipt in (r for r in receipts if r['Mode']=='success'):
            if actual_receipt['TerrainSha256']!=hashlib.sha256(terrain).hexdigest().upper() or actual_receipt['TerrainBytes']!=len(terrain):
                raise ValueError('Actual headless/GPU production original minimum mismatch')
    for name,pin in hashes.items():
        if hashlib.sha256((ROOT/name).read_bytes()).hexdigest()!=pin:
            raise ValueError('Public source changed during replay')
    (out/'verification.json').write_text(json.dumps(dict(Passed=True,SourceHashes=hashes,Commands=commands,Receipts=receipts,Injected92=injected_receipt,DirectStartupCases=4,SyntheticOffline=True,RealUdpBranch='source-reviewed-unexecuted',SocketCreated=False,GPU=dict(Requested=a.gpu,Status='passed' if a.gpu else 'skipped-unverified',Accepted=a.gpu),LiveCompatibility=False,FullTerrainReadiness=False),indent=2)+'\n')
    print(json.dumps(dict(Passed=True,Cases=len(receipts),Commands=len(commands),SocketCreated=False,Output=str(out))))

if __name__=='__main__':main()
