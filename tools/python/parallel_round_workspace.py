"""Capture public working files and review bounded worker changes without Git mutation."""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import subprocess
import zipfile


def digest(data):
    return hashlib.sha256(data).hexdigest()


def listed(root):
    result = subprocess.check_output([
        'git', '-c', f'safe.directory={root.as_posix()}', '-C', str(root),
        'ls-files', '-co', '--exclude-standard', '-z'])
    paths = sorted(set(result.decode('utf-8').split('\0')) - {''})
    if len({path.casefold() for path in paths}) != len(paths):
        raise ValueError('Case-folded workspace path collision')
    return paths


def safe(root, relative):
    portable = PurePosixPath(relative)
    if (not relative or '\\' in relative or ':' in relative or portable.is_absolute()
            or portable.as_posix() != relative
            or any(part in ('.', '..') or part.casefold() == '.git' for part in portable.parts)):
        raise ValueError(f'Unsafe workspace path: {relative}')
    path = root
    candidates = [*reversed(root.parents), root]
    for part in portable.parts:
        path = path / part
        candidates.append(path)
    for path in candidates:
        try:
            metadata = path.lstat()
        except FileNotFoundError:
            continue
        if path.is_symlink() or getattr(metadata, 'st_file_attributes', 0) & 0x400:
            raise ValueError(f'Linked workspace path: {relative}')
    path = root / portable
    if not path.resolve().is_relative_to(root.resolve()):
        raise ValueError(f'Unsafe workspace path: {relative}')
    return path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['snapshot', 'review', 'integrate'])
    parser.add_argument('--root', type=Path, default=Path.cwd())
    parser.add_argument('--baseline', type=Path, required=True)
    parser.add_argument('--worker', type=Path)
    parser.add_argument('--allowed', type=Path, help='JSON list of exact paths; no globs')
    args = parser.parse_args()
    root = args.root.absolute()
    if args.mode == 'snapshot':
        args.baseline.mkdir(parents=True, exist_ok=True)
        if (args.baseline / 'baseline.json').exists():
            raise ValueError('Baseline already exists')
        rows = []
        with zipfile.ZipFile(args.baseline / 'baseline.zip', 'x', zipfile.ZIP_DEFLATED) as archive:
            for relative in listed(root):
                path = safe(root, relative)
                if not path.is_file():
                    continue
                data = path.read_bytes()
                rows.append(dict(path=relative, sha256=digest(data), bytes=len(data)))
                archive.writestr(relative, data)
        head = subprocess.check_output(['git', '-C', str(root), 'rev-parse', 'HEAD']).decode().strip()
        (args.baseline / 'baseline.json').write_text(json.dumps(dict(head=head, files=rows), indent=2), encoding='utf-8')
        print(json.dumps(dict(files=len(rows), manifest_sha256=digest((args.baseline / 'baseline.json').read_bytes()), zip_sha256=digest((args.baseline / 'baseline.zip').read_bytes()))))
        return
    if args.worker is None or args.allowed is None:
        parser.error('review/integrate require --worker and --allowed')
    worker = args.worker.absolute()
    allowed = set(json.loads(args.allowed.read_text(encoding='utf-8')))
    manifest = json.loads((args.baseline / 'baseline.json').read_text(encoding='utf-8'))
    baseline = {row['path']: row['sha256'] for row in manifest['files']}
    changes = []
    for relative in sorted(set(baseline) | set(listed(worker))):
        path = safe(worker, relative)
        current = digest(path.read_bytes()) if path.is_file() else None
        previous = baseline.get(relative)
        if current != previous:
            changes.append(dict(path=relative, before=previous, after=current))
    unexpected = [row['path'] for row in changes if row['path'] not in allowed]
    if unexpected:
        raise ValueError(f'Changes outside assignment: {unexpected}')
    if args.mode == 'integrate':
        # Complete preflight before the first write. Never delete, stage or commit.
        prepared = []
        for row in changes:
            target = safe(root, row['path'])
            current = digest(target.read_bytes()) if target.is_file() else None
            if row['after'] is None or current not in (row['before'], row['after']):
                raise ValueError(f'Integration conflict/deletion: {row["path"]}')
            data = safe(worker, row['path']).read_bytes()
            if digest(data) != row['after']:
                raise ValueError(f'Worker bytes changed during preflight: {row["path"]}')
            prepared.append((row, data))
        for row, data in prepared:
            target = safe(root, row['path'])
            target.parent.mkdir(parents=True, exist_ok=True)
            safe(root, row['path']).write_bytes(data)
        for row, _ in prepared:
            if digest(safe(root, row['path']).read_bytes()) != row['after']:
                raise ValueError(f'Integration postflight mismatch: {row["path"]}')
    print(json.dumps(dict(mode=args.mode, worker=str(worker), changes=changes), indent=2))


if __name__ == '__main__':
    main()
