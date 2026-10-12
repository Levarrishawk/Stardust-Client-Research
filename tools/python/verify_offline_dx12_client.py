"""Verify an exported offline client, including actual D3D12, original animation and refusal cases."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import time

from build_offline_dx12_client import ROOT, digest, pe_x64


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--gpu', action='store_true')
    args = parser.parse_args()
    build = args.build.resolve(strict=True)
    output = args.output.resolve()
    output.relative_to((ROOT / 'local-private').resolve())
    if output.exists():
        raise ValueError('Fresh private output required')
    package = build / 'client'
    executable = package / 'Stardust.Client.exe'
    pe_x64(executable)
    receipt = json.loads((build / 'build-receipt.json').read_text())
    for name, expected in receipt['PackageFiles'].items():
        if digest(package / name) != expected['Sha256']:
            raise ValueError('Frozen package identity changed')
    data, = [path for path in package.glob('data_*') if path.is_dir()]
    for library in ('coreclr.dll', 'hostfxr.dll', 'System.Private.CoreLib.dll', 'Stardust.Core.dll', 'Stardust.Godot.dll'):
        if not (data / library).is_file():
            raise ValueError('Self-contained runtime/library missing: ' + library)
    env = os.environ.copy()
    for name in tuple(env):
        if name.startswith(('DOTNET', 'NUGET', 'MSBUILD')):
            env.pop(name)
    # The export must run without the downloaded SDK/editor on PATH.
    env['PATH'] = os.pathsep.join(part for part in env.get('PATH', '').split(os.pathsep)
                                if 'local-private' not in part.lower())
    output.mkdir(parents=True)
    observations = []

    def run(label, arguments, expected_exit=0, required=()):
        log_path = output / (label + '.log')
        start = time.perf_counter()
        with log_path.open('w', encoding='utf-8') as log:
            result = subprocess.run([str(executable), '--log-file', str(output / (label + '-engine.log')), *arguments],
                                    cwd=output, env=env, stdout=log, stderr=subprocess.STDOUT, timeout=120)
        text = log_path.read_text(encoding='utf-8')
        if result.returncode != expected_exit or any(token not in text for token in required):
            raise RuntimeError(label + ' failed; inspect its private log')
        admissions = [json.loads(line.split('=', 1)[1]) for line in text.splitlines() if line.startswith('renderer_admission=')]
        observation = dict(Case=label, ExitCode=result.returncode, ExpectedExitCode=expected_exit,
                           ExpectedOutcomePassed=True, Seconds=time.perf_counter()-start,
                           Renderer=admissions[-1] if admissions else None)
        observations.append(observation)
        (output / 'commands.json').write_text(json.dumps(observations, indent=2) + '\n')
        return text

    run('headless', ['--headless', '--', '--asset-self-test'], required=(
        'archive_scope_closed=true', 'original character geometry texture_defaults=True',
        'original idle preview clock ticks=480 loops=2'))
    run('cancel', ['--headless', '--', '--cancel-creature-load'], required=(
        'pending first lookup, archive cleanup and zero GPU publication',))
    run('headless-dx12-refused', ['--headless', '--', '--asset-self-test', '--require-d3d12'], 1,
        required=('"D3d12Accepted":false', 'Offline client checkpoint failed: InvalidOperationException'))
    run('unsupported-option', ['--headless', '--', '--asset-self-test', '--unsupported'], 1,
        required=('Offline client checkpoint failed: ArgumentException',))
    profile = json.loads((package / 'character-launch.json').read_text())
    profile['Version'] = 2
    bad_profile = output / 'bad-profile.json'
    bad_profile.write_text(json.dumps(profile))
    run('invalid-profile', ['--headless', '--', '--asset-self-test', '--offline-character-profile', str(bad_profile)], 1,
        required=('Offline client checkpoint failed: InvalidDataException',))
    if args.gpu:
        run('gpu-fallback-refused', ['--rendering-method', 'gl_compatibility', '--rendering-driver', 'opengl3',
                                    '--', '--asset-self-test'], 1,
            required=('"D3d12Accepted":false', 'Offline client checkpoint failed: InvalidOperationException'))
        run('gpu-fixtures', ['--', '--capture', str(output / 'character.png')], required=(
            '"D3d12Accepted":true', 'original idle preview clock ticks=480 loops=2',
            'sampled normal-mapped specular GPU', 'original character GPU upload and capture boundary'))
        animation = output / 'animation'
        animation.mkdir()
        run('gpu-animation', ['--', '--offline-animation-proof', str(animation)], required=(
            '"D3d12Accepted":true', 'PASS: wall-clock original animation process frames'))
        evidence = json.loads((animation / 'animation.json').read_text())
        if (evidence['Driver'] != 'd3d12' or evidence['ProcessArchitecture'] != 'X64' or
            evidence['PoseUpdates'] < 2 or evidence['WallSeconds'] < 1.0 or evidence['FirstFrame'] == evidence['LastFrame'] or
            evidence['OriginalClipSha256'] != receipt['OriginalPreparation']['Idle']['ClipSha256'] or
            digest(animation / 'pose-a.png') == digest(animation / 'pose-b.png')):
            raise ValueError('Original wall-clock animation evidence mismatch')
    for name, expected in receipt['PackageFiles'].items():
        if digest(package / name) != expected['Sha256']:
            raise ValueError('Package modified during execution')
    report = dict(Version=1, Passed=True, ExportedExecutableSha256=digest(executable),
                  SelfContainedRuntime=True, GpuVerified=args.gpu, Cases=observations,
                  LiveNetwork=False, OriginalExecutableExecuted=False)
    (output / 'verification.json').write_text(json.dumps(report, indent=2) + '\n')
    print('PASS: exported x64 offline client cases=' + str(len(observations)) + ' D3D12_GPU=' + str(args.gpu))


if __name__ == '__main__':
    main()
