"""Audit floor node global keys against layout node ordinals and paired portal geometry."""
import argparse,collections,contextlib,hashlib,json,math,subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_portal_layout import inspect as portal
from verify_path_graph import inspect as graph

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('portal_inventory',type=Path);p.add_argument('private_output',type=Path);p.add_argument('--core-dll',type=Path);p.add_argument('--key-policy',choices=('geometry','ordinal'),default='geometry');a=p.parse_args()
    a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    totals=collections.Counter();kind=collections.Counter();side_counts=collections.Counter();missing=[];issues=[];records=[];cache={};distances=[];floor_field04=collections.Counter()
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(a.root,a.configuration)]
        def read(member):
            if member in cache:return cache[member]
            for _,index in indices:
                value=index.lookup(member)
                if value is not None:cache[member]=value;return value
            cache[member]=None;return None
        def path_nodes(data):
            root,=nodes(data);v,=root[1];pg=next((value for tag,value in v[1] if tag==b'PGRF'),None)
            return graph(pg)[1] if pg is not None else None
        for item in json.loads(a.portal_inventory.read_text(encoding='utf-8')):
            data=read(item['member']);assert hashlib.sha256(data).hexdigest().upper()==item['sha256'];layout=portal(data);global_nodes=path_nodes(data);totals['layouts']+=1;totals['layout_graphs']+=global_nodes is not None
            geometry_sides=collections.defaultdict(set)
            for ci,cell in enumerate(layout['cells']):
                for ref in cell['portals']:geometry_sides[ref['geometry_index']].add(ci)
            floor_graphs={};bindings=[];groups=collections.defaultdict(list)
            for ci,cell in enumerate(layout['cells']):
                if not cell['floor']:totals['no_floor']+=1;continue
                floor_data=read(cell['floor'])
                if floor_data is None:missing.append(dict(layout=item['member'],cell=ci,floor=cell['floor']));continue
                totals['live_floor_references']+=1;ns=path_nodes(floor_data)
                if ns is None:totals['floor_without_graph']+=1;continue
                totals['floor_graph_references']+=1;floor_graphs[ci]=ns
                for ni,n in enumerate(ns):
                    floor_field04['minus_one' if n[1]==-1 else 'nonnegative' if n[1]>=0 else 'other_negative']+=1
                    if n[2]==-1:continue
                    totals['global_floor_nodes']+=1;kind[n[3]]+=1
                    problem=None
                    if a.key_policy=='ordinal':
                        if global_nodes is None:problem='NoLayoutGraph'
                        elif n[2]<0 or n[2]>=len(global_nodes):problem='GlobalKeyOutOfRange'
                        else:
                            target=global_nodes[n[2]];geometry=target[2]
                            if target[3] not in (3,5):problem='TargetNotPortalType'
                            elif geometry not in geometry_sides or ci not in geometry_sides[geometry]:problem='CellNotPortalSide'
                            distances.append(math.dist(n[4:7],target[4:7]))
                            binding=dict(cell=ci,node=ni,layout_node=n[2],layout_type=target[3],geometry=geometry);bindings.append(binding);groups[n[2]].append(binding)
                    else:
                        geometry=n[2]
                        if geometry not in geometry_sides or ci not in geometry_sides[geometry]:problem='CellNotPortalSide'
                        candidates=[] if global_nodes is None else [(gi,g) for gi,g in enumerate(global_nodes) if g[3] in (3,5) and g[2]==geometry]
                        if len(candidates)!=1:totals['layout_portal_node_missing_or_ambiguous']+=1
                        binding=dict(cell=ci,node=ni,layout_node=candidates[0][0] if len(candidates)==1 else None,layout_type=candidates[0][1][3] if len(candidates)==1 else None,geometry=geometry)
                        bindings.append(binding);groups[geometry].append(binding)
                        if len(candidates)==1:distances.append(math.dist(n[4:7],candidates[0][1][4:7]))
                    if problem:issues.append(dict(layout=item['member'],cell=ci,node=ni,key=n[2],problem=problem))
            for key,bindings_for_key in groups.items():
                side_counts[len(bindings_for_key)]+=1;geometry=bindings_for_key[0]['geometry'];actual=[b['cell'] for b in bindings_for_key];expected=geometry_sides[geometry]&floor_graphs.keys()
                if len(actual)!=len(set(actual)):issues.append(dict(layout=item['member'],key=key,problem='DuplicateCellGlobalKey'))
                if set(actual)!=expected:issues.append(dict(layout=item['member'],key=key,problem='PairedFloorSideMismatch',actual=actual,expected=sorted(expected)))
            if global_nodes is not None:
                for ni,n in enumerate(global_nodes):
                    if n[3]==4 and not 0<=n[2]<len(layout['cells']):issues.append(dict(layout=item['member'],node=ni,problem='CellNodeKeyOutOfRange'))
            records.append(dict(member=item['member'],sha256=item['sha256'],bindings=bindings,portal_sides={key:sorted(value) for key,value in geometry_sides.items()}))
    totals['missing_floor_references']=len(missing);totals['issues']=len(issues);totals['bound_layout_nodes']=sum(len({b['layout_node'] for b in r['bindings'] if b['layout_node'] is not None}) for r in records)
    output=dict(key_policy=a.key_policy,totals=dict(totals),global_floor_node_types=dict(kind),binding_side_counts=dict(side_counts),floor_field04=dict(floor_field04),maximum_node_position_separation=max(distances,default=0),missing=missing,issues=issues,layouts=records)
    (a.private_output/'associations.json').write_text(json.dumps(output,indent=2),encoding='utf-8')
    print('configuration_sha256='+hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper());print(f'totals={dict(totals)} floor_types={dict(kind)} side_counts={dict(side_counts)} field04={dict(floor_field04)} max_position_separation={max(distances,default=0)}')
    print('issue_examples='+str(issues[:8]))
    if a.core_dll:
        assert a.key_policy=='geometry'
        manifest=a.private_output/'members.txt';manifest.write_text('\n'.join(r['member'] for r in records)+'\n',encoding='utf-8');digest=hashlib.sha256(manifest.read_bytes()).hexdigest().upper()
        result=subprocess.run(['dotnet',str(a.core_dll),'--portal-associations',str(a.root),'--configuration',str(a.configuration),'--members',str(manifest),'--expected-members-sha256',digest],capture_output=True,text=True,check=True,timeout=90)
        (a.private_output/'native-association-agreement.txt').write_text(result.stdout,encoding='utf-8')
        native={r['member']:r for r in map(json.loads,(line[13:] for line in result.stdout.splitlines() if line.startswith('associations=')))}
        eligible_count=0;candidate_count=0;multiple_count=0;incomplete_count=0;invalid_count=0
        for record in records:
            grouped=collections.defaultdict(lambda:collections.defaultdict(list));bindings=[];diagnostics=[]
            for b in record['bindings']:
                if b['cell'] not in record['portal_sides'].get(b['geometry'],[]):
                    diagnostics.append(f"GlobalKeyNotCellPortal:{b['cell']}:{b['node']}:{b['geometry']}");invalid_count+=1;continue
                bindings.append({key:b[key] for key in ('cell','node','geometry','layout_node')});grouped[b['geometry']][b['cell']].append(b['node'])
                if b['layout_node'] is None:diagnostics.append(f"LayoutPortalNodeUnavailable:{b['cell']}:{b['node']}:{b['geometry']}")
            candidates=[]
            for geometry,cells in sorted(grouped.items()):
                sides=record['portal_sides'][geometry]
                if any(len(ns)>1 for ns in cells.values()):diagnostics.append(f'MultiplePortalNodes:{geometry}');multiple_count+=1
                if len(sides)!=2 or any(side not in cells for side in sides):diagnostics.append(f'IncompletePortalCandidate:{geometry}');incomplete_count+=1;continue
                candidates.append(dict(geometry=geometry,left_cell=sides[0],right_cell=sides[1],left_nodes=sorted(cells[sides[0]]),right_nodes=sorted(cells[sides[1]])))
            expected_missing=[f"UnavailableCellFloor:{r['cell']}:{r['floor']}:Missing/masked floor." for r in missing if r['layout']==record['member']]
            expected=dict(member=record['member'],bindings=bindings,candidates=candidates,diagnostics=diagnostics,bundle_diagnostics=expected_missing)
            assert native[record['member']]==expected,(record['member'],native[record['member']],expected)
            eligible_count+=len(bindings);candidate_count+=len(candidates)
        summary=dict(layouts=len(records),eligible_bindings=eligible_count,candidates=candidate_count,multiple_groups=multiple_count,incomplete_groups=incomplete_count,invalid_bindings=invalid_count,missing_floors=len(missing))
        (a.private_output/'native-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
        print('members_sha256='+digest);print('PASS independent association agreement '+str(summary))

if __name__=='__main__':main()
