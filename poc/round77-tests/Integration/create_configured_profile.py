"""Create an explicit configured offline host profile; data files stay in fresh private output."""
import argparse,json,math,runpy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
DEFAULT=dict(Version=1,MaximumUpdates=6000,MaximumClickAttempts=128,StepMicroseconds=10000,WalkSpeed=3,RunSpeed=4,ArrivalRadius=.001,HoldMissingDestination=True)
def unique(pairs):
    result={}
    for key,value in pairs:
        if key in result:raise ValueError('Duplicate property')
        result[key]=value
    return result
def f(value):return struct.unpack('<f',struct.pack('<f',value))[0]
def bits(value):return struct.unpack('<I',struct.pack('<f',value))[0]
def settings(value):
    if set(value)!=set(DEFAULT):raise ValueError('Exact explicit settings fields required')
    for key in ('Version','MaximumUpdates','MaximumClickAttempts','StepMicroseconds'):
        if type(value[key]) is not int:raise ValueError('Integer field required')
    if value['Version']!=1 or not 1<=value['MaximumUpdates']<=6000 or not 1<=value['MaximumClickAttempts']<=128 or value['StepMicroseconds']!=10000:raise ValueError('Integer range')
    for key in ('WalkSpeed','RunSpeed','ArrivalRadius'):
        if type(value[key]) not in (float,int) or not math.isfinite(f(value[key])):raise ValueError('Finite numeric field required')
    if not 0<f(value['WalkSpeed'])<=10 or not f(value['WalkSpeed'])<=f(value['RunSpeed'])<=10 or not f(.0001)<=f(value['ArrivalRadius'])<=1 or type(value['HoldMissingDestination']) is not bool:raise ValueError('Movement range')
    return value
def create(source,value,replay=False,synthetic=False):
    if source.get('Version')!=12 or source.get('Policy')!='SuppliedIdleOfflinePlayground':raise ValueError('Accepted Version12 offline profile required')
    value=settings(value)
    if synthetic and not replay:raise ValueError('Synthetic timing requires replay')
    if replay and (value['MaximumUpdates']!=6000 or value['MaximumClickAttempts']!=32 or f(value['WalkSpeed'])!=3 or f(value['RunSpeed'])!=4 or not value['HoldMissingDestination']):raise ValueError('Replay has explicit6000/32/3/4/hold policy')
    result=dict(source,Version=13,Policy='SuppliedConfiguredOfflinePlayground',PlaygroundSettings=value,MaximumUpdates=value['MaximumUpdates'],SpeedBits=bits(value['WalkSpeed']),WalkSpeedBits=bits(value['WalkSpeed']),RunSpeedBits=bits(value['RunSpeed']),ArrivalRadiusBits=bits(value['ArrivalRadius']),InteractiveReplay=replay,SyntheticFrameClock=synthetic)
    for key in ('SoeRequestHex','SoeResponseHex'):result.pop(key,None)
    return result
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--source',type=Path,required=True);p.add_argument('--settings',type=Path);p.add_argument('--output',type=Path,required=True);p.add_argument('--replay',action='store_true');p.add_argument('--synthetic',action='store_true');a=p.parse_args()
    if a.source.stat().st_size>16384 or a.settings and a.settings.stat().st_size>1024:raise ValueError('Bounded source/settings required')
    source=json.loads(a.source.read_text(encoding='utf-8-sig'),object_pairs_hook=unique)
    value=json.loads(a.settings.read_text(encoding='utf-8'),object_pairs_hook=unique) if a.settings else dict(DEFAULT)
    result=create(source,value,a.replay,a.synthetic)
    target=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.output,ROOT)
    target.write_text(json.dumps(result,separators=(',',':'),allow_nan=False)+'\n',encoding='utf-8');print('Configured offline profile created')
