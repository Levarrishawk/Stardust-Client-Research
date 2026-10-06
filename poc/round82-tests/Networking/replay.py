"""Independent admission/immutability/byte checks for the explicit scenario producer."""
import argparse
import copy
import hashlib
import json
import runpy
import struct
from pathlib import Path
import scenario_author as api


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--profile', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    out = runpy.run_path(str(api.ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](args.output, api.ROOT, directory=True)
    inputs = out/'inputs'; inputs.mkdir(); (inputs/'assets').mkdir()
    for key in api.INPUTS:
        if key != 'asset-root': (inputs/(key+'.txt')).write_text('authored synthetic input '+key+'\n')
    # No native scene loads these intentionally synthetic admission files.
    source = args.profile.read_bytes(); (inputs/'profile.json').write_bytes(source)
    p = json.loads(source.decode('utf-8-sig'))
    base = []
    for key in api.INPUTS: base += ['--'+key, 'assets' if key=='asset-root' else key+'.txt']
    base += ['--'+api.PROFILE_KEY, 'profile.json']
    basepath = inputs/'base.json'; basepath.write_text(json.dumps(base)); profilepath = inputs/'profile.json'
    baseline = {path: hashlib.sha256(path.read_bytes()).hexdigest() for path in inputs.iterdir() if path.is_file()}
    checks = 0; negatives = 0; serial = 0
    def check(value):
        nonlocal checks
        api.require(value, 'Test failure'); checks += 1
    def reject(base_value=None, profile_value=None, runs=2, raw_base=None, raw_profile=None, output=None):
        nonlocal negatives, serial
        serial += 1; badbase = inputs/f'bad-base-{serial}.json'; badprofile = inputs/f'bad-profile-{serial}.json'
        badbase.write_text(raw_base if raw_base is not None else json.dumps(base if base_value is None else base_value))
        badprofile.write_text(raw_profile if raw_profile is not None else json.dumps(p if profile_value is None else profile_value))
        target = out/f'rejected-{serial}' if output is None else output
        existed = target.exists()
        try: api.create(badbase, badprofile, runs, target)
        except (ValueError, TypeError, KeyError, OverflowError, OSError): negatives += 1
        else: raise ValueError('Expected rejection')
        check(target.exists() == existed)
        if not existed: check(not target.parent.exists() or target not in target.parent.iterdir())
    net = api.net_module()
    for runs, flags in [(1, []), (3, list(api.OPTIONS)), (32, ['terminal-clock'])]:
        explicit = base + ['--'+f for f in flags]; basepath.write_text(json.dumps(explicit))
        oldbase = basepath.read_bytes(); oldprofile = profilepath.read_bytes()
        target = out/f'accepted-{runs}'; receipt = api.create(basepath, profilepath, runs, target)
        manifestraw = (target/'scenario.json').read_bytes(); m = json.loads(manifestraw)
        check(set(m) == {'Schema','RunCount','Inputs','Options','PointTargetProfiles'} and type(m['Schema']) is int and m['Schema']==1 and m['RunCount']==runs)
        check(list(m['Inputs']) == list(api.INPUTS) and list(m['Options']) == list(api.OPTIONS))
        check(all(type(v) is bool and v==(k in flags) for k,v in m['Options'].items()))
        check(all(Path(v).is_absolute() and Path(v).parent==inputs or k=='asset-root' and Path(v)==inputs/'assets' for k,v in m['Inputs'].items()))
        check(receipt['ScenarioSha256']==hashlib.sha256(manifestraw).hexdigest())
        for i, name in enumerate(m['PointTargetProfiles'], 1):
            raw = (target/name).read_bytes(); new = json.loads(raw)
            expectedid = (p['SoeConnectionId']+i)&0xffffffff; expectedseed=(p['SoeSeed']+i)&0xffffffff
            check(new['SoeConnectionId']==expectedid and new['SoeSeed']==expectedseed)
            check({k:v for k,v in new.items() if not k.startswith('Soe')}=={k:v for k,v in p.items() if not k.startswith('Soe')})
            check(net.reseed(new,p['SoeConnectionId'],p['SoeSeed'])==p)
            check(receipt['Profiles'][i-1]['Sha256']==hashlib.sha256(raw).hexdigest())
            check(bytes.fromhex(receipt['Profiles'][i-1]['RequestHex'])==struct.pack('>HIII',1,2,expectedid,496))
            check(bytes.fromhex(receipt['Profiles'][i-1]['ResponseHex'])==struct.pack('>HII3BI',2,expectedid,expectedseed,2,1,4,496))
            wire=net.encoder(expectedseed)
            check(wire.open_frame(bytes.fromhex(new['SoeCorrectionDatagrams'][0]))==b'\0\x09\0\0'+bytes.fromhex(p['SoeCorrectionHex']))
            try: wire.open_frame(bytes.fromhex(p['SoeCorrectionAcks'][0]))
            except ValueError: check(True)
            else: raise ValueError('Stale source ACK admitted under new seed')
        check(basepath.read_bytes()==oldbase and profilepath.read_bytes()==oldprofile)
        check(set(x.name for x in target.iterdir())==set(m['PointTargetProfiles'])|{'scenario.json','producer-receipt.json'})
    basepath.write_text(json.dumps(base))
    wrap_profile=inputs/'wrap-profile.json'
    wrap_profile.write_text(json.dumps(net.reseed(p,0xffffffff,0xffffffff)))
    wrap=api.create(basepath,wrap_profile,2,out/'wrap-output')
    check([(r['ConnectionId'],r['Seed']) for r in wrap['Profiles']]==[(0,0),(1,1)])
    for runs in (0,33,True,1.,'2',None): reject(runs=runs)
    for value in ({},[],base[:-2],base+['--local-input','false'],base+['--local-input','--local-input'],base+['--unknown'],base+base[:2],base+['--'+api.PROFILE_KEY,'profile.json']): reject(base_value=value)
    bad=copy.deepcopy(base);bad[1]=True;reject(base_value=bad)
    for value in ('', 'x'*4097, 'https://example.invalid/a', 'res://thing', 'C:relative', 'C:/file:stream', '\\\\server\\share', 'bad\0path', 'missing-file'):
        bad=copy.deepcopy(base);bad[3]=value;reject(base_value=bad)
    bad=copy.deepcopy(base);bad[1]='asset-config.txt';reject(base_value=bad)
    bad=copy.deepcopy(base);bad[3]='assets';reject(base_value=bad)
    reject(raw_base='['*5000+']'*5000)
    reject(raw_base='["unterminated')
    reject(raw_base=' '*262145)
    for key,value in [('Version',12),('Version',True),('SoeSeed',p['SoeSeed']+1),('SpeedBits',p['SpeedBits']+1),('SoeRequestHex','00'),('SoeCorrectionAcks',['00','00'])]:
        bad=copy.deepcopy(p);bad[key]=value;reject(profile_value=bad)
    reject(raw_profile='{"Version":13,"Version":13}')
    bad=copy.deepcopy(p);bad['PlaygroundSettings']['Version']=True;reject(profile_value=bad)
    reject(raw_profile=json.dumps(p).replace('"Version": 1,','"Version": 1, "Version": 1,',1))
    reject(raw_profile='{"x":NaN}')
    reject(raw_profile=' '*16385)
    try: api.encoded({'value':'x'*262144},262144)
    except ValueError: negatives += 1
    else: raise ValueError('Serialized byte cap not enforced')
    reject(output=out/'accepted-1')
    reject(output=api.ROOT/'not-private-scenario')
    # A late serialized-size error also leaves neither output nor missing parent.
    old_encoded=api.encoded
    def too_big(value,cap):
        if type(value) is dict and 'PointTargetProfiles' in value: raise ValueError('Injected manifest byte-cap rejection')
        return old_encoded(value,cap)
    api.encoded=too_big
    try: reject(output=out/'absent-parent'/'not-created')
    finally: api.encoded=old_encoded
    for path,digest in baseline.items():
        if path!=basepath: check(hashlib.sha256(path.read_bytes()).hexdigest()==digest)
    check(args.profile.read_bytes()==source)
    receipt=dict(Passed=True,Checks=checks,Rejections=negatives,AuthoredAdmissionInputs=True,AcceptedRuns=[1,3,32],
                 OutputAbsentOnEveryValidationRejection=True,OldInputsUnchanged=True,NativeExecuted=False,
                 SourceSha256=hashlib.sha256(source).hexdigest())
    (out/'test-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))


if __name__=='__main__': main()
