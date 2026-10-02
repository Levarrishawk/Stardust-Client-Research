"""Independent cross-product quaternion placement/affine/reflection oracle for synthetic decoded creates."""
import argparse
import json
import math
import struct
from pathlib import Path

SIGNS=(1,1,-1)
KEYS={'identity','x90','y90','z90','x180','y180','z180','x270','y270','z270','asymmetric','opposite','nonunit','scalar2','tiny_x','tiny_w','tiny_mixed','minimum_normal','maximum','unbalanced','small_scaled','large_scaled','signed_zero','zero_identity','signed_zero_identity','extreme_position','subnormal_position'}

def f32(value):return struct.unpack('<f',struct.pack('<f',value))[0]
def bits(value):return struct.unpack('<I',struct.pack('<f',value))[0]
def scalar(word):return struct.unpack('<f',struct.pack('<I',word))[0]
def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def rotate(q,v):
    # Hamilton rotation via two vector cross products; no native matrix coefficient formula.
    u=q[:3];uv=cross(u,v);uuv=cross(u,uv)
    return [v[i]+2*(q[3]*uv[i]+uuv[i]) for i in range(3)]
def basis(q):return [list(row) for row in zip(*(rotate(q,v) for v in ([1,0,0],[0,1,0],[0,0,1])))]
def apply(rows,p):return [math.fsum(a*b for a,b in zip(row,p)) for row in rows]
def add(a,b):return [x+y for x,y in zip(a,b)]
def reflect(rows):return [[SIGNS[i]*rows[i][j]*SIGNS[j] for j in range(3)] for i in range(3)]
def sp(p):return [a*b for a,b in zip(SIGNS,p)]
def multiply(a,b):return [[math.fsum(a[i][k]*b[k][j] for k in range(3)) for j in range(3)] for i in range(3)]
def distance(a,b):return max(abs(x-y) for x,y in zip(a,b))
def flatten(rows):return [x for row in rows for x in row]

def audit(output):
    assert output.stat().st_size<1048576
    text=output.read_text(encoding='utf-8-sig');assert 'PASS R6-ASSETS-01:' in text
    groups={}
    for line in text.splitlines():
        if '=' in line:
            tag,value=line.split('=',1)
            if tag.startswith('placement'):groups.setdefault(tag,[]).append(json.loads(value,parse_int=lambda s:-0.0 if s=='-0' else int(s)))
    records={r['key']:r for r in groups['placement']};assert len(records)==len(groups['placement'])==27 and set(records)==KEYS
    validation,=groups['placement_validation']
    assert all(validation[k]==v for k,v in dict(cases=27,probes=105,compositions=26,rejections=25,gpu=False).items())
    assert len(groups['placement_point'])==105 and len(groups['placement_composed'])==26
    names=['QuaternionX','QuaternionY','QuaternionZ','QuaternionW','PositionX','PositionZ','PositionY']
    expected={};maximum_basis=maximum_point=maximum_composition=maximum_norm_relative=0
    def near(actual,wanted,tolerance=3e-6):
        actual=[f32(v) for v in actual] # Native Vector3/Quaternion exports use shortest binary32 JSON decimals.
        assert len(actual)==len(wanted) and all(math.isfinite(v) for v in actual)
        assert all(abs(a-b)<=tolerance*max(1,abs(b)) for a,b in zip(actual,wanted)),(actual,wanted)
        return distance(actual,wanted)
    for key,row in records.items():
        raw=row['raw'];words=row['rawBits'];assert len(words)==7 and [bits(raw[n]) for n in names]==words
        assert raw['ObjectId']==0xfedcba9876543210 and raw['TemplateCrc']==2937962913 and raw['RawHyperspaceFlag']==255
        q=[scalar(w) for w in words[:4]];norm=math.hypot(*q);fallback=norm==0
        assert row['policy'] in (0,1) and (not fallback or row['policy']==1)
        unit=[0,0,0,1] if fallback else [v/norm for v in q]
        near(row['unit'],unit,1e-7)
        assert row['UsedZeroIdentity']==fallback and row['AppliedNormalization']==(not fallback and norm!=1)
        assert row['InputQuaternionNorm']==0 if fallback else abs(row['InputQuaternionNorm']/norm-1)<=5e-16
        if norm:maximum_norm_relative=max(maximum_norm_relative,abs(row['InputQuaternionNorm']/norm-1))
        rotation=basis(unit);p=[scalar(words[4]),scalar(words[6]),scalar(words[5])]
        maximum_basis=max(maximum_basis,near(flatten(row['rows']),flatten(rotation),2e-7))
        assert [bits(v) for v in row['translation']]==[words[4],words[6],words[5]]
        expected[key]=(rotation,p)
    assert records['signed_zero']['rawBits'][0]==records['signed_zero']['rawBits'][2]==0x80000000
    assert records['signed_zero']['rawBits'][4]==records['signed_zero']['rawBits'][5]==0x80000000
    assert records['subnormal_position']['rawBits'][4]==1 and records['subnormal_position']['rawBits'][6]==0x80000001
    assert records['maximum']['InputQuaternionNorm']>scalar(0x7f7fffff) and records['tiny_x']['InputQuaternionNorm']==scalar(1)
    near(flatten(records['asymmetric']['rows']),[f32(v) for v in flatten(records['opposite']['rows'])],0)
    for key,p,wanted in [('x90',[0,1,0],[0,0,1]),('y90',[0,0,1],[1,0,0]),('z90',[1,0,0],[0,1,0])]:near(apply(expected[key][0],p),wanted,1e-7)
    transpose_wrong=xzy_wrong=reflection_wrong=order_wrong=0;point_counts={};composition_keys=set()
    for row in groups['placement_point']:
        key=row['key'];point_counts[key]=point_counts.get(key,0)+1;r,p=expected[key];point=row['point']
        direction=apply(r,point);world=add(direction,p)
        maximum_point=max(maximum_point,near(row['direction'],direction),near(row['world'],world))
        near(flatten(row['engineRows']),flatten(reflect(r)),2e-7);near(row['engineTranslation'],sp(p),0);near(row['enginePoint'],sp(world))
        wrong=add(apply([list(col) for col in zip(*r)],point),p)
        transpose_wrong+=distance(wrong,world)>1e-4
        xzy_wrong+=distance(add(direction,[p[0],p[2],p[1]]),world)>1e-4
        single_reflection=[[SIGNS[i]*r[i][j] for j in range(3)] for i in range(3)]
        reflection_wrong+=distance(add(apply(single_reflection,sp(point)),sp(p)),sp(world))>1e-4
    assert point_counts=={k:(1 if k=='extreme_position' else 4) for k in KEYS}
    for row in groups['placement_composed']:
        key=row['key'];assert key not in composition_keys;composition_keys.add(key)
        r,p=expected[key];parent=[[f32(v) for v in r] for r in row['parentRows']];parent_p=row['parentTranslation'];point=row['point']
        assert parent==[[0,-2,.25],[1.5,.125,0],[f32(.2),0,1.25]] and parent_p==[10,-20,30]
        rotation=multiply(parent,r);translation=add(apply(parent,p),parent_p);world=add(apply(rotation,point),translation)
        maximum_composition=max(maximum_composition,near(flatten(row['rows']),flatten(rotation)),near(row['translation'],translation),near(row['world'],world))
        near(flatten(row['engineRows']),flatten(reflect(rotation)));near(row['engineTranslation'],sp(translation));near(row['enginePoint'],sp(world))
        reverse=add(apply(multiply(r,parent),point),add(apply(r,parent_p),p));order_wrong+=distance(reverse,world)>1e-4
    assert composition_keys==KEYS-{'extreme_position'}
    assert transpose_wrong>=30 and xzy_wrong>=70 and reflection_wrong>=30 and order_wrong>=20
    return dict(cases=27,point_probes=105,parent_compositions=26,native_rejections=25,maximum_basis_error=maximum_basis,maximum_point_error=maximum_point,maximum_composition_error=maximum_composition,maximum_norm_relative_error=maximum_norm_relative,wrong_transpose_cases=transpose_wrong,wrong_wire_xyz_cases=xzy_wrong,wrong_single_reflection_cases=reflection_wrong,wrong_composition_order_cases=order_wrong,policy='modern standard XYZW quaternion normalization; explicit zero choice',original_dynamic=False,gpu=False)

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('native_output',type=Path);args=p.parse_args();print(json.dumps(audit(args.native_output),indent=2))
