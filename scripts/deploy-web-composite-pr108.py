#!/usr/bin/env python3
"""Narrow PR108 Web-only release for the existing protected STANDARD runtime.

Read-only by default. No API/Finance/Media mutation. For use only after a
separately reviewed and accepted release tool, and an accepted PR108 merge.
Fail closed on any unknown state; never silently select ordinary compose.yaml.
"""
import argparse
import copy
import fcntl
import json
import os
from pathlib import Path
import re
import stat
import subprocess
import sys
import time
import uuid
from urllib.request import urlopen

ROOT=Path(__file__).resolve().parents[1]
SOURCE="60f8818a7aa46e39c29e729bdf537fe9b1f27765"
UPDATED_HEAD="f3b9f5958b69dc41af44ee726e78612ad8f7900d"
RELEASE_TOOL_MERGE="d96813b1850adbf667529c87f5940c37f52b99d8"

BASE="873013d2c2f2c0c326acdec7eebd35c87c642ce5"
REV=SOURCE  # replaced by exact accepted merge SHA in pr_and_source()
WEB_OLD="sha256:f9e4c19fa059edf400a489607b5481a2093b4ba89f5ea276d11dd03ba1d75f96"
API_OLD="sha256:e68b2077b860bfb22b1875d127c3b05d692c6acaf87405c4d418f40abb7ed20c"
WEB="bigbrain-sprint1-web-1"
API="bigbrain-sprint1-api-1"
NODE_BASE="node@sha256:fb4cd12c85ee03686f6af5362a0b0d56d50c58a04632e6c0fb8363f609372293"
NGINX_BASE="nginxinc/nginx-unprivileged@sha256:0c79d56aee561a1d81c63f00eee5fb5fe29279560cdc55e91425133104c7fbe6"
STATE=Path.home()/".local/state/bigbrain-web-composite"
LOCK=Path.home()/".local/state/bigbrain-web-deploy/lock"
CHANGED={
 "src/BigBrain.Web/src/App.test.tsx",
 "src/BigBrain.Web/src/meal-planner/MealPlanner.test.tsx",
 "src/BigBrain.Web/src/meal-planner/MealPlanner.tsx",
 "src/BigBrain.Web/src/meal-planner/SchoolLunch.tsx",
}
RELEASE_FILES={
 "scripts/deploy-web-composite-pr108.py",
 "tests/deployment/test_web_composite_pr108.py",
 "docs/operations/deployment/README.md",
}
class Blocked(Exception):pass
class Uncertain(Blocked):pass
def need(ok,reason):
 if not ok:raise Blocked(reason)
def run(args,timeout=45,cwd=ROOT):
 try:r=subprocess.run(args,cwd=cwd,capture_output=True,timeout=timeout,check=False)
 except (OSError,subprocess.TimeoutExpired):raise Uncertain("CommandUnavailableOrTimedOut") from None
 need(r.returncode==0,"CommandFailed:"+str(args[0]))
 return r.stdout
def git(*args):return run(["git",*args]).decode().strip()
def docker(*args,timeout=45):return run(["docker","--host","unix:///var/run/docker.sock",*args],timeout=timeout)
def inspect(name):return json.loads(docker("inspect",name))[0]
def pinned_image(sha):
 need(re.fullmatch(r"sha256:[0-9a-f]{64}",sha),"InvalidImageSHA")
 img=json.loads(docker("image","inspect",sha))[0]
 need(img["Id"]==sha,"ImageChanged")
 return img
def healthy(c):
 need(c["State"].get("Running") and c["State"].get("Health",{}).get("Status")=="healthy"
      and not c["State"].get("OOMKilled"),"UnhealthyRuntime")
def http():
 for route in ("/","/icons/bigbrain-32.png"):
  with urlopen("http://127.0.0.1:13000"+route,timeout=4) as r:
   first=r.read(512)
   need(r.status==200 and (b"<html" in first.lower() if route=="/" else first.startswith(b"\x89PNG\r\n\x1a\n")),"BadWebHttp")
def ci_green(sha):
 checks=json.loads(run(["gh","api","repos/T-bear/BigBrain/commits/"+sha+"/check-runs?per_page=100"]))
 def passed(name):
  rows=[x for x in checks.get("check_runs",[])
        if x.get("name")==name and x.get("app",{}).get("slug")=="github-actions"]
  return bool(rows) and all(x.get("status")=="completed" and x.get("conclusion")=="success"
                            for x in rows)
 need(checks.get("total_count",101)<=100 and all(
      passed(n) for n in ("backend","frontend","documentation","secrets")),"MainCINotGreen")
def pr_and_source():
 """Enforce PR108 source and exactly one reviewed release-tool PR in current main."""
 global REV
 current=git("ls-remote","origin","refs/heads/main").split()[0]
 need(git("rev-parse","HEAD")==current,"ToolCheckoutNotAcceptedMain")
 need(not git("status","--porcelain","--untracked-files=no"),"TrackedChanges")
 need(git("rev-parse",SOURCE+"^1")==BASE,"SourceParentChanged")
 need(set(git("diff","--name-only",BASE,SOURCE).splitlines())==CHANGED,
      "UnreviewedPR108Diff")
 pr=json.loads(run(["gh","api","repos/T-bear/BigBrain/pulls/108"]))
 need(pr.get("merged") is True and pr.get("head",{}).get("sha")==UPDATED_HEAD
      and pr.get("base",{}).get("ref")=="main", "PR108NotAccepted")
 # GitHub required a normal merge of release-tool main into the PR branch.
 # Authenticate both exact parents; never accept a changed PR head by name alone.
 need(git("rev-parse",UPDATED_HEAD+"^1")==SOURCE
      and git("rev-parse",UPDATED_HEAD+"^2")==RELEASE_TOOL_MERGE,
      "PR108UpdateParentsChanged")
 need(git("merge-base",UPDATED_HEAD,current)==UPDATED_HEAD,
      "PR108UpdatedHeadNotAccepted")
 merged=pr.get("merge_commit_sha")
 need(bool(re.fullmatch(r"[a-f0-9]{40}",merged or "")),"PR108MergeUnknown")
 # The accepted release must include PR108's exact merge, regardless of
 # which of the two independent PRs was merged first. File allowlists below
 # reject any unrelated changes, even when current main is newer.
 need(git("merge-base",merged,current)==merged,"PR108MergeNotAncestor")
 need(git("merge-base",SOURCE,current)==SOURCE,"PR108HeadNotAncestor")
 need(set(git("diff","--name-only",BASE,current).splitlines()) ==
      CHANGED|RELEASE_FILES,"UnexpectedAcceptedMainDiff")
 # The merge should contain the exact fixed PR108 candidate source tree.
 need(not git("diff","--name-only",SOURCE,current,"--",
              "src/BigBrain.Web","infrastructure",".dockerignore",
              "Directory.Build.props","global.json"),"BuildInputDrift")
 need(git("hash-object","scripts/deploy-web-composite-pr108.py")
      ==git("rev-parse","HEAD:scripts/deploy-web-composite-pr108.py"),
      "ToolFileModified")
 ci_green(SOURCE);ci_green(current)
 REV=current

def compose_from_web(spec,image,revision):
 need(set(spec)=={"command","entrypoint","healthcheck","image","labels","networks",
                 "ports","pull_policy","restart","stop_grace_period"},"UnexpectedWebContract")
 need(spec["pull_policy"]=="never" and spec["image"]==WEB_OLD
      and spec["networks"]=={"default":None}
      and spec["ports"]==[{"mode":"ingress","protocol":"tcp","published":"13000","target":8080}],
      "WebContractDrift")
 new=copy.deepcopy(spec);new["image"]=image
 new["labels"]["org.opencontainers.image.revision"]=revision
 return {"name":"bigbrain-sprint1","services":{"web":new},
         "networks":{"default":{"external":True,"name":"bigbrain-sprint1_default"}}}
def dc(file,work,*args,timeout=70):
 return docker("compose","--project-directory",work,"--env-file","/dev/null","-f",str(file),*args,timeout=timeout)
def recreate_web(file,work):
 try:
  dc(file,work,"up","-d","--no-deps","--no-build","--pull","never",
     "--force-recreate","web",timeout=150)
 except Blocked:
  raise Uncertain("ComposeMutationUnknown; manual review required") from None
def web_shape(c):
 need(c["Config"].get("User")=="101" and not c["HostConfig"].get("Privileged")
      and not c["Mounts"] and set(c["NetworkSettings"]["Networks"])=={"bigbrain-sprint1_default"},
      "WebPrivilegeMountOrNetworkDrift")
 ports=c["HostConfig"].get("PortBindings",{}).get("8080/tcp",[])
 need(len(ports)==1 and ports[0].get("HostPort")=="13000","WebHostPortDrift")
def trusted_rollback_manifest(source):
 # A failed release may restore Web from its private previous.json. Require a
 # completed rollback receipt before accepting that manifest as a new baseline.
 need(source.name=="previous.json" and source.parent.parent==STATE
      and re.fullmatch(r"run-[a-f0-9]{32}",source.parent.name),"UntrustedRollbackPath")
 st=STATE.lstat()
 need(st.st_uid==os.getuid() and stat.S_ISDIR(st.st_mode)
      and st.st_mode&0o777==0o700 and not stat.S_ISLNK(st.st_mode),"UntrustedRollbackState")
 journal_path=source.parent/"journal.jsonl"
 receipt_path=source.parent/"candidate.json"
 for path in (journal_path,receipt_path):
  st=path.lstat()
  need(st.st_uid==os.getuid() and stat.S_ISREG(st.st_mode)
       and st.st_mode&0o077==0 and not stat.S_ISLNK(st.st_mode),"RollbackReceiptPermissions")
 rows=journal_path.read_text().splitlines()
 need(bool(rows) and json.loads(rows[-1]).get("phase")=="ROLLED_BACK","RollbackNotComplete")
 receipt=json.loads(receipt_path.read_text())
 need(receipt.get("source")==REV and receipt.get("previousImage")==WEB_OLD
      and re.fullmatch(r"sha256:[0-9a-f]{64}",receipt.get("candidateImage","")),
      "RollbackCandidateMismatch")

def runtime():
 api,web=inspect(API),inspect(WEB)
 healthy(api);healthy(web)
 need(api["Image"]==API_OLD and web["Image"]==WEB_OLD,"RunningImageChanged")
 web_shape(web)
 wl=web["Config"]["Labels"];al=api["Config"]["Labels"]
 for key in ("com.docker.compose.project.config_files",
             "com.docker.compose.project.working_dir"):
  need(wl.get(key) and wl.get(key)==al.get(key),"ReleaseOriginsDiffer")
 source=Path(wl["com.docker.compose.project.config_files"])
 root=Path.home()/".local/state/bigbrain-standard"
 need(source.name=="release.json" and source.parent.parent==root
      and re.fullmatch(r"run-[a-f0-9]{32}",source.parent.name)
      and source.is_absolute(),"UntrustedReleaseSource")
 for p,mode,is_dir in [(root,0o700,True),(source.parent,0o700,True),
                       (source,0o600,False)]:
  st=p.lstat()
  need(st.st_uid==os.getuid() and (st.st_mode&0o777)==mode
       and not stat.S_ISLNK(st.st_mode)
       and (stat.S_ISDIR(st.st_mode) if is_dir else stat.S_ISREG(st.st_mode)),
       "UnsafeReleaseProvenance")
 journal_path=source.parent/"journal.jsonl"
 js=journal_path.lstat()
 need(stat.S_ISREG(js.st_mode) and not stat.S_ISLNK(js.st_mode)
      and js.st_uid==os.getuid() and not (js.st_mode & 0o077),
      "ReleaseReceiptPermissions")
 lines=journal_path.read_text().splitlines()
 need(lines and json.loads(lines[-1]).get("phase")=="PASS",
      "IncompletePriorRelease")
 data=json.loads(source.read_text())
 need(data.get("name")=="bigbrain-sprint1"
      and set(data.get("services",{}))=={"api","web"}
      and data.get("networks",{}).get("default")==
      {"external":True,"name":"bigbrain-sprint1_default"},
      "UnexpectedReleaseTopology")
 need(data["services"]["api"].get("image")==API_OLD,"ApiSnapshotMismatch")
 spec=data["services"]["web"]
 compose_from_web(spec,WEB_OLD,BASE)
 need(spec["labels"]["org.opencontainers.image.revision"]==BASE,
      "WebRevisionDrift")
 work=wl["com.docker.compose.project.working_dir"]
 need(Path(work).is_absolute() and Path(work).is_dir(),"UntrustedWorkdir")
 for role,container in (("api",api),("web",web)):
  hashed=dc(source,work,"config","--hash",role).decode().strip().split()
  need(hashed==[role,container["Config"]["Labels"].get("com.docker.compose.config-hash")],
       "PriorComposeHashMismatch")
 pinned_image(WEB_OLD);pinned_image(API_OLD)
 http()
 return api,web,spec,work
def others():
 ids=docker("ps","-aq","--no-trunc").decode().splitlines()
 return {cid:(c["Image"],c["State"]["StartedAt"],c["State"]["Running"])
         for cid in ids if (c:=inspect(cid))["Name"]!="/"+WEB}
def write(path,value):
 fd=os.open(path,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW,0o600)
 with os.fdopen(fd,"w") as f:
  json.dump(value,f,sort_keys=True);f.flush();os.fsync(f.fileno())
def journal(folder,phase):
 with (folder/"journal.jsonl").open("a") as f:
  f.write(json.dumps({"phase":phase})+"\n");f.flush();os.fsync(f.fileno())
def wait_web(sha):
 deadline=time.monotonic()+90
 while time.monotonic()<deadline:
  try:
   c=inspect(WEB)
   if c["Image"]==sha and c["State"].get("Health",{}).get("Status")=="healthy":
    http();return
  except (Blocked,OSError,TimeoutError):pass
  time.sleep(3)
 raise Blocked("WebHealthTimeout")
def combined_meal_present(container=WEB):
 # Pure content check, no user data and no external network.
 out=docker("exec",container,"sh","-c",
   "grep -lF 'Kontrollera lunchalternativ' /usr/share/nginx/html/assets/index-*.js >/dev/null && "
   "grep -lF 'Musikugglan:' /usr/share/nginx/html/assets/index-*.js >/dev/null && "
   "echo PRESENT || echo ABSENT")
 return out.strip()==b"PRESENT"
def preflight_web_image(sha):
 # This catches restrictive git-worktree umask permissions before the first
 # production mutation. No ports, API DNS, networks, secrets or media mounts.
 docker("run","--rm","--network","none","--read-only","--cap-drop","ALL",
        "--security-opt","no-new-privileges","--memory","64m",
        "--memory-swap","64m","--pids-limit","24","--user","101:101",
        "--entrypoint","/bin/sh",sha,"-c",
        "set -eu; "
        "test -r /etc/nginx/conf.d/default.conf; "
        "test -s /usr/share/nginx/html/index.html; "
        "ls /usr/share/nginx/html/assets/index-*.js >/dev/null 2>&1; "
        "find /usr/share/nginx/html -type f | "
        "while IFS= read -r path; do test -r \"$path\" || exit 1; done; "
        "grep -lF 'Kontrollera lunchalternativ' /usr/share/nginx/html/assets/index-*.js >/dev/null; "
        "grep -lF 'Musikugglan:' /usr/share/nginx/html/assets/index-*.js >/dev/null",
        timeout=45)

def smoke_web_image(sha):
 # Mandatory isolated HTTP integration gate. Production Web/API stay untouched.
 # No Docker network, host port, secrets, mounts or privileged user.
 cmd=(
  "nginx >/dev/null 2>&1; "
  "for route in / /index.html /manifest.webmanifest /icons/bigbrain-32.png "
  "/media/control/import /media/control/flow; do "
  "wget -T 4 -qO /dev/null http://127.0.0.1:8080$route; done; "
  "js=$(ls /usr/share/nginx/html/assets/index-*.js | head -1); "
  "wget -T 4 -qO /dev/null http://127.0.0.1:8080/assets/$(basename \"$js\"); "
  "grep -qF 'Kontrollera lunchalternativ' \"$js\"; "
  "grep -qF 'Musikugglan:' \"$js\""
 )
 docker("run","--rm","--pull","never","--network","none",
        "--add-host","api:127.0.0.1","--user","101:101",
        "--cap-drop","ALL","--security-opt","no-new-privileges",
        "--memory","128m","--memory-swap","128m","--pids-limit","32",
        "--entrypoint","/bin/sh",sha,"-ec",cmd,timeout=65)

def deployment(snapshot):
 api,web,spec,work=snapshot
 need(os.getuid()==1000,"OperatorUser")
 STATE.mkdir(mode=0o700,parents=True,exist_ok=True)
 st=STATE.lstat()
 need(st.st_uid==os.getuid() and st.st_mode&0o777==0o700 and stat.S_ISDIR(st.st_mode),
      "StatePermission")
 for folder in STATE.glob("run-*"):
  j=folder/"journal.jsonl"
  need(j.is_file() and json.loads(j.read_text().splitlines()[-1])["phase"]
       in ("PASS","ROLLED_BACK","PRE_SWITCH_FAILED"),"PreviousDeploymentNeedsReview")
 baseline=others()
 folder=STATE/("run-"+uuid.uuid4().hex)
 folder.mkdir(mode=0o700)
 journal(folder,"STARTED")
 try:
  oldmanifest=folder/"previous.json"
  write(oldmanifest,compose_from_web(spec,WEB_OLD,BASE))
  # A detached worktree at the exact accepted tree excludes untracked user files.
  src=folder/"source"
  run(["git","worktree","add","--detach",str(src),REV],timeout=70)
  df=src/"src/BigBrain.Web/Dockerfile"
  original=df.read_text()
  need(original.count("FROM node:20-alpine AS build")==1 and
       original.count("FROM nginxinc/nginx-unprivileged:1.29-alpine AS runtime")==1 and
       original.count("COPY src/BigBrain.Web/nginx.conf /etc/nginx/conf.d/default.conf")==1 and
       original.count("RUN npm run build")==1,
       "UnexpectedDockerfile")
  # Vite copies public icons and manifest with worktree's restrictive 0600 modes.
  # Change only generated dist, not source files, credentials or global umask.
  df.write_text(original.replace("FROM node:20-alpine AS build","FROM "+NODE_BASE+" AS build")
                    .replace("RUN npm run build",
                             "RUN npm run build\nRUN chmod -R a+rX /app/dist")
                    .replace("FROM nginxinc/nginx-unprivileged:1.29-alpine AS runtime",
                             "FROM "+NGINX_BASE+" AS runtime")
                    .replace("COPY src/BigBrain.Web/nginx.conf /etc/nginx/conf.d/default.conf",
                             "COPY --chmod=0644 src/BigBrain.Web/nginx.conf /etc/nginx/conf.d/default.conf"))
  pinned_image(NODE_BASE.split("@",1)[1])
  pinned_image(NGINX_BASE.split("@",1)[1])
  tag="bigbrain-web:pr108-"+REV
  docker("build","--pull=false","-f",str(df),"-t",tag,
         "--label","org.opencontainers.image.revision="+REV,str(src),timeout=1200)
  imageid=json.loads(docker("image","inspect",tag))[0]["Id"]
  pinned_image(imageid)
  preflight_web_image(imageid)
  smoke_web_image(imageid)
  write(folder/"candidate.json",{"source":REV,"candidateImage":imageid,
                                 "previousImage":WEB_OLD})
  manifest=folder/"release.json"
  write(manifest,compose_from_web(spec,imageid,REV))
  resolved=json.loads(dc(manifest,work,"config","--format","json"))
  w=resolved.get("services",{}).get("web",{})
  need(set(resolved.get("services",{}))=={"web"} and w.get("image")==imageid
       and not w.get("volumes") and w.get("ports")==spec["ports"],
       "UnsafeResolvedWebManifest")
  need(inspect(WEB)["Id"]==web["Id"] and inspect(API)["Id"]==api["Id"]
       and others()==baseline,"RuntimeChangedBeforeSwitch")
  pr_and_source()
  journal(folder,"READY")
 except Uncertain:
  journal(folder,"MANUAL_REVIEW")
  raise
 except BaseException:
  journal(folder,"PRE_SWITCH_FAILED")
  raise
 finally:
  # Never reuse a modified source worktree; preserve the deployment manifests.
  if (folder/"source").exists():
   try:run(["git","worktree","remove","--force",str(folder/"source")],timeout=40)
   except Blocked:pass
 journal(folder,"SWITCH_STARTED")
 try:
  recreate_web(manifest,work)
  wait_web(imageid)
  web_shape(inspect(WEB))
  need(combined_meal_present(),"CombinedMealMissing")
  healthy(inspect(API))
  need(others()==baseline and inspect(API)["Id"]==api["Id"],"OtherServiceChanged")
  journal(folder,"PASS")
  print("PASS: PR108 Web only; API, Finance and Media unchanged. iPhone/PWA approval PENDING.")
 except (Uncertain,KeyboardInterrupt):
  journal(folder,"MANUAL_REVIEW")
  print("UNKNOWN DOCKER MUTATION: no blind rollback; manual review required",file=sys.stderr)
  raise
 except BaseException:
  try:
   need(others()==baseline and inspect(API)["Id"]==api["Id"],"OthersChangedDuringRollback")
   pinned_image(WEB_OLD)
   recreate_web(oldmanifest,work)
   wait_web(WEB_OLD)
   need(others()==baseline,"RollbackOtherServiceChanged")
   journal(folder,"ROLLED_BACK")
   print("ROLLBACK PASS: previous Web restored.",file=sys.stderr)
  except BaseException:
   journal(folder,"MANUAL_REVIEW")
   print("ROLLBACK NOT PROVEN: manual review mandatory",file=sys.stderr)
  raise
def main():
 os.umask(0o077)
 parser=argparse.ArgumentParser(description=__doc__)
 parser.add_argument("--execute",action="store_true")
 parser.add_argument("--runtime-only",action="store_true",
                     help="Read-only snapshot verification before PR108 merge")
 args=parser.parse_args()
 need(not (args.execute and args.runtime_only),"ModesConflict")
 if not args.runtime_only:
  pr_and_source()
 if args.execute:
  need(LOCK.is_file() and LOCK.lstat().st_uid==os.getuid()
       and LOCK.lstat().st_mode&0o077==0 and not LOCK.is_symlink(),"SharedLockUntrusted")
  fd=os.open(LOCK,os.O_WRONLY|os.O_NOFOLLOW)
  with os.fdopen(fd,"a") as f:
   fcntl.flock(f,fcntl.LOCK_EX|fcntl.LOCK_NB)
   deployment(runtime())
 else:
  runtime()
  print("PREFLIGHT PASS: protected STANDARD Web baseline.",
        "PR108 acceptance not checked in runtime-only mode."
        if args.runtime_only else "Exact PR108 accepted and CI green.")
  print("No build, deployment or data mutation.")
if __name__=="__main__":
 try:main()
 except Blocked as ex:print("BLOCKED: "+str(ex),file=sys.stderr);sys.exit(2)
 except BaseException:print("BLOCKED: unexpected failure; inspect private journal",file=sys.stderr);sys.exit(3)
