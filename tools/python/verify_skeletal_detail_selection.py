"""Independent formula/state oracle for fixed modern skeletal detail selection fixtures."""
import argparse,json,math
from pathlib import Path


def project(sample):
    radius,depth,fov,height=(sample[k] for k in ('BoundingRadius','CenterDepth','VerticalFovDegrees','ViewportHeight'))
    if depth<=radius:return None
    return radius/depth*height/math.tan(math.radians(fov)/2)


def choose(pixels,current,thresholds,h):
    if pixels is None:return 0
    if current is None:return sum(pixels<t for t in thresholds)
    # Determine every eligible crossing directly; no native while-loop implementation is shared.
    coarse=[d for d in range(current,4) if all(pixels<thresholds[k]*(1-h) for k in range(current,d))]
    detail=max(coarse)
    fine=[d for d in range(detail+1) if all(pixels>=thresholds[k]*(1+h) for k in range(d,detail))]
    return min(fine)


def audit(output):
    text=output.read_text(encoding='utf-8-sig');assert 'PASS R4-ASSETS-01:' in text and output.stat().st_size<1048576
    current=None;count=resets=wrong=inside=0;groups={};alternate={};exact=0;boundaries=0;maximum=0.
    for line in text.splitlines():
        if '=' not in line:continue
        tag,value=line.split('=',1)
        if tag not in ('detail_reset','detail_sample','detail_exact','detail_alternate','detail_hysteresis_boundary'):continue
        row=json.loads(value)
        if tag=='detail_reset':current=None;resets+=1;continue
        pixels=project(row['sample'])
        if tag=='detail_exact':assert row['threshold']==pixels and row['detail']==0;exact+=1;continue
        if tag=='detail_hysteresis_boundary':
            t=row['threshold'];assert t*(1.25 if row['upward'] else .75)==pixels
            assert row['before']==(1 if row['upward'] else 0) and row['result']['Detail']==choose(pixels,row['before'],[t,t/2,t/4],.25)==0
            boundaries+=1;continue
        h=row['h'] if tag=='detail_alternate' else .1
        prior=alternate.get(h) if tag=='detail_alternate' else current
        expected=choose(pixels,prior,[240,120,60],h);native=row['result']
        assert native['Detail']==expected and native['NearCamera']==(pixels is None)
        if pixels is None:assert native['ProjectedDiameterPixels'] is None;inside+=1
        else:
            error=abs(native['ProjectedDiameterPixels']-pixels);maximum=max(maximum,error);assert error<=max(1e-12,abs(pixels)*1e-12)
            wrong+=expected!=sum(pixels<t for t in (240,120,60))
        if tag=='detail_alternate':alternate[h]=expected
        else:current=expected;groups.setdefault(row['group'],[]).append(expected)
        count+=1
    assert boundaries==2 and resets==3 and exact==1 and count==76 and inside==3
    assert len(groups['descending'])==len(groups['ascending'])==15 and len(groups['jitter'])==25 and len(groups['repeat'])==4 and len(groups['projection'])==7
    assert len(set(groups['jitter']))==1 and groups['descending'][0]==0 and groups['descending'][-1]==3 and groups['ascending'][0]==3 and groups['ascending'][-1]==0
    assert set(alternate)=={0,.49} and wrong>=10
    return dict(supplied_cases=count,near_inside_cases=inside,exact_boundary_cases=exact+boundaries,resets=resets,stateless_wrong_policy_cases=wrong,maximum_projection_error=maximum,policy='modern centered-sphere diameter; downward strict/upward inclusive',original_thresholds=False)

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('native_output',type=Path);a=p.parse_args();print(json.dumps(audit(a.native_output),indent=2))
