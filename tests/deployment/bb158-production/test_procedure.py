import importlib.util
import copy
import json
from pathlib import Path
import tempfile
import unittest
from types import SimpleNamespace
from unittest.mock import patch

spec=importlib.util.spec_from_file_location('procedure',Path(__file__).with_name('execute.py'))
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)

class Activity(unittest.TestCase):
    def setUp(self):
        self.jobs={'jobs':[{'provider':'qBittorrent','status':'downloading',
                           'downloadSpeedBytesPerSecond':None}]}
        self.download={'status':'active','category':'audiobooks',
                       'diagnosis':{'code':'waitingForMetadata'},
                       'downloadedBytes':0,'downloadSpeedBytesPerSecond':0}

    def check(self, downloads=None):
        with patch.object(m,'read_api',side_effect=[
                self.jobs, {'downloads':downloads if downloads is not None else [self.download]}]):
            m.activity()

    def test_nullable_display_speed_requires_numeric_queue_evidence(self):
        self.check()

    def test_explicit_zero_display_speed(self):
        self.jobs['jobs'][0]['downloadSpeedBytesPerSecond']=0
        self.check()

    def test_positive_job_speed_rejected(self):
        self.jobs['jobs'][0]['downloadSpeedBytesPerSecond']=1
        with self.assertRaises(AssertionError):self.check()

    def test_missing_queue_evidence_rejected(self):
        with self.assertRaises(AssertionError):self.check([])

    def test_unknown_or_positive_queue_measurements_rejected(self):
        for field in ('downloadedBytes','downloadSpeedBytesPerSecond'):
            for value in (None,1,-1,'0',False):
                with self.subTest(field=field,value=value):
                    row=copy.deepcopy(self.download);row[field]=value
                    with self.assertRaises(AssertionError):self.check([row])

    def test_missing_measurement_rejected(self):
        for field in ('downloadedBytes','downloadSpeedBytesPerSecond'):
            row=copy.deepcopy(self.download);del row[field]
            with self.subTest(field=field),self.assertRaises(KeyError):self.check([row])

    def test_other_category_cannot_hide_active_transfer(self):
        row=copy.deepcopy(self.download);row['category']='other';row['downloadSpeedBytesPerSecond']=1
        with self.assertRaises(AssertionError):self.check([self.download,row])

    def test_non_metadata_activity_rejected(self):
        self.download['diagnosis']['code']='downloading'
        with self.assertRaises(AssertionError):self.check()

    def test_other_provider_or_job_state_rejected(self):
        for field,value in (('provider','Librarr'),('status','importing')):
            with self.subTest(field=field):
                original=copy.deepcopy(self.jobs)
                self.jobs['jobs'][0][field]=value
                with self.assertRaises(AssertionError):self.check()
                self.jobs=original

    def test_upstream_failure_rejected(self):
        with patch.object(m,'read_api',side_effect=[self.jobs,TimeoutError]),self.assertRaises(TimeoutError):
            m.activity()

class Recovery(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        self.patches=[patch.object(m,'ROOT',self.root),patch.object(m,'STATE',self.root/'state.json')]
        for p in self.patches:p.start();self.addCleanup(p.stop)
        self.old={'Id':'fixture','Image':'old-image','Config':{'Labels':{'com.docker.compose.service':'librarr'}},'HostConfig':{},'Mounts':[],'State':{'Running':False,'ExitCode':0,'OOMKilled':False}}
        self.plan={'run':'r','originals':[self.old]}
    def state(self,phase,inv='i'):
        m.save(m.STATE,{'run':'r','invocation':inv,'planHash':'h','role':'librarr','phase':phase})
    def test_clean_old_only_restart(self):
        self.state('stopped-clean');running=copy.deepcopy(self.old);running['State']['Running']=True
        with patch.object(m,'inspect',side_effect=[self.old,running]),patch.object(m,'command') as cmd:
            m.recover(self.plan,'i','h');cmd.assert_called_once_with(['docker','start','fixture'],timeout=30)
    def test_already_running_no_restart(self):
        self.state('old-running');i=copy.deepcopy(self.old);i['State']['Running']=True
        with patch.object(m,'inspect',return_value=i),patch.object(m,'command') as cmd:
            m.recover(self.plan,'i','h');cmd.assert_not_called()
    def test_uncertain_replacement_never_rolls_back(self):
        self.state('replacement-possible')
        with patch.object(m,'command') as cmd,self.assertRaises(AssertionError):
            m.recover(self.plan,'i','h')
        cmd.assert_not_called()
    def test_stale_invocation_never_starts(self):
        self.state('stopped-clean','stale')
        with patch.object(m,'command') as cmd,self.assertRaises(AssertionError):m.recover(self.plan,'i','h')
        cmd.assert_not_called()
    def test_changed_identity_never_starts(self):
        self.state('stopped-clean');changed=copy.deepcopy(self.old);changed['Image']='other'
        with patch.object(m,'inspect',return_value=changed),patch.object(m,'command') as cmd,self.assertRaises(AssertionError):m.recover(self.plan,'i','h')
        cmd.assert_not_called()
    def test_unclean_exit_never_starts(self):
        self.state('stopped-clean');bad=copy.deepcopy(self.old);bad['State']['ExitCode']=137
        with patch.object(m,'inspect',return_value=bad),patch.object(m,'command') as cmd,self.assertRaises(AssertionError):m.recover(self.plan,'i','h')
        cmd.assert_not_called()
    def test_pending_stop_is_reconciled(self):
        self.state('stopping-old');running=copy.deepcopy(self.old);running['State']['Running']=True
        with patch.object(m,'inspect',side_effect=[running,self.old,running]),patch.object(m,'command') as cmd:
            m.recover(self.plan,'i','h')
        self.assertEqual(cmd.call_args_list[0].args[0],['docker','wait','fixture'])
        self.assertEqual(cmd.call_args_list[1].args[0],['docker','start','fixture'])
    def test_completed_execution_no_action(self):
        self.state('complete')
        with patch.object(m,'command') as cmd:m.recover(self.plan,'i','h')
        cmd.assert_not_called()


class Execution(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name);self.calls=[]
        self.old=[];self.new={}
        for role in ('librarr','api','web'):
            i={'Id':role+'-old','Image':'old-'+role,'Config':{'Env':['API_KEY=synthetic'], 'StopTimeout':1,'Labels':{'com.docker.compose.service':role}},'HostConfig':{},'Mounts':[],'State':{'Running':True,'StartedAt':'original','ExitCode':0,'OOMKilled':False}}
            self.old.append(i);n=copy.deepcopy(i);n['Id']=role+'-new';n['Image']='candidate-'+role;n['NetworkSettings']={'Networks':{'test':{'IPAddress':'127.0.0.1'}}};self.new[n['Id']]=n
        self.live={i['Id']:copy.deepcopy(i) for i in self.old}|self.new
        pin=self.root/'pins.json';m.save(pin,{'services':{r:{'image':'old-'+r} for r in ('librarr','api','web')}})
        self.plan={'run':'fixture','originals':self.old,'inputHashes':{},'candidates':{r:'candidate-'+r for r in ('librarr','api','web')},'composeCommand':['compose-fixture'],'runtimeOverride':str(pin)}
        def command(args,timeout=60):
            self.calls.append(args)
            if args[:3]==['docker','image','inspect']:return json.dumps([{'Id':args[3]}]).encode()
            if args[:2]==['docker','stop']:self.live[args[-1]]['State']['Running']=False
            if args[:2]==['docker','start']:self.live[args[-1]]['State']['Running']=True
            if args[:3]==['compose-fixture','ps','-q']:return (args[-1]+'-new').encode()
            return b''
        class Response:
            def __enter__(self):return self
            def __exit__(self,*a):pass
            def read(self):return b'{"contract":"bb158-direct-audio-v2","maximumBytes":67108864,"formats":["mp3","m4a"]}'
        for p in [patch.object(m.os,'statvfs',return_value=SimpleNamespace(f_bavail=100*1024**3,f_frsize=1)),patch.object(m,'ROOT',self.root),patch.object(m,'STATE',self.root/'state.json'),patch.object(m,'inspect',side_effect=lambda cid:copy.deepcopy(self.live[cid])),patch.object(m,'command',side_effect=command),patch.object(m,'activity'),patch.object(m,'backup'),patch.object(m,'verify_backup'),patch.object(m,'wait_healthy',side_effect=lambda cid:self.live[cid]),patch.object(m.urllib.request,'urlopen',return_value=Response())]:
            p.start();self.addCleanup(p.stop)
    def test_exact_order_capture_restart_then_replace(self):
        m.execute(self.plan,'i','h')
        operations=[('replace' if a[0]=='compose-fixture' else a[1],a[-1]) for a in self.calls if a[:2] in (['docker','stop'],['docker','start'],['compose-fixture','up'])]
        self.assertEqual(operations,[('stop','librarr-old'),('start','librarr-old'),('replace','librarr'),('stop','api-old'),('start','api-old'),('replace','api'),('replace','web')])
        self.assertEqual(json.loads((self.root/'complete.json').read_text())['status'],'DEPLOYED_IMPORT_DISABLED')
    def test_bad_backup_blocks_replacement_after_old_restart(self):
        with patch.object(m,'verify_backup',side_effect=AssertionError),self.assertRaises(AssertionError):m.execute(self.plan,'i','h')
        self.assertTrue(self.live['librarr-old']['State']['Running'])
        self.assertFalse(any(a[:2]==['compose-fixture','up'] for a in self.calls))
    def test_drift_prevents_any_stop(self):
        self.live['api-old']['Image']='unexpected'
        with self.assertRaises(AssertionError):m.execute(self.plan,'i','h')
        self.assertEqual(self.calls,[])
    def test_activity_failure_prevents_state_and_stop(self):
        with patch.object(m,'activity',side_effect=AssertionError('QueueSpeedActiveOrUnknown')):
            with self.assertRaises(AssertionError):m.execute(self.plan,'i','h')
        self.assertEqual(self.calls,[])
        self.assertFalse(m.STATE.exists())

if __name__=='__main__':unittest.main()
