"""Offline negative/contract tests for the isolated PR72 Web release tool."""
import copy
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
 def test_known_pr_diff_restricts_source(self):
  self.assertEqual(len(m.CHANGED),11)
  self.assertFalse(any(x.startswith("src/BigBrain.Api/") for x in m.CHANGED))
  self.assertEqual(m.REV,"3112868d73c7ef986b4fba5c9a09eb0deb9cd0f9")
  self.assertEqual(m.BASE,"e8f3e62cc446cf52a89045d1a3ce7d4c3a2116ff")

if __name__=="__main__":unittest.main()
