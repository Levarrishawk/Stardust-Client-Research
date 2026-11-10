"""Enumerate literal source Bink/DebugHelp binds, then run the static PE auditor.

This adapter targets the existing source tree layout, not arbitrary C++ syntax.
DLL roots and hashes remain explicit; it never reads client configuration.
"""
import argparse
from pathlib import Path
import re

from audit_pe_runtime_dependencies import common_arguments, run_audit


def source_exports(source, renderer, bink, debug):
    bink_source = source / 'src/engine/client/library/clientGraphics/src/Bink/BinkDLL.cpp'
    bink_extra = source / 'src/engine/client/library/clientGraphics/src/Bink/BinkVideo.h'
    debug_source = source / 'src/engine/shared/library/sharedDebug/src/win32/DebugHelp.cpp'
    captured = {path: path.read_bytes() for path in [bink_source, bink_extra, debug_source]}
    bink_names = sorted(set(re.findall(r'_bind\("([^"]+)"\)', captured[bink_source].decode('utf-8')) +
                            re.findall(r'_bind\("([^"]+)"\)', captured[bink_extra].decode('utf-8'))))
    debug_names = sorted(set(re.findall(r'^\s*GPA\(\w+,\s*(\w+)\);',
                                        captured[debug_source].decode('utf-8'), re.M)))
    if not bink_names or not debug_names:
        raise ValueError('source literal bind-list extraction empty; review adapter against this source')
    requests = [(renderer, 'GetApi')]
    requests += [(bink, name) for name in bink_names]
    requests += [(debug, name) for name in debug_names]
    return captured, requests


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    common_arguments(parser)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--renderer-name', required=True)
    parser.add_argument('--bink-name', required=True)
    parser.add_argument('--debug-name', required=True)
    args = parser.parse_args()
    blobs, dynamic = source_exports(args.source.resolve(), args.renderer_name, args.bink_name, args.debug_name)
    run_audit(args, blobs, dynamic)


if __name__ == '__main__':
    main()
