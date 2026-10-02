"""Independent streaming insertion-slot audit for authored original-cell input fixtures."""
import struct


def verify(light_rows,slot_rows,synthetic_rows=()):
    f32=lambda v:struct.unpack('<f',struct.pack('<f',v))[0]
    by_cell={}
    for row in light_rows:by_cell.setdefault(row['cell'],[]).append(row)
    maximum=0
    def score(rgb):return f32(abs(rgb[2])*f32(.11)+abs(rgb[1])*f32(.59)+abs(rgb[0])*f32(.3))
    for row in slot_rows:
        sources=sorted(by_cell[row['cell']],key=lambda r:r['index']);expected=[]
        for source in sources:
            if row['baked'] and not source['HasSpecularRgb']:continue
            expected.append(dict(Key=source['index'],RuntimeType=source['RuntimeLightType'],diffuse=[0,0,0] if row['baked'] else source['diffuse'][:3],specular=[0,0,0] if source['kind']==0 else source['specular'][:3]))
        assert len(expected)==len(row['inputs'])
        for source,input_row in zip(expected,row['inputs']):
            for key,value in source.items():assert input_row[key]==value
            maximum=max(maximum,abs(score(source['diffuse'])-input_row['diffuseScore']),abs(score(source['specular'])-input_row['specularScore']))
        spec_d=[None];diff_d=[None]*2;spec_p=[None];diff_p=[None]*4;ambient=[0,0,0]
        for candidate in expected:
            kind=candidate['RuntimeType']
            if kind==0:ambient=[f32(a+b) for a,b in zip(ambient,candidate['diffuse'])];continue
            if kind not in (1,3):continue
            spec,diff=(spec_d,diff_d) if kind==1 else (spec_p,diff_p)
            for slots,field in ((spec,'specular'),(diff,'diffuse')):
                for i,current in enumerate(slots):
                    if candidate is None:break
                    if current is None or score(candidate[field])>score(current[field]):slots[i],candidate=candidate,current
        key=lambda item:None if item is None else item['Key']
        assert row['ambient']==ambient and row['specularDirectional']==key(spec_d[0]) and row['specularPoint']==key(spec_p[0])
        assert row['diffuseDirectional']==[key(r) for r in diff_d if r is not None] and row['diffusePoint']==[key(r) for r in diff_p if r is not None]
    for row in synthetic_rows:
        slots={1:([None],[None]*2),3:([None],[None]*4)};ambient=list(map(f32,(.1,.2,.3)))
        for source in row['inputs']:
            candidate=dict(source)
            if row['scaled']:
                for field,factor in (('diffuse','DiffuseScale'),('specular','SpecularScale')):candidate[field]=[f32(v*source[factor]) for v in source[field]]
            kind=candidate['RuntimeType']
            if kind==0:ambient=[f32(a+b) for a,b in zip(ambient,candidate['diffuse'])]
            elif kind in slots:
                spec,diff=slots[kind]
                for group,field in ((spec,'specular'),(diff,'diffuse')):
                    for i,current in enumerate(group):
                        if candidate is None:break
                        if current is None or score(candidate[field])>score(current[field]):group[i],candidate=candidate,current
        key=lambda item:None if item is None else item['Key']
        assert max(abs(a-b) for a,b in zip(ambient,row['ambient']))<2e-6
        for kind,name in ((1,'Directional'),(3,'Point')):
            spec,diff=slots[kind];assert key(spec[0])==row['specular'+name] and [key(r) for r in diff if r is not None]==row['diffuse'+name]
    assert not synthetic_rows or len(synthetic_rows)==64
    assert len(slot_rows)==12 and maximum<2e-6
    return dict(authored_order_cell_slot_cases=len(slot_rows),synthetic_slot_cases=len(synthetic_rows),maximum_score_error=maximum,live_input_visibility_order='unknown',specular_slots_per_type=1,diffuse_directional_slots=2,diffuse_point_slots=4)
