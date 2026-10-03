"""Independent synthetic mixed presentation peer and modern coordinator observation model."""
import argparse
import copy
import importlib.util
import json
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('r14_independent_peer', ROOT/'poc/round14-tests/Networking/peer.py')
wire = importlib.util.module_from_spec(spec)
spec.loader.exec_module(wire)
P = lambda fmt, *v: struct.pack('<'+fmt, *v)
H = 0xF122334455667788
T = 0xFE02030405060708


def envelope(oid, opcode, tag, count, body):
    return P('HIQIBIH', 5, opcode, oid, tag, 3, len(body)+2, count)+body


def produce():
    baseline = envelope(H, 0x68A75F0C, 0x4352454F, 18,
        P('fHIHIIHIIIIIIBBBQfIQII', 1,0,0,0,0,1,0,0,7,0,0,0,100,1,4,9,
          0xF102030405060708,1,0xF1234567,0xFEDCBA9876543210,0,9))
    apps = {'start': P('HIBQ',9,0x3AE6DFAE,165,H)+P('H',3)+b'A/B'+P('3f',0,0,0)+P('H',8)+b'template'+P('Q',0),
        'ready': bytes.fromhex('0100221CFD43'),
        'humanCreate': wire.application('create',H,[0x80000000,0x3E800000,0xBF000000,0x40000000,0xBFC00000,1,0xC2350000],2937962913,255),
        'terminalCreate': wire.application('create',T,[0,0,0,0x3F800000,0x3FC00000,0x80000000,0x41200000],2907007746,165),
        'humanBaseline': baseline,
        'terminalBaseline': envelope(T,0x68A75F0C,0x54414E4F,65535,b'SYNTHETIC OPAQUE TANO3\0\xff'),
        'terminalDelta': envelope(T,0x12862153,0x54414E4F,65535,b'SYNTHETIC STATE\x80'),
        'terminalBadFamily': envelope(T,0x68A75F0C,0x4352454F,18,baseline[25:]),
        'heightA': envelope(H,0x12862153,0x4352454F,1,P('HI',14,0x3FB33333)),
        'heightB': envelope(H,0x12862153,0x4352454F,1,P('HI',14,0x40000000)),
        'lateIntactBad': envelope(H,0x12862153,0x4352454F,2,P('HBHI',11,9,14,0x7FC00000))}
    for name, oid in [('human',H),('terminal',T)]:
        apps[name+'End'] = P('HIQ',2,0x2C436037,oid)
        apps[name+'Destroy'] = P('HIQB',3,0x4D45D504,oid,255)
    initial = ['start','humanCreate','terminalCreate','humanBaseline','terminalBaseline','humanEnd','terminalEnd']
    frames = [wire.frame(struct.pack('>HH',9,i)+apps[name]) for i,name in enumerate(initial)]
    bad = bytearray(frames[0]); bad[-1] ^= 1
    actions = []
    def add(name, op, key=None, job=None):
        actions.append(dict(Name=name, Op=op, Key=key, Job=job))
    add('staged','stage'); add('barrier','barrier'); add('server-ready','accept','ready')
    add('initial-held','hold','initial'); add('start-delayed','poll')
    add('state-while-delayed','accept','heightA'); add('initial-stale-ack','ack','initial')
    add('latest-state','hold','latest'); add('latest-state-ack','ack','latest')
    add('destroy-before-completion','accept','humanDestroy'); add('human-recreate','accept','humanCreate')
    add('human-reseed','accept','humanBaseline'); add('human-refinalize','accept','humanEnd'); add('replacement-start','poll')
    add('old-success','complete',job=0); add('old-retired','poll')
    add('terminal-success','complete',job=1); add('new-human-success','complete',job=2); add('publish-current','poll')
    add('recreated-batch','hold','recreated'); add('recreated-ack','ack','recreated')
    add('human-new-state','accept','heightB'); add('held-new-state','hold','held')
    add('terminal-opaque-state','accept','terminalDelta'); add('held-stale-ack','ack','held')
    add('foreign-ack','foreign'); add('retry-latest','hold','retry'); add('retry-current-ack','ack','retry')
    add('terminal-only-destroy','accept','terminalDestroy'); add('terminal-only-recreate','accept','terminalCreate')
    add('terminal-only-reseed','accept','terminalBaseline'); add('terminal-only-refinalize','accept','terminalEnd')
    add('terminal-replacement-start','poll'); add('terminal-replacement-success','complete',job=3); add('terminal-replacement-publish','poll')
    add('terminal-latest','hold','terminal'); add('terminal-current-ack','ack','terminal')
    add('pending-before-failure','accept','heightA'); add('family-failure','fail','terminalBadFamily'); add('closed','close')
    return dict(Version=1, Label='SYNTHETIC OFFLINE PRESENTATION PEER; TANO PAYLOAD OPAQUE',
        Applications={k:v.hex().upper() for k,v in apps.items()}, Initial=initial,
        SessionResponse=struct.pack('>HII3BI',2,0x12345678,wire.SEED,2,1,4,496).hex().upper(),
        Transcript=[bytes(bad).hex().upper()]+[frames[i].hex().upper() for i in [2,0,1,3,4,5,6,6]], Actions=actions)


class Model:
    def __init__(self, peer):
        self.peer=peer; self.objects={}; self.regrev=0; self.staterev=0; self.mutation=0
        self.pending={}; self.jobs=[]; self.publications=[]; self.held={}; self.state='Selecting'; self.generation=1
    def batch(self):
        if self.state not in ('Ready','AwaitingServerReady'): return None
        return dict(Generation=1, MutationRevision=self.mutation, Entries=[dict(ObjectId=oid,Kind=k[0],
            Current=copy.deepcopy(self.objects.get(oid))) for oid,k in sorted(self.pending.items(),key=lambda x:(x[1][1],x[0]))])
    def change(self,oid,kind):
        self.mutation+=1
        previous=self.pending.get(oid)
        self.pending[oid]=(kind | (previous[0] if previous else 0),previous[1] if previous else self.mutation)
    def apply(self,key):
        if key=='ready': self.state='Ready'; return
        data=bytes.fromhex(self.peer['Applications'][key]);oid=struct.unpack_from('<Q',data,6)[0]
        if key.endswith('Create'):
            self.regrev+=1
            self.objects[oid]=dict(ObjectId=oid,Revision=self.regrev,CreationRevision=self.regrev,
                RegistryGeneration=1,Phase='PendingBaselines',CreateHex=data.hex().upper(),ScalarRevision=None,Scalars=None,OpaqueHex=[])
            self.change(oid,1)
        elif key.endswith('End'):
            self.regrev+=1;self.objects[oid].update(Revision=self.regrev,Phase='Finalized');self.change(oid,1)
        elif key.endswith('Destroy'):
            del self.objects[oid]
            for j in self.jobs:
                if j['ObjectId']==oid and not j['Polled']:j['Canceled']=True
            self.change(oid,1)
        elif key=='humanBaseline':
            self.staterev+=1
            self.objects[oid].update(ScalarRevision=self.staterev,Scalars=dict(Posture=4,Rank=9,LinkId=0xF102030405060708,
                HeightBits=0x3F800000,Shock=0xF1234567,States=0xFEDCBA9876543210));self.change(oid,2)
        elif key in ('heightA','heightB'):
            index,bits=struct.unpack('<HI',data[25:]);assert index==14
            self.staterev+=1;self.objects[oid]['ScalarRevision']=self.staterev;self.objects[oid]['Scalars']['HeightBits']=bits;self.change(oid,2)
        else:
            self.objects[oid]['OpaqueHex'].append(data[25:].hex().upper());self.change(oid,2)
    def current(self,p):
        o=self.objects.get(p['ObjectId'])
        return self.state in ('Ready','AwaitingServerReady') and o is not None and o['Revision']==p['Revision']
    def event(self,a):
        op=a['Op'];result=None
        if op=='stage':self.state='Initializing';result=True
        elif op=='barrier':
            for key in self.peer['Initial'][1:]:self.apply(key)
            self.state='AwaitingServerReady';result=self.peer['Applications']['ready']
        elif op=='accept':self.apply(a['Key']);result=True
        elif op=='hold':self.held[a['Key']]=copy.deepcopy(self.batch());result=copy.deepcopy(self.held[a['Key']])
        elif op=='ack':
            result=self.state in ('Ready','AwaitingServerReady') and self.held[a['Key']]['MutationRevision']==self.mutation
            if result:self.pending.clear()
        elif op=='foreign':result=False
        elif op=='complete':self.jobs[a['Job']]['Completed']=True;result=True
        elif op=='poll':
            prepared=[];retired=[]
            for j in self.jobs:
                if j['Completed'] and not j['Polled']:
                    j['Polled']=True
                    ticket={k:j[k] for k in ('ObjectId','Revision','Attempt')}
                    if j['Canceled'] or not self.current(j):retired.append(dict(**ticket,Kind='Stale',HasResources=False))
                    else:prepared.append(ticket);self.publications.append(copy.deepcopy(ticket))
            for o in self.objects.values():
                if o['Phase']=='Finalized' and not any(j['ObjectId']==o['ObjectId'] and j['Revision']==o['Revision'] for j in self.jobs):
                    i=len(self.jobs)
                    self.jobs.append(dict(JobIndex=i,ObjectId=o['ObjectId'],Revision=o['Revision'],CreationRevision=o['CreationRevision'],
                        RegistryGeneration=1,Attempt=i+1,Completed=False,Canceled=False,Polled=False))
            result=dict(Prepared=prepared,Retired=retired)
        elif op=='fail':self.state='Failed';self.objects.clear();self.pending.clear();result=True
        elif op=='close':self.state='Closed';self.generation+=1;result=True
        return dict(Name=a['Name'],Result=result,State=self.state,Generation=self.generation,
            OutstandingJobs=sum(not j['Polled'] for j in self.jobs),Objects=copy.deepcopy([self.objects[k] for k in sorted(self.objects)]),
            Calls=[{k:v for k,v in j.items() if k not in ('Polled','Attempt')} for j in self.jobs],
            Publications=[dict(**p,Current=self.current(p)) for p in self.publications],Pending=self.batch())


def verify(peer,export):
    assert peer==produce(), 'Authored peer changed'
    model=Model(peer);expected=[model.event(a) for a in peer['Actions']]
    for actual,want in zip(export['Events'],expected,strict=True):
        assert actual==want, (want['Name'],actual,want)
    assert export['Delivered']==[peer['Applications'][k] for k in peer['Initial']]
    assert export['OriginalJobs']==2 and export['ArchiveScopes']==0 and export['FinalOutstanding']==0
    assert export['Driver']==[dict(ObjectId=H,Recipe='CreatureDetail0',CreateHex=peer['Applications']['humanCreate'],ExternalReads=39,SourceBytes=4686371),
        dict(ObjectId=T,Recipe='StaticAppearance',CreateHex=peer['Applications']['terminalCreate'],ExternalReads=27,SourceBytes=1401794)]
    assert export['Checks']==dict(StateDoesNotReload=True,OldCompletionRetired=True,HumanUnchangedOnTerminalRecreate=True,
        ForeignPublicationRejected=True,FamilyFailureClearsPending=True,ClosedDrained=True)
    assert export['Negatives']==dict(LateIntactNoReady=True,LateIntactNoBatch=True,CurrentFaultNoPreparedPrefix=True,
        CurrentFaultNoBatch=True,StaleGenerationBatchRejected=True)
    print(f"PASS independent mixed presentation observations: {len(expected)} events, delayed/recreated resources, stale/current/foreign batches and atomic failures")


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--generate',type=Path);parser.add_argument('--peer',type=Path);parser.add_argument('--verify',type=Path)
    args=parser.parse_args()
    if args.generate:
        args.generate.parent.mkdir(parents=True,exist_ok=True);args.generate.write_text(json.dumps(produce(),indent=2)+'\n',encoding='utf-8',newline='\n')
        print('PASS handpacked mixed presentation fixture; opaque static payload synthetic')
    else:verify(json.loads(args.peer.read_text()),json.loads(args.verify.read_text()))
