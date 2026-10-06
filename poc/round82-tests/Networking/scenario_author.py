"""Author one explicit modern offline scenario, without assets extraction or live I/O."""
import argparse
import hashlib
import importlib.util
import json
import os
import runpy
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
INPUTS = ('asset-root', 'asset-config', 'mixed-peer', 'catalog-profile', 'motion-peer',
          'face-profile', 'customization-peer', 'customization-dictionary',
          'customization-asset-context', 'customization-name-context', 'tangible-peer',
          'tangible6-peer', 'creature6-full-peer', 'creature4-partial-peer', 'terrain-peer',
          'terrain-material-profile')
OPTIONS = ('local-input', 'customization-wire', 'customization-scalars', 'customization-full',
           'customization-extended', 'customization-named', 'terminal-animation', 'terminal-clock')
PROFILE_KEY = 'terrain-point-target-profile'


def require(value, message):
    if not value:
        raise ValueError(message)


def unique(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, 'Duplicate JSON property: ' + key)
        result[key] = value
    return result


def depth(value, level=0):
    if type(value) in (list, dict):
        require(level < 4, 'JSON depth exceeds four containers')
        for child in value.values() if type(value) is dict else value:
            depth(child, level+1)


def read_json(path, cap):
    require(path.stat().st_size <= cap, 'Bounded JSON input required')
    with path.open('rb') as stream:
        raw = stream.read(cap+1)
    require(0 < len(raw) <= cap, 'Bounded JSON input required')
    text = raw.decode('utf-8-sig')
    # Bound parser nesting before handing data to json.loads; ignore quoted braces.
    nesting = 0; quoted = False; escaped = False
    for char in text:
        if quoted:
            if escaped: escaped = False
            elif char == '\\': escaped = True
            elif char == '"': quoted = False
        elif char == '"': quoted = True
        elif char in '[{':
            nesting += 1; require(nesting <= 4, 'JSON depth exceeds four containers')
        elif char in ']}': nesting -= 1
    value = json.loads(text, object_pairs_hook=unique,
                       parse_constant=lambda value: (_ for _ in ()).throw(ValueError('Nonfinite JSON number')))
    depth(value)
    return value, raw


def local_path(value, directory):
    require(type(value) is str and 0 < len(value) <= 4096 and '\0' not in value and value.strip(), 'Bounded nonempty path string required')
    require(not value.startswith(('\\\\', '//')), 'Network paths are not offline inputs')
    require(':' not in value or (value.find(':') == value.rfind(':') == 1 and
            value[0] in 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz' and
            len(value) >= 3 and value[2] in '\\/'), 'Only one rooted ASCII drive colon allowed')
    path = Path(value)
    path = path if path.is_absolute() else directory/path
    result = path.resolve()
    require(not str(result).startswith(('\\\\', '//')), 'Resolved network path rejected')
    require(len(str(result)) <= 4096, 'Resolved path exceeds bound')
    return result


def parse_base(value, directory):
    require(type(value) is list and 34 <= len(value) <= 42 and all(type(x) is str for x in value), 'Explicit bounded string argument array required')
    inputs = {}; options = {key: False for key in OPTIONS}; profile = None; seen = set(); cursor = 0
    while cursor < len(value):
        token = value[cursor]; cursor += 1
        require(token.startswith('--'), 'Named argument required')
        key = token[2:]
        require(key not in seen, 'Duplicate argument: ' + key); seen.add(key)
        if key in OPTIONS:
            options[key] = True
        else:
            require(key in INPUTS or key == PROFILE_KEY, 'Unknown argument: ' + key)
            require(cursor < len(value) and not value[cursor].startswith('--'), 'Incomplete value argument: ' + key)
            path = local_path(value[cursor], directory); cursor += 1
            require(path.is_dir() if key == 'asset-root' else path.is_file(), 'Missing or wrong-kind input: ' + key)
            if key == PROFILE_KEY:
                profile = path
            else:
                inputs[key] = str(path)
    require(set(inputs) == set(INPUTS) and profile is not None, 'Every required input and one point profile required')
    return {key: inputs[key] for key in INPUTS}, options, profile


def net_module():
    spec = importlib.util.spec_from_file_location('frozen81_scenario', ROOT/'poc/round81-tests/Networking/transport_fixtures.py')
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def encoded(value, cap):
    depth(value)
    raw = (json.dumps(value, ensure_ascii=True, allow_nan=False, separators=(',', ':'))+'\n').encode('utf-8')
    require(len(raw) <= cap, 'Serialized output exceeds byte cap')
    return raw


def sha_file(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1048576), b''):
            digest.update(block)
    return digest.hexdigest()


def create(base_argument_file, profile_file, runs, output):
    """Return producer receipt; validate everything before creating fresh private output.

    The explicit profile_file replaces the base's point-profile template. Both paths
    must exist and are recorded; no mutation of either file or caller arguments.
    """
    require(type(runs) is int and 1 <= runs <= 32, 'RunCount integer1..32 required')
    base_path = local_path(os.fspath(base_argument_file), Path.cwd())
    profile_path = local_path(os.fspath(profile_file), Path.cwd())
    require(base_path.is_file() and profile_path.is_file(), 'Explicit input files required')
    base, base_raw = read_json(base_path, 262144)
    inputs, options, base_profile = parse_base(base, base_path.parent)
    profile, profile_raw = read_json(profile_path, 16384)
    net = net_module()
    net.admit(profile)
    # frozen81.reseed checks every inherited protected field, not just admit's header.
    authored = [net.reseed(profile, (profile['SoeConnectionId']+i+1) & 0xffffffff,
                           (profile['SoeSeed']+i+1) & 0xffffffff) for i in range(runs)]
    require(len({p['SoeConnectionId'] for p in authored}) == runs and len({p['SoeSeed'] for p in authored}) == runs, 'Distinct synthetic identities required')
    target = Path(os.path.abspath(output))
    names = [f'run-{i:02d}.json' for i in range(1, runs+1)]
    profile_bytes = [encoded(p, 16384) for p in authored]
    manifest = dict(Schema=1, RunCount=runs, Inputs=inputs, Options=options, PointTargetProfiles=names)
    manifest_bytes = encoded(manifest, 262144)
    input_receipts = [{"Key": key, "Path": inputs[key], "Kind": 'Directory' if key == 'asset-root' else 'File',
                       "Sha256": None if key == 'asset-root' else sha_file(Path(inputs[key]))} for key in INPUTS]
    receipt = dict(Schema=1, Synthetic=True, RunCount=runs, BaseArguments=dict(Path=str(base_path), Sha256=hashlib.sha256(base_raw).hexdigest()),
                   SourceProfile=dict(Path=str(profile_path), Sha256=hashlib.sha256(profile_raw).hexdigest()),
                   BasePointProfile=dict(Path=str(base_profile), Sha256=sha_file(base_profile)), Inputs=input_receipts,
                   ScenarioSha256=hashlib.sha256(manifest_bytes).hexdigest(),
                   Profiles=[dict(Path=name, ConnectionId=p['SoeConnectionId'], Seed=p['SoeSeed'], Sha256=hashlib.sha256(raw).hexdigest(),
                                  **net.handshakes(p)) for name, p, raw in zip(names, authored, profile_bytes)],
                   Codec='FrozenRound81Reseed', SocketCreated=False, SourceMutation=False)
    receipt_bytes = encoded(receipt, 262144)
    # No mkdir/write has happened above, including validation of every profile/size.
    guard = runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    target = guard(target, ROOT, directory=True)
    for name, raw in zip(names, profile_bytes):
        with (target/name).open('xb') as stream:
            stream.write(raw)
    for name, raw in [('scenario.json', manifest_bytes), ('producer-receipt.json', receipt_bytes)]:
        with (target/name).open('xb') as stream:
            stream.write(raw)
    return receipt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base-arguments', type=Path, required=True)
    parser.add_argument('--profile', type=Path, required=True)
    parser.add_argument('--runs', type=int, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    receipt = create(args.base_arguments, args.profile, args.runs, args.output)
    print(json.dumps(dict(Passed=True, RunCount=receipt['RunCount'], Scenario=str(Path(args.output).absolute()/'scenario.json'),
                          ScenarioSha256=receipt['ScenarioSha256'])))


if __name__ == '__main__':
    main()
