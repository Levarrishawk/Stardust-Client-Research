"""Extract pinned original Mos Taike research inputs and inventory into a fresh ignored directory."""
import argparse
import contextlib
import hashlib
import importlib.util
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--asset-root',type=Path,required=True)
    p.add_argument('--config',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True)
    a=p.parse_args();out=a.output.resolve();out.relative_to((ROOT/'local-private').resolve())
    if out.exists():raise ValueError('Fresh ignored output required')
    spec=importlib.util.spec_from_file_location('mos_taike_tgen_inventory',ROOT/'poc/round33-tests/Assets/verify_tgen_input.py')
    t=importlib.util.module_from_spec(spec);spec.loader.exec_module(t)
    if t.sha(a.config.read_bytes())!=t.CONFIG:raise ValueError('Unverified original archive configuration')
    with contextlib.ExitStack() as stack:
        indices=[(priority,path,t.Index(stack.enter_context(path.open('rb')))) for priority,path in t.archives(a.asset_root,a.config)]
        def read(name):
            for priority,path,index in indices:
                value=index.lookup(name)
                if value is not None:return value,priority,path.name
            raise ValueError('Original input missing: '+name)
        terrain,tp,ta=read('terrain/tatooine.trn');snapshot,sp,sa=read('snapshot/tatooine.ws')
    if t.sha(terrain)!=t.SOURCE or t.sha(snapshot)!='87E1810A7C87F1D6CDF3FB47BBE6F84D0922471802A74C69E4CAAA5157FC7477':raise ValueError('Unverified terrain/snapshot')
    v,=t.records(terrain,12,len(terrain));_,g,_=t.records(terrain,v[2]+12,v[3]);generator=terrain[g[2]:g[3]]
    inventory=t.metadata(generator)
    for layer in inventory['Layers']:
        s=layer['Section'];v,=t.records(generator,s['Offset']+12,s['Offset']+s['EncodedBytes']);h,*_=t.records(generator,v[2]+12,v[3]);hv,=t.records(generator,h[2]+12,h[3]);d,=t.records(generator,hv[2]+12,hv[3]);layer['Name']=d[1][4:].split(b'\0',1)[0].decode('latin1')
    out.mkdir(parents=True)
    (out/'tatooine.trn').write_bytes(terrain);(out/'tatooine.ws').write_bytes(snapshot)
    (out/'terrain-layers.json').write_text(json.dumps(inventory,indent=2)+'\n')
    receipt=dict(TerrainSha256=t.sha(terrain),SnapshotSha256=t.sha(snapshot),GeneratorSha256=t.sha(generator),TerrainPriority=tp,SnapshotPriority=sp,Layers=len(inventory['Layers']),Rules=len(inventory['Rules']),OriginalExecutableExecuted=False,LiveNetwork=False)
    (out/'inspection.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))

if __name__=='__main__':main()
