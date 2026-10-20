import copy
import importlib.util
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0,str(Path(__file__).parent))
spec=importlib.util.spec_from_file_location('activation',Path(__file__).with_name('activate_import.py'))
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)

class Activation(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name);p=patch.object(m,'ROOT',self.root);p.start();self.addCleanup(p.stop)
        self.old={'Id':'a'*64,'Image':'same-image','Config':{'Env':['UNCHANGED=value'],
            'Labels':{'com.docker.compose.service':'api','com.docker.compose.project':'fixture'}},'HostConfig':{},'Mounts':[],
            'NetworkSettings':{'Networks':{'existing':{}}},'State':{'Running':True,'Health':{'Status':'healthy'},'ExitCode':0,'OOMKilled':False}}
        self.new=copy.deepcopy(self.old);self.new['Id']='b'*64;self.new['Config']['Env'].append(m.FLAG+'=true')
        self.plan={'api':self.old,'project':'fixture','run':'r','originals':[self.old],'drafts':[],
            'runtimeOverride':str(self.root/'override.json'),'libraryCount':1}
        m.save(self.root/'override.json',{'services':{'api':{'image':'same-image'},'web':{'image':'unchanged'}}})
        self.plan['inputHashes']={str(self.root/'override.json'):m.hashlib.sha256((self.root/'override.json').read_bytes()).hexdigest()}
        self.commands=[]

    def compose(self,p,*args,**unused):
        self.commands.append(args)
        return b'b'*64 if args[0]=='ps' else b''

    def work(self,probe_code='draftNotFound'):
        with patch.object(m,'preflight'),patch.object(m,'unchanged'),patch.object(m,'drafts',return_value=[]), \
             patch.object(m,'compose',side_effect=self.compose),patch.object(m,'inspect',return_value=self.new), \
             patch.object(m,'request',side_effect=[(200,b'ok'),(404,json.dumps({'code':probe_code}).encode()),(200,b'{"total":1}')]):
            m.work(self.plan,'h','i')

    def test_only_api_and_one_env_value_persisted_after_proof(self):
        self.work()
        self.assertEqual(self.commands[0],('up','-d','--no-deps','--no-build','--pull','never','--wait','--wait-timeout','100','api'))
        pins=json.loads((self.root/'override.json').read_text())
        self.assertEqual(pins,{'services':{'api':{'image':'same-image','environment':{m.FLAG:'true'}},'web':{'image':'unchanged'}}})
        done=json.loads((self.root/'complete.json').read_text());self.assertEqual(done['status'],'LIVE_READY');self.assertFalse(done['realImportPerformed'])

    def test_non_opt_in_env_image_or_mount_changes_rejected(self):
        for kind in ('environment','image','mount'):
            v=copy.deepcopy(self.new)
            if kind=='environment':v['Config']['Env'][0]='UNCHANGED=changed'
            elif kind=='image':v['Image']='other-image'
            else:v['Mounts']=[{'Type':'volume','Source':'changed','Destination':'/data','RW':True}]
            with self.subTest(kind=kind),self.assertRaises(AssertionError):m.enabled_matches(self.plan,v)

    def test_no_success_if_live_capability_and_opt_in_probe_not_passed(self):
        old=(self.root/'override.json').read_bytes()
        with self.assertRaises(AssertionError):self.work('importDisabled')
        self.assertFalse((self.root/'complete.json').exists());self.assertEqual(old,(self.root/'override.json').read_bytes())

    def test_preflight_failure_performs_no_replacement(self):
        with patch.object(m,'preflight',side_effect=AssertionError),patch.object(m,'compose') as c:
            with self.assertRaises(AssertionError):m.work(self.plan,'h','i')
            c.assert_not_called()

    def test_failed_replacement_preserves_intent_no_pin_or_success(self):
        with patch.object(m,'preflight'),patch.object(m,'compose',side_effect=RuntimeError):
            with self.assertRaises(RuntimeError):m.work(self.plan,'h','i')
        self.assertEqual(json.loads((self.root/'state.json').read_text())['phase'],'api-replacement-possible')
        self.assertFalse((self.root/'complete.json').exists())

    def test_healthy_enabled_candidate_retained_without_fabricating_success(self):
        m.save(self.root/'state.json',{'run':'r','planHash':'h','invocation':'i'})
        with patch.object(m,'unchanged'),patch.object(m,'compose',return_value=b'b'*64),patch.object(m,'inspect',return_value=self.new),patch.object(m,'command') as c:
            m.recover(self.plan,'h','i');c.assert_not_called()
        self.assertEqual(json.loads((self.root/'recovery.json').read_text())['outcome'],'API_ENABLED_RETAINED_MANUAL_REVIEW')
        self.assertFalse((self.root/'complete.json').exists())

    def test_stale_marker_cannot_authorize_recovery(self):
        m.save(self.root/'state.json',{'run':'other','planHash':'h','invocation':'i'})
        with patch.object(m,'unchanged'),patch.object(m,'compose') as c:
            with self.assertRaises(AssertionError):m.recover(self.plan,'h','i')
            c.assert_not_called()

    def test_no_automatic_retry_after_attempt(self):
        self.work()
        with self.assertRaises(AssertionError):self.work()
        self.assertEqual(sum(c[0]=='up' for c in self.commands),1)

if __name__=='__main__':unittest.main()
