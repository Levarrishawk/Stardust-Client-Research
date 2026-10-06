"""Derive a bounded, idle-start modern profile from the accepted offline Version11 profile."""
import argparse,json,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def create(source,replay=False,synthetic=False):
    if source.get('Version')!=11 or source.get('Policy')!='SuppliedOfflineSessionControls':raise ValueError('Accepted Version11 offline profile required')
    if synthetic and not replay:raise ValueError('Synthetic timing requires explicit replay')
    result=dict(source,Version=12,Policy='SuppliedIdleOfflinePlayground',MaximumUpdates=6000,SpeedBits=source['WalkSpeedBits'],InteractiveReplay=replay,SyntheticFrameClock=synthetic)
    for key in ('SoeCadenceAckHex','SoeAckHex','SessionStopAtStep'):result.pop(key,None)
    return result
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--source',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--replay',action='store_true');p.add_argument('--synthetic',action='store_true');a=p.parse_args()
    if a.source.stat().st_size>16384:raise ValueError('Bounded input profile required')
    value=create(json.loads(a.source.read_text(encoding='utf-8-sig')),a.replay,a.synthetic)
    target=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.output,ROOT)
    target.write_text(json.dumps(value,separators=(',',':'))+'\n',encoding='utf-8')
    print('Idle offline profile created: 6000 updates, 16 terrain click attempts; replay='+str(a.replay))
