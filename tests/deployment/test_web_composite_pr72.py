"""Offline negative/contract tests for the isolated PR72 Web release tool."""
import copy
import json
import os
import tempfile
import importlib.util
from pathlib import Path
import unittest
from unittest.mock import patch

FILE=Path(__file__).resolve().parents[2]/"scripts/deploy-web-composite-pr72.py"
spec=importlib.util.spec_from_file_location("web_composite",FILE)
m=importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)

def web():
 return {
  "command":None,"entrypoint":None,
  "healthcheck":{"test":["CMD","wget","--spider","http://127.0.0.1:8080/"]},
  "image":m.WEB_OLD,"labels":{"org.opencontainers.image.revision":m.BASE},
  "networks":{"default":None},
  "ports":[{"mode":"ingress","protocol":"tcp","published":"13000","target":8080}],
  "pull_policy":"never","restart":"unless-stopped","stop_grace_period":"10s"
 }

class CompositeReleaseTests(unittest.TestCase):
 def test_manifest_contains_only_web_without_api_or_secrets(self):
  d=m.compose_from_web(web(),"sha256:"+"a"*64,m.REV)
  self.assertEqual(set(d["services"]),{"web"})
  self.assertEqual(d["name"],"bigbrain-sprint1")
  self.assertEqual(d["services"]["web"]["image"],"sha256:"+"a"*64)
  self.assertEqual(d["networks"],{"default":{"external":True,"name":"bigbrain-sprint1_default"}})
  self.assertNotIn("api",str(d))
  self.assertNotIn("environment",str(d))
 def test_original_specification_is_not_mutated(self):
  original=web();before=copy.deepcopy(original)
  m.compose_from_web(original,"sha256:"+"a"*64,m.REV)
  self.assertEqual(original,before)
 def test_other_services_cannot_be_injected(self):
  cases=[("build","."),("environment",{"TOKEN":"oops"}),("volumes",["/:/host"]),
         ("privileged",True),("extra_hosts",["bad:127.0.0.1"])]
  for field,value in cases:
   with self.subTest(field=field):
    item=web();item[field]=value
    with self.assertRaises(m.Blocked):m.compose_from_web(item,m.WEB_OLD,m.REV)
 def test_wrong_image_port_and_network_fail_closed(self):
  for field,value in [
   ("image","sha256:"+"b"*64),
   ("pull_policy","always"),
   ("networks",{"media":None}),
   ("ports",[{"mode":"ingress","protocol":"tcp","published":"18080","target":8080}])
  ]:
   with self.subTest(field=field):
    item=web();item[field]=value
    with self.assertRaises(m.Blocked):m.compose_from_web(item,m.WEB_OLD,m.REV)
 def test_failed_compose_switch_is_uncertain_not_retryable(self):
  with patch.object(m,"dc",side_effect=m.Blocked("Docker failed")):
   with self.assertRaises(m.Uncertain):
    m.recreate_web(Path("/private/release.json"),"/old/project")
 def test_successful_compose_switch_targets_web_only(self):
  with patch.object(m,"dc",return_value=b"") as mock:
   m.recreate_web(Path("/private/release.json"),"/old/project")
   args=mock.call_args.args
   self.assertEqual(args[-1],"web")
   self.assertIn("--no-deps",args)
   self.assertIn("--no-build",args)
   self.assertIn("--force-recreate",args)
   self.assertNotIn("--remove-orphans",args)
 def test_web_runtime_shape_rejects_privilege_and_mounts(self):
  base={"Config":{"User":"101"},"HostConfig":{"Privileged":False,
     "PortBindings":{"8080/tcp":[{"HostPort":"13000"}]}},
     "Mounts":[],"NetworkSettings":{"Networks":{"bigbrain-sprint1_default":{}}}}
  m.web_shape(base)
  for field,value in [("Mounts",[{"Destination":"/data"}]),("HostConfig",{"Privileged":True})]:
   with self.subTest(field=field):
    x=copy.deepcopy(base);x[field]=value
    with self.assertRaises(m.Blocked):m.web_shape(x)
 def test_isolated_candidate_probe_never_uses_network_or_mounts(self):
  image="sha256:"+"a"*64
  with patch.object(m,"docker",return_value=b"") as mocked:
   m.preflight_web_image(image)
   a=mocked.call_args.args
   self.assertEqual(a[0:3],("run","--rm","--network"))
   self.assertIn("none",a)
   self.assertIn("--read-only",a)
   self.assertIn("--cap-drop",a)
   self.assertIn("--user",a)
   self.assertIn("101:101",a)
   self.assertNotIn("--volume",a)
   self.assertIn(image,a)
   self.assertIn("test -r /etc/nginx/conf.d/default.conf",a[-1])
  with patch.object(m,"docker",side_effect=m.Blocked("unreadable")):
   with self.assertRaises(m.Blocked):m.preflight_web_image(image)

 def test_previous_release_is_trusted_only_with_completed_rollback_receipt(self):
  with tempfile.TemporaryDirectory() as d:
   state=Path(d);state.chmod(0o700)
   run=state/("run-"+"a"*32);run.mkdir(mode=0o700)
   source=run/"previous.json";source.write_text("{}");source.chmod(0o600)
   journal=run/"journal.jsonl";journal.write_text(json.dumps({"phase":"ROLLED_BACK"})+"\n");journal.chmod(0o600)
   receipt=run/"candidate.json"
   receipt.write_text(json.dumps({"source":m.REV,"previousImage":m.WEB_OLD,
                                  "candidateImage":"sha256:"+"b"*64}))
   receipt.chmod(0o600)
   with patch.object(m,"STATE",state):
    m.trusted_rollback_manifest(source)
    journal.write_text(json.dumps({"phase":"SWITCH_STARTED"})+"\n")
    with self.assertRaises(m.Blocked):m.trusted_rollback_manifest(source)
    journal.write_text(json.dumps({"phase":"ROLLED_BACK"})+"\n")
    receipt.write_text(json.dumps({"source":m.REV,"previousImage":"sha256:"+"c"*64,
                                   "candidateImage":"sha256:"+"b"*64}))
    with self.assertRaises(m.Blocked):m.trusted_rollback_manifest(source)
    receipt.write_text(json.dumps({"source":m.REV,"previousImage":m.WEB_OLD,
                                   "candidateImage":"sha256:"+"b"*64}))
    receipt.chmod(0o644)
    with self.assertRaises(m.Blocked):m.trusted_rollback_manifest(source)

 def test_temporary_dockerfile_must_pin_config_mode(self):
  source=FILE.read_text()
  self.assertIn("COPY --chmod=0644 src/BigBrain.Web/nginx.conf",source)
  self.assertIn("preflight_web_image(imageid)",source)
  self.assertIn("trusted_rollback_manifest(source)",source)

 def test_known_pr_diff_restricts_source(self):
  self.assertEqual(len(m.CHANGED),11)
  self.assertFalse(any(x.startswith("src/BigBrain.Api/") for x in m.CHANGED))
  self.assertEqual(m.REV,"3112868d73c7ef986b4fba5c9a09eb0deb9cd0f9")
  self.assertEqual(m.BASE,"e8f3e62cc446cf52a89045d1a3ce7d4c3a2116ff")

if __name__=="__main__":unittest.main()
