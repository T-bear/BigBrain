import contextlib
import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import deploy as d

class ProcedureTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name)
        self.stack=contextlib.ExitStack();self.addCleanup(self.stack.close)
        self.stack.enter_context(patch.object(d,'ROOT',self.root))
        self.stack.enter_context(patch.object(d,'command',side_effect=AssertionError('No real commands in tests')))
        self.p={'run':'run','targets':{},'candidates':{'api':'new-api','web':'new-web'},'originals':[],'drafts':[],'runtimeOverride':str(self.root/'override.json')}
        for role in d.ROLES:
            self.p['targets'][role]={'Id':'old-'+role,'Image':'old-image-'+role,'Config':{'StopTimeout':35},'State':{'Running':True,'ExitCode':0,'OOMKilled':False,'Status':'running'}}
        pins={'services':{'api':{'image':'old-image-api','environment':{d.FLAG:'true','preserved':'yes'}},'web':{'image':'old-image-web'},'librarr':{'image':'untouched'}}}
        d.save(self.root/'override.json',pins)
        self.p['inputHashes']={str(self.root/'override.json'):d.digest(self.root/'override.json')}
        self.live=copy.deepcopy(self.p['targets']);self.calls=[]

    def mock(self,name,**kwargs):return self.stack.enter_context(patch.object(d,name,**kwargs))

    def prepare_work(self):
        for name in ['preflight','imports','activity','unchanged','verify_backup','backup','wait_healthy','verify_service','verify_api','verify_web']:
            self.mock(name)
        self.mock('fingerprint',side_effect=lambda c:c['Id'])
        self.mock('inspect',side_effect=lambda cid:copy.deepcopy(next(c for c in self.live.values() if c['Id']==cid)))
        def docker(*args,**kwargs):
            self.calls.append(args)
            if args[0] in ('stop','wait'):self.live['api']['State'].update(Running=False,Status='exited')
            elif args[0]=='start':self.live['api']['State'].update(Running=True,Status='running')
            else:raise AssertionError('Unexpected Docker operation')
            return b''
        self.mock('docker',side_effect=docker)
        def compose(p,*args,**kwargs):
            self.calls.append(args)
            assert args[:8]==('up','-d','--no-deps','--no-build','--pull','never','--wait','--wait-timeout')
            assert args[-1] in d.ROLES
            role=args[-1];self.live[role]['Id']='new-'+role;self.live[role]['Image']=p['candidates'][role]
            return b''
        self.mock('compose',side_effect=compose)
        self.mock('current',side_effect=lambda p,role:copy.deepcopy(self.live[role]))

    def test_api_then_web_with_optin_and_other_pins_preserved(self):
        self.prepare_work();d.work(self.p,'hash','inv')
        self.assertEqual([x[-1] for x in self.calls if x[0]=='up'],['api','web'])
        pins=json.loads((self.root/'override.json').read_text())
        self.assertEqual(pins['services']['api']['environment'],{d.FLAG:'true','preserved':'yes'})
        self.assertEqual(pins['services']['librarr']['image'],'untouched')
        self.assertEqual(json.loads((self.root/'complete.json').read_text())['status'],'DOI_OPTIONAL_LIVE_READY')

    def test_failed_preflight_never_stops(self):
        self.prepare_work();d.preflight.side_effect=AssertionError('drift')
        with self.assertRaises(AssertionError):d.work(self.p,'hash','inv')
        self.assertEqual(self.calls,[]);self.assertFalse((self.root/'state.json').exists())

    def test_failed_backup_records_stopped_api_for_recovery(self):
        self.prepare_work();d.backup.side_effect=TimeoutError()
        with self.assertRaises(TimeoutError):d.work(self.p,'hash','inv')
        self.assertEqual(json.loads((self.root/'state.json').read_text())['phase'],'stopped-clean')
        d.recover(self.p,'hash','inv')
        self.assertTrue(self.live['api']['State']['Running'])
        self.assertEqual([x for x in self.calls if x[0]=='up'],[])

    def test_api_verification_failure_does_not_replace_web_or_repin(self):
        self.prepare_work();d.verify_api.side_effect=AssertionError('probe')
        with self.assertRaises(AssertionError):d.work(self.p,'hash','inv')
        self.assertEqual([x[-1] for x in self.calls if x[0]=='up'],['api'])
        self.assertEqual(json.loads((self.root/'override.json').read_text())['services']['api']['image'],'old-image-api')
        d.recover(self.p,'hash','inv')
        self.assertEqual(json.loads((self.root/'recovery.json').read_text())['outcome'],'SERVICES_RETAINED_MANUAL_REVIEW')
        self.assertEqual(len([x for x in self.calls if x[0]=='up']),1)

    def test_success_recovery_does_not_restart_or_restore(self):
        self.prepare_work();d.work(self.p,'hash','inv');count=len(self.calls)
        d.recover(self.p,'hash','inv')
        self.assertEqual(len(self.calls),count)
        self.assertEqual(json.loads((self.root/'recovery.json').read_text())['outcome'],'COMPLETE_NO_ACTION')

    def test_replay_refused(self):
        self.prepare_work();d.work(self.p,'hash','inv');count=len(self.calls)
        with self.assertRaises(AssertionError):d.work(self.p,'hash','inv')
        self.assertEqual(count,len(self.calls))

    def test_stale_invocation_never_recovers(self):
        self.prepare_work();d.record(self.p,'hash','old-inv','api','stopped-clean')
        with self.assertRaises(AssertionError):d.recover(self.p,'hash','new-inv')
        self.assertEqual(self.calls,[])

    def test_interrupted_stop_waits_for_daemon_before_restarting_exact_old_api(self):
        self.prepare_work();d.record(self.p,'hash','inv','api','stopping-old')
        d.recover(self.p,'hash','inv')
        self.assertEqual([x[0] for x in self.calls],['wait','start'])
        self.assertTrue(self.live['api']['State']['Running'])

    def test_unclean_old_exit_is_not_restarted(self):
        self.prepare_work();d.record(self.p,'hash','inv','api','stopped-clean')
        self.live['api']['State'].update(Running=False,ExitCode=1,Status='exited')
        with self.assertRaises(AssertionError):d.recover(self.p,'hash','inv')
        self.assertEqual(self.calls,[])

    def test_import_activity_or_history_drift_blocks(self):
        for status in ['downloading','importing','indexing','uncertain','confirmed']:
            with self.subTest(status=status),patch.object(d,'request',return_value=(200,json.dumps([{'status':status}]).encode())):
                with self.assertRaises(AssertionError):d.imports(self.p)

    def test_no_stop_marker_requires_no_action(self):
        self.prepare_work();d.recover(self.p,'hash','inv')
        self.assertEqual(self.calls,[])
        self.assertEqual(json.loads((self.root/'recovery.json').read_text())['outcome'],'NO_STOP_RECORDED')

if __name__=='__main__':unittest.main()
