#!/usr/bin/env python3
"""Summarize effective .lod IFF structure without exporting proprietary payloads."""
from __future__ import annotations
import argparse, csv, math, re, struct, sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from scan_tre_sound_references import expand

def archive_records(path):
    """TRE records: uncompressed kind 0 stores usize bytes; csize may be zero."""
    raw=path.read_bytes()
    if raw[:8]!=b"EERT5000":return
    count,offset,rk,rs,nk,ns,nus=struct.unpack_from("<7I",raw,8)
    records=expand(rk,raw[offset:offset+rs],count*24)
    names=expand(nk,raw[offset+rs:offset+rs+ns],nus)
    for n in range(count):
        _,usize,poff,kind,csize,noff=struct.unpack_from("<6I",records,n*24)
        end=names.index(0,noff)
        name=names[noff:end].decode("ascii","replace").lower().replace("\\","/")
        stored=usize if kind==0 else csize
        yield name,usize,kind,raw[poff:poff+stored]

def node(data, at, limit):
    if at + 8 > limit: raise ValueError("short header")
    tag, size = struct.unpack_from(">4sI", data, at)
    stop = at + 8 + size
    if stop > limit: raise ValueError("record overrun")
    return tag, at + 8, stop

def children(data, begin, end):
    at = begin
    while at < end:
        tag, body, stop = node(data, at, end)
        label = ("FORM:" + data[body:body+4].decode("ascii", "replace")) if tag == b"FORM" else tag.decode("ascii", "replace")
        yield label, body, stop
        at = stop
    if at != end: raise ValueError("misaligned")

def parse(data):
    tag, body, stop = node(data, 0, len(data))
    if tag != b"FORM" or data[body:body+4] != b"DTLA" or stop != len(data): raise ValueError("not DTLA")
    top = list(children(data, body+4, stop))
    if len(top) != 1 or not top[0][0].startswith("FORM:"): raise ValueError("version form")
    version = top[0][0][5:]
    vbody, vend = top[0][1]+4, top[0][2]
    first = list(children(data, vbody, vend))
    signature = "/".join(x[0] for x in first)
    nested = []
    refs=[]; info_count=0; info_ids=[]; intervals=[]; pivt_sizes=[]; child_prefixes=[]; misc=[]; policy_layouts=[]; policy_meshes=[]
    for label, start, end in first:
        if label.startswith("FORM:"):
            sub = list(children(data, start+4, end))
            nested.append((label, "/".join(x[0] for x in sub), len(sub)))
            if label in ("FORM:RADR","FORM:TEST","FORM:WRIT"):
                for sublabel,sbegin,send in sub:
                    if sublabel=="FORM:IDTL":
                        idtl=list(children(data,sbegin+4,send))
                        detail=[]
                        for ilabel,ibegin,iend in idtl:
                            if ilabel.startswith("FORM:"):
                                chunks=list(children(data,ibegin+4,iend))
                                detail.extend(x[0] for x in chunks)
                                if ilabel=="FORM:0000" and [x[0] for x in chunks]==["VERT","INDX"]:
                                    nv=chunks[0][2]-chunks[0][1];ni=chunks[1][2]-chunks[1][1]
                                    if nv%12 or ni%4:raise ValueError("IDTL geometry width")
                                    indices=[struct.unpack_from('<I',data,chunks[1][1]+j*4)[0] for j in range(ni//4)]
                                    policy_meshes.append((label,nv//12,ni//4,all(index<nv//12 for index in indices)))
                        policy_layouts.append((label,"/".join(x[0] for x in idtl),"/".join(detail)))
            if label=="FORM:DATA":
                for child_label,cstart,cend in sub:
                    if child_label!="CHLD":raise ValueError("non-CHLD DATA item")
                    raw=data[cstart:cend]
                    if len(raw)<5 or raw[-1]!=0: raise ValueError("CHLD unterminated or short")
                    ref=raw[4:-1].decode("ascii","replace").lower().replace("\\","/")
                    refs.append(ref);child_prefixes.append(raw[:4].hex())
        elif label=="INFO":
            raw=data[start:end]
            if len(raw)%12: raise ValueError("INFO not triple of four-byte values")
            info_count=len(raw)//12
            info_ids=[struct.unpack_from('<I',raw,n*12)[0] for n in range(info_count)]
            intervals=[struct.unpack_from('<ff',raw,n*12+4) for n in range(info_count)]
        elif label=="PIVT":pivt_sizes.append(data[start:end].hex())
        else:misc.append((label,end-start))
    return version, signature, nested, refs, info_count, info_ids, intervals, pivt_sizes, child_prefixes, misc, policy_layouts, policy_meshes

def main():
    p=argparse.ArgumentParser(); p.add_argument("root",type=Path); p.add_argument("cfg",type=Path); p.add_argument("output",type=Path); a=p.parse_args()
    configured=[]
    for line in a.cfg.read_text(errors="replace").splitlines():
        m=re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)",line,re.I)
        if m and (a.root/m.group(2)).exists(): configured.append((int(m.group(1)),a.root/m.group(2)))
    winners=set(); rows=[]; versions=Counter(); sigs=Counter(); nested_sigs=Counter(); errors=[]; tomb=0; counts=Counter(); infos=Counter(); pivts=Counter(); child_flags=Counter(); suffixes=Counter(); live=set(); missing=[]; id_misses=[]; dup_child_ids=[]; nonfinite=[]; reversed_interval=[]; first_intervals=[]; layouts=Counter(); geometries=[]; no_render_copies=[]
    for priority,archive in sorted(configured,reverse=True):
        for name,usize,kind,packed in archive_records(archive):
            if name in ("appearance/no_render.apt","appearance/no_render.iff"):
                body=expand(kind,packed,usize) if packed else b""
                name_at=body.find(b"NAME")
                target=body[name_at+8:].split(b"\0",1)[0].decode("ascii","replace") if name_at>=0 else ""
                no_render_copies.append((name,archive.name,priority,usize,kind,len(packed),body[:24].hex(),target))
            if name in winners: continue
            winners.add(name)
            if packed:live.add(name)
            if not name.endswith(".lod"): continue
            if not packed: tomb+=1; continue
            try:
                version,sig,nested,refs,info_count,info_ids,intervals,pivt_sizes,flags,misc,policy_layouts,policy_meshes=parse(expand(kind,packed,usize))
                rows.append((archive.name,priority,name,version,sig,";".join(f"{x}:{y}" for x,y,_ in nested),";".join(refs),info_count,";".join(flags),";".join(str(x) for x in info_ids),";".join(f"{low:g}:{high:g}" for low,high in intervals)))
                versions[version]+=1; sigs[(version,sig)]+=1
                layouts.update(policy_layouts)
                geometries.extend(policy_meshes)
                ids=[int.from_bytes(bytes.fromhex(f),"little") for f in flags]
                if len(ids)!=len(set(ids)):dup_child_ids.append(name)
                if any(id not in info_ids for id in ids):id_misses.append(name)
                if any(not math.isfinite(x) for pair in intervals for x in pair):nonfinite.append(name)
                if any(low>high for low,high in intervals):reversed_interval.append(name)
                if intervals:first_intervals.append(intervals[0])
                counts[len(refs)]+=1;infos[info_count]+=1;pivts.update(pivt_sizes);child_flags.update(flags);suffixes.update(Path(ref).suffix for ref in refs)
                for label,subsig,count in nested: nested_sigs[(label,subsig,count)]+=1
            except Exception as exc:
                raw=expand(kind,packed,usize)
                errors.append((archive.name,name,str(exc),f"bytes={len(raw)}",f"prefix={raw[:24].hex()}"))
    a.output.parent.mkdir(parents=True,exist_ok=True)
    with a.output.open("w",newline="",encoding="utf-8") as f:
        w=csv.writer(f);w.writerow(("archive","priority","name","version","signature","nested_signatures","child_refs","info_triples","child_prefixes","info_ids","intervals"));w.writerows(rows)
    print("configured",len(configured),"live",len(rows),"tombstones",tomb,"errors",len(errors))
    print("versions",dict(versions))
    print("child_counts",dict(sorted(counts.items())),"info_triples",dict(sorted(infos.items())),"pivt_values",dict(pivts))
    print("child_prefixes",dict(child_flags),"child_suffixes",dict(suffixes))
    for layout,count in layouts.most_common(12):print("POLICY_LAYOUT",count,*layout,sep="\t")
    if geometries:print("policy_geometries",len(geometries),"invalid_indices",sum(not row[3] for row in geometries),"vertex_range",min(row[1] for row in geometries),max(row[1] for row in geometries),"index_range",min(row[2] for row in geometries),max(row[2] for row in geometries))
    print("child_ids_absent_from_INFO",len(id_misses),"duplicate_child_id_files",len(dup_child_ids))
    print("nonfinite_interval_files",len(nonfinite),"reversed_interval_files",len(reversed_interval))
    if first_intervals:print("first_interval_range",min(x[0] for x in first_intervals),max(x[0] for x in first_intervals),min(x[1] for x in first_intervals),max(x[1] for x in first_intervals))
    for item in id_misses[:10]:print("ID_MISS",item,sep="\t")
    for item in dup_child_ids[:12]:print("DUP_CHILD_ID",item,sep="\t")
    for item in reversed_interval[:10]:print("REVERSED_INTERVAL",item,sep="\t")
    missing_positions=Counter()
    for row in rows:
        for index,ref in enumerate(filter(None,row[6].split(";"))):
            full=ref if ref.startswith("appearance/") else "appearance/"+ref
            if full not in live:
                missing.append((row[2],ref));missing_positions[(index,len(row[6].split(";")))]+=1
    print("missing_references",len(missing),"unique",len(set(x[1] for x in missing)))
    print("missing_parent_files",len(set(x[0] for x in missing)),"missing_positions",dict(sorted(missing_positions.items())))
    missing_per_parent=Counter(x[0] for x in missing)
    all_missing=[row[2] for row in rows if missing_per_parent[row[2]]==len(row[6].split(";"))]
    print("parents_with_all_children_missing",len(all_missing))
    for name in all_missing[:8]:print("ALL_MISSING",name,sep="\t")
    tomb_refs=sum((ref if ref.startswith("appearance/") else "appearance/"+ref) in winners for _,ref in missing)
    print("missing_due_to_overlay_tombstone",tomb_refs,"missing_name_entirely",len(missing)-tomb_refs)
    print("no_render_apt_archive_copies",no_render_copies)
    print("missing_by_suffix",dict(Counter(Path(x[1]).suffix for x in missing)))
    for ref,count in Counter(x[1] for x in missing).most_common(15):print("MISSING_TOP",count,ref,sep="\t")
    for item in missing[:15]:print("MISSING",*item,sep="\t")
    for key,count in sigs.most_common(30):print("SIGNATURE",count,*key,sep="\t")
    for key,count in nested_sigs.most_common(30):print("NESTED",count,*key,sep="\t")
    for row in errors[:15]:print("ERROR",*row,sep="\t")
if __name__=="__main__":main()
