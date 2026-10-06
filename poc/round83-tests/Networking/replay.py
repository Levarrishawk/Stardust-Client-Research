"""Independent pre-native ClientId/permissions fixtures; offline standalone codec test."""
import argparse
import hashlib
import json
import os
import runpy
import struct
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--dotnet', type=Path, default=Path('C:/Program Files/dotnet/dotnet.exe'))
    args = parser.parse_args()
    out = runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](args.output, ROOT, directory=True)
    existing = runpy.run_path(str(ROOT/'tools/python/verify_login_zone_fixtures.py'))
    crc = existing['swg_hash']
    assert crc('LoginEnumCluster') == 0xC11C63B9 and crc('LoginClusterStatus') == 0x3436AEB6
    client, permissions = crc('ClientIdMsg'), crc('ClientPermissionsMessage')
    cases = []
    for credential, version in [(bytes.fromhex('000080FF'), 'A'), (bytes.fromhex('00FF107F78563412'), '20260817-SD:01'),
                                (bytes(i%256 for i in range(65536)), 'V'*256)]:
        raw = struct.pack('<HIII', 3, client, 0xFE, len(credential)) + credential + struct.pack('<H', len(version)) + version.encode('ascii')
        cases.append(dict(CredentialHex=credential.hex().upper(), Version=version, ExpectedHex=raw.hex().upper()))
    good = []
    for length in (3, 4):
        for bits in range(1 << length):
            flags = [bool(bits & (1 << i)) for i in range(length)]
            raw = struct.pack('<HI', 4, permissions) + bytes(flags)
            good.append(dict(Hex=raw.hex().upper(), Flags=flags))
    canonical = struct.pack('<HI', 4, permissions) + bytes([1,1,0,0])
    malformed = [canonical[:i] for i in range(9)] + [canonical+b'\0', canonical+b'\0'*10]
    for offset in range(6):
        bad = bytearray(canonical); bad[offset] ^= 0x80; malformed.append(bytes(bad))
    for length in (3,4):
        for offset in range(length):
            for value in (2, 127, 128, 255):
                bad = bytearray(canonical[:6+length]); bad[6+offset] = value; malformed.append(bytes(bad))
    fixture = dict(ClientIdOpcode=client, PermissionsOpcode=permissions, ClientIdCases=cases,
                   PermissionCases=good, MalformedPermissions=[v.hex().upper() for v in malformed])
    fixturepath = out/'fixtures-before-native.json'; fixturepath.write_text(json.dumps(fixture)+'\n', encoding='utf-8')
    config = out/'NuGet.Config'; config.write_text('<configuration><packageSources><clear /></packageSources></configuration>\n')
    env = os.environ.copy(); env.update(APPDATA=str(out/'appdata'), DOTNET_CLI_HOME=str(out/'dotnet-home'),
        DOTNET_GENERATE_ASPNET_CERTIFICATE='false', DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', NUGET_PACKAGES=str(out/'packages'))
    project = ROOT/'poc/round83-tests/Networking/Networking.csproj'
    commands = [[str(args.dotnet), 'build', str(project), '--artifacts-path', str(out/'artifacts'), '--configfile', str(config),
                 '-p:NuGetAudit=false', '-p:UseSharedCompilation=false', '-p:UseAppHost=false', '--nologo'],
                [str(args.dotnet), str(out/'artifacts/bin/Networking/debug/Networking.dll'), str(fixturepath), str(out/'native-receipt.json')]]
    for i, command in enumerate(commands):
        result = subprocess.run(command, cwd=ROOT, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, encoding='utf-8', timeout=120)
        (out/f'step-{i}.log').write_text(result.stdout, encoding='utf-8')
        if result.returncode: raise RuntimeError(result.stdout)
        if i == 0: assert '0 Warning(s)' in result.stdout and '0 Error(s)' in result.stdout
    native = json.loads((out/'native-receipt.json').read_text())
    assert native['Passed'] and native['Synthetic'] and not native['SocketCreated']
    receipt = dict(native, ClientIdOpcode=f'0x{client:08X}', PermissionsOpcode=f'0x{permissions:08X}',
                   ExactClientIdFixtures=len(cases), PermissionVariants=len(good), MalformedPermissionFixtures=len(malformed),
                   FixtureSha256=hashlib.sha256(fixturepath.read_bytes()).hexdigest(), Commands=commands)
    (out/'receipt.json').write_text(json.dumps(receipt, indent=2)+'\n'); print(json.dumps(receipt))


if __name__ == '__main__': main()
