"""One original layer provenance and explicit operational rejection; no numeric family evaluation."""
import json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'poc/round54-tests/Integration'))
import per_cell_height_peer as old
r=old.r;j=old.j

def generate(assetroot,config,exe):
 r.n.gate(Path(exe).read_bytes(),r.n.EXE);r.n.setup()
 generator=old.old.g.generator(assetroot,config);group,entries=r.original_group(assetroot,config)
 meta=r.n.family.load('slice_metadata55','poc/round33-tests/Assets/verify_tgen_input.py').metadata(generator)
 layer=meta['Layers'][3];assert layer['Section']['Version']=='0003' and [c['FormType'] for c in layer['Children']]==['BCIR','FFRA','AHFR']
 children=[]
 for ordinal,section in enumerate(layer['Children'],2):
  index,row=next((i,row) for i,row in enumerate(meta['Rules']) if row['LayerIndex']==3 and row['ChildOrdinal']==ordinal)
  children.append(dict(ChildOrdinal=ordinal,GlobalRuleIndex=index,FormType=section['FormType'],Version=section['Version'],Offset=section['Offset'],EncodedBytes=section['EncodedBytes'],Sha256=section['Sha256']))
 section=layer['Children'][2];blob=generator[section['Offset']:section['Offset']+section['EncodedBytes']];snap=j.ah.snapshot(blob)
 fid=snap['Family'];fid=fid-(1<<32) if fid>=1<<31 else fid
 family=next(e for e in entries if e['Id']==fid);words=family['Words']
 assert words[1] or words[3],'Expected original curves-enabled height-family boundary'
 expected=dict(LayerIndex=3,ParentIndex=layer['ParentIndex'],Depth=layer['Depth'],LayerOffset=layer['Section']['Offset'],LayerBytes=layer['Section']['EncodedBytes'],LayerSha256=layer['Section']['Sha256'],Children=children,BoundaryInverted=bool(layer['AdtaWordBits'][0]),FilterInverted=bool(layer['AdtaWordBits'][1]),AdtaWord2Bits=layer['AdtaWordBits'][2],HeightFamilyId=fid,HeightOperationalInputs=False,OperationalRejected=True,OriginalHeightEvaluated=False)
 return dict(GeneratorHex=generator.hex(),GeneratorSha256=r.sha(generator),GroupSha256=r.sha(group),SelectedLayer=layer,Expected=expected,OriginalHeightEvaluated=False)

if __name__=='__main__':
 action,assetroot,config,exe,fixture=sys.argv[1:6]
 assert Path.cwd().resolve()==ROOT
 guard=old.old.runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];d=generate(assetroot,config,exe)
 if action=='generate':
  guard(Path(fixture),ROOT);Path(fixture).write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8');print('Generated original layer provenance; curves-enabled height requires operational rejection')
 elif action=='verify':
  assert json.loads(Path(fixture).read_text(encoding='utf-8'))==d
  actual=json.loads(Path(sys.argv[6]).read_text(encoding='utf-8'));assert actual==d['Expected']
  guard(Path(sys.argv[7]),ROOT);result=dict(Passed=True,OriginalLayerObserved=True,OperationalRejected=True,OriginalHeightEvaluated=False)
  Path(sys.argv[7]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
 else:raise ValueError('Unknown action')
