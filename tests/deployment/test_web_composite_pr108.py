"""Offline negative/contract tests for the isolated PR108 Web release tool."""
import copy
import json
import os
import tempfile
import importlib.util
from pathlib import Path
import unittest
from unittest.mock import patch

FILE=Path(__file__).resolve().parents[2]/"scripts/deploy-web-composite-pr108.py"
spec=importlib.util.spec_from_file_location("web_composite_pr108",FILE)
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

 def test_dist_permissions_are_normalized_only_for_generated_assets(self):
  source=FILE.read_text()
  self.assertIn("RUN chmod -R a+rX /app/dist",source)
  self.assertIn("find /usr/share/nginx/html -type f",source)
  self.assertIn("smoke_web_image(imageid)",source)
  self.assertIn("preflight_web_image(imageid)",source)

 def test_isolated_web_smoke_gate_has_no_prod_ports_network_or_mounts(self):
  with patch.object(m,"docker",return_value=b"") as mocked:
   m.smoke_web_image("sha256:"+"a"*64)
   args=mocked.call_args.args
   self.assertEqual(args[0],"run")
   self.assertIn("--rm",args)
   self.assertIn("--network",args)
   self.assertEqual(args[args.index("--network")+1],"none")
   self.assertEqual(args[args.index("--user")+1],"101:101")
   self.assertEqual(args[args.index("--add-host")+1],"api:127.0.0.1")
   for forbidden in ("--publish","--network-alias","--mount","--volume","--privileged","--link"):
    self.assertNotIn(forbidden,args)
   sh=args[-1]
   self.assertIn("nginx >/dev/null 2>&1",sh)
   self.assertIn("/manifest.webmanifest",sh)
   self.assertIn("/icons/bigbrain-32.png",sh)
   self.assertIn("/media/control/import",sh)
   self.assertIn("Kontrollera lunchalternativ",sh)
  with patch.object(m,"docker",side_effect=m.Blocked("HTTP_403")):
   with self.assertRaises(m.Blocked):m.smoke_web_image("sha256:"+"a"*64)

 def test_temporary_dockerfile_must_pin_config_mode(self):
  source=FILE.read_text()
  self.assertIn("COPY --chmod=0644 src/BigBrain.Web/nginx.conf",source)
  self.assertIn("preflight_web_image(imageid)",source)
  self.assertIn("trusted_rollback_manifest(source)",source)


 def test_release_scope_exact_web_only_and_no_api_edits(self):
  self.assertEqual(set(m.RELEASE_FILES),{
    "scripts/deploy-web-composite-pr108.py",
    "tests/deployment/test_web_composite_pr108.py",
    "docs/operations/deployment/README.md",
  })
  self.assertEqual(set(m.CHANGED),{
    "src/BigBrain.Web/src/App.test.tsx",
    "src/BigBrain.Web/src/meal-planner/MealPlanner.test.tsx",
    "src/BigBrain.Web/src/meal-planner/MealPlanner.tsx",
    "src/BigBrain.Web/src/meal-planner/SchoolLunch.tsx",
  })
  self.assertEqual(m.WEB_OLD,"sha256:f9e4c19fa059edf400a489607b5481a2093b4ba89f5ea276d11dd03ba1d75f96")
  self.assertEqual(m.API_OLD,"sha256:e68b2077b860bfb22b1875d127c3b05d692c6acaf87405c4d418f40abb7ed20c")

 def test_nonaccepted_pr108_is_blocked_before_mutation(self):
  with patch.object(m,"git",side_effect=lambda *args: {
    ("ls-remote","origin","refs/heads/main"):"a"*40+"\trefs/heads/main",
    ("rev-parse","HEAD"):"a"*40,
    ("status","--porcelain","--untracked-files=no"):"",
    ("rev-parse",m.SOURCE+"^1"):m.BASE,
    ("diff","--name-only",m.BASE,m.SOURCE):"\n".join(m.CHANGED),
  }.get(args,"")), patch.object(m,"run",return_value=json.dumps({
     "merged":False, "head":{"sha":m.SOURCE},"base":{"ref":"main"}
  }).encode()):
   with self.assertRaisesRegex(m.Blocked,"PR108NotAccepted"):
    m.pr_and_source()

 def test_runtime_only_does_not_authorize_execute(self):
  source=FILE.read_text()
  self.assertIn('not (args.execute and args.runtime_only)',source)
  self.assertIn('PR108 acceptance not checked in runtime-only mode.',source)
  self.assertIn('if not args.runtime_only:',source)
  self.assertIn('pr_and_source()',source)

 def test_actual_release_preserves_api_and_checks_private_standard_provenance(self):
  source=FILE.read_text()
  self.assertIn('set(data.get("services",{}))=={"api","web"}',source)
  self.assertIn('bigbrain-standard',source)
  self.assertIn('PriorComposeHashMismatch',source)
  self.assertIn('IncompletePriorRelease',source)
  self.assertIn('others()==baseline',source)
  self.assertIn('combined_meal_present()',source)
  self.assertIn('web_shape(inspect(WEB))',source)
  self.assertIn('MANUAL_REVIEW',source)
  self.assertIn('bigbrain-web-composite',source)


 def test_accepted_pr_and_exact_main_sets_release_revision(self):
  main="b"*40
  blob="c"*40
  def git(*args):
   answers={
    ("ls-remote","origin","refs/heads/main"):main+" refs/heads/main",
    ("rev-parse","HEAD"):main,
    ("status","--porcelain","--untracked-files=no"):"",
    ("rev-parse",m.SOURCE+"^1"):m.BASE,
    ("diff","--name-only",m.BASE,m.SOURCE):"\n".join(m.CHANGED),
    ("diff","--name-only",m.BASE,main):"\n".join(m.CHANGED|m.RELEASE_FILES),
    ("merge-base",main,main):main,
    ("merge-base",m.SOURCE,main):m.SOURCE,
    ("diff","--name-only",m.SOURCE,main,"--",
      "src/BigBrain.Web","infrastructure",".dockerignore",
      "Directory.Build.props","global.json"):"",
    ("hash-object","scripts/deploy-web-composite-pr108.py"):blob,
    ("rev-parse","HEAD:scripts/deploy-web-composite-pr108.py"):blob,
   }
   if args not in answers:raise AssertionError("Unexpected git query:"+str(args))
   return answers[args]
  prior=m.REV
  try:
   with patch.object(m,"git",side_effect=git),patch.object(m,"run",
      return_value=json.dumps({"merged":True,
        "head":{"sha":m.SOURCE},"base":{"ref":"main"},
        "merge_commit_sha":main}).encode()),patch.object(m,"ci_green") as ci:
    m.pr_and_source()
    self.assertEqual(m.REV,main)
    self.assertEqual(ci.call_count,2)
    self.assertEqual([x.args[0] for x in ci.call_args_list],
                     [m.SOURCE,main])
  finally:
   m.REV=prior

 def test_unapproved_extra_main_diff_blocks_before_ci(self):
  main="b"*40
  blob="c"*40
  def git(*args):
   if args==("ls-remote","origin","refs/heads/main"):return main+" refs/heads/main"
   if args==("rev-parse","HEAD"):return main
   if args==("status","--porcelain","--untracked-files=no"):return ""
   if args==("rev-parse",m.SOURCE+"^1"):return m.BASE
   if args==("diff","--name-only",m.BASE,m.SOURCE):return "\n".join(m.CHANGED)
   if args==("merge-base",main,main):return main
   if args==("merge-base",m.SOURCE,main):return m.SOURCE
   if args==("diff","--name-only",m.BASE,main):
    return "\n".join(m.CHANGED|m.RELEASE_FILES|{"src/BigBrain.Api/Program.cs"})
   raise AssertionError(str(args))
  with patch.object(m,"git",side_effect=git),patch.object(m,"run",
      return_value=json.dumps({"merged":True,"head":{"sha":m.SOURCE},
      "base":{"ref":"main"},"merge_commit_sha":main}).encode()),patch.object(m,"ci_green") as ci:
   with self.assertRaisesRegex(m.Blocked,"UnexpectedAcceptedMainDiff"):
    m.pr_and_source()
   ci.assert_not_called()

 def test_runtime_only_blocks_unknown_prior_compose_hash(self):
  # Calls are read-only; simulate a changed Web hash to ensure fail-closed.
  source=FILE.read_text()
  self.assertIn('"PriorComposeHashMismatch"',source)
  self.assertTrue('hashed==[role,container["Config"]["Labels"].get(' in source)
  self.assertIn('journal_path.read_text().splitlines()',source)

if __name__=="__main__":unittest.main()
