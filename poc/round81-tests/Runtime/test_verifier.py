"""Authored synthetic parser tests; these are not native input evidence."""
import copy,json,unittest
from verify_fresh import expected,verify_runs

PROFILE=dict(Version=13,Policy='SuppliedConfiguredOfflinePlayground',PlaygroundSettings=dict(Version=1,MaximumUpdates=3,MaximumClickAttempts=8),OrbitYaw=7,OrbitPitch=.6,OrbitDistance=18,OrbitMinimumPitch=-1,OrbitMaximumPitch=1,OrbitMinimumDistance=2,OrbitMaximumDistance=30)
def fixture(reason='BudgetCompleted',identity=1):
    state=expected(PROFILE);state.update(LocalClock=10,OfflineStartClock=10,DriverClock=10,PreviewInstanceId=identity,CameraInstanceId=identity+100,SceneGeneration=1)
    return [('offline_fresh_preview_state',state),('offline_completion_summary',dict(Reason=reason,CompletedUpdates=2,AttemptedUpdates=3)),('configured_session_owner_cleanup',dict(Disposed=True,OwnedPointDisposed=True,OwnedControllerDisposed=True)),('configured_root_owner_cleanup',dict(Disposed=True,PendingMessages=0,RetainedPackets=0)),('manual_outbound_queue_cleanup',dict(Count=0,QueuedBytes=0)),*[(k,dict(Closed=True)) for k in ['manual_input_host_cleanup','run_input_host_cleanup','camera_orbit_cleanup']]]
def encode(rows):return '\n'.join(k+'='+json.dumps(v) for k,v in rows)
class Tests(unittest.TestCase):
    def test_normal_fault_repeated(self):
        self.assertTrue(verify_runs([encode(fixture()),encode(fixture('Faulted',2))],[PROFILE,PROFILE],True)['Passed'])
    def test_negative_state(self):
        for key,value in [('CompletedUpdates',1),('AttemptedUpdates',1),('ManualHeld',1),('RunHeld',1),('OrbitPitch',0),('ClickAttempts',1),('ReservedSteps',1),('RootNextSequence',1),('QueueBytes',1),('DriverClock',11),('OwnedPoint',1),('SceneGeneration',0),('LocalClock',True)]:
            with self.subTest(key=key):
                rows=fixture();rows[0][1][key]=value
                with self.assertRaises(AssertionError):verify_runs([encode(rows)],[PROFILE])
    def test_missing_cleanup(self):
        for index in range(2,len(fixture())):
            rows=fixture();del rows[index]
            with self.assertRaises(AssertionError):verify_runs([encode(rows)],[PROFILE])
    def test_boolean_substitutions(self):
        for key,value in [('Running',0),('Paused',0.0),('OwnedController',1),('Speed',False)]:
            rows=fixture();rows[0][1][key]=value
            with self.assertRaises(AssertionError):verify_runs([encode(rows)],[PROFILE])
    def test_recycled_individual_instances(self):
        for key in ['PreviewInstanceId','CameraInstanceId']:
            first=fixture();second=fixture(identity=2);second[0][1][key]=first[0][1][key]
            with self.assertRaises(AssertionError):verify_runs([encode(first),encode(second)],[PROFILE,PROFILE],True)
    def test_undrained_closed_duplicate(self):
        for index,key,value in [(2,'Disposed',False),(3,'PendingMessages',1),(4,'Count',1),(5,'Closed',False),(6,'Closed',False),(7,'Closed',False)]:
            rows=fixture();rows[index][1][key]=value
            with self.assertRaises(AssertionError):verify_runs([encode(rows)],[PROFILE])
        with self.assertRaises(AssertionError):verify_runs([encode(fixture())]*2,[PROFILE]*2,True)
        rows=fixture();rows.append(copy.deepcopy(rows[0]))
        with self.assertRaises(AssertionError):verify_runs([encode(rows)],[PROFILE])
if __name__=='__main__':unittest.main()
