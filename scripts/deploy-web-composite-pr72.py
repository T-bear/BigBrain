#!/usr/bin/env python3
"""Exact, reviewed PR72 Web-only release for the protected composite BigBrain runtime.

Dry-run by default. No API, DB, Finance, media, or general release functionality.
Requires independent review of this release tool before --execute.
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
REV="3112868d73c7ef986b4fba5c9a09eb0deb9cd0f9"
BASE="e8f3e62cc446cf52a89045d1a3ce7d4c3a2116ff"
WEB_OLD="sha256:335f23a95fcb9e1b31b6f838dd3af943d2627aeec61222d338a0cf61456485ba"
API_OLD="sha256:9310e3cc8ea3484d94bcfeed78b005ba1aecb5724e4ca2cfc28fe9deb44ab047"
WEB="bigbrain-sprint1-web-1"
API="bigbrain-sprint1-api-1"
NODE_BASE="node@sha256:fb4cd12c85ee03686f6af5362a0b0d56d50c58a04632e6c0fb8363f609372293"
NGINX_BASE="nginxinc/nginx-unprivileged@sha256:0c79d56aee561a1d81c63f00eee5fb5fe29279560cdc55e91425133104c7fbe6"
STATE=Path.home()/".local/state/bigbrain-web-composite"
LOCK=Path.home()/".local/state/bigbrain-web-deploy/lock"
CHANGED={
 "docs/BACKLOG.md","docs/STATUS.md","docs/modules/media.md",
 "docs/operations/bb158-freeze-20261023.md","docs/operations/freezes.md",
 "src/BigBrain.Web/src/media-control/MediaControl.tsx",
 "src/BigBrain.Web/src/media-control/MediaControl.test.tsx",
 "src/BigBrain.Web/src/media-control/MediaUrlImport.test.tsx",
 "src/BigBrain.Web/src/media-control/mediaNavigation.ts",
 "src/BigBrain.Web/src/ux-lab/UXLab.tsx",
 "src/BigBrain.Web/src/ux-lab/UXLab.test.tsx",
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
RELEASE_FILES={
 "scripts/deploy-web-composite-pr72.py",
 "tests/deployment/test_web_composite_pr72.py",
 "docs/operations/deployment/README.md",
}
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
 current=git("ls-remote","origin","refs/heads/main").split()[0]
 need(git("rev-parse","refs/remotes/origin/main")==current,"OriginMainStale")
 need(git("rev-parse","HEAD")==current,"ToolCheckoutNotAcceptedMain")
 need(not git("status","--porcelain","--untracked-files=no"),"TrackedChanges")
 need(set(git("diff","--name-only",BASE,REV).splitlines())==CHANGED,"UnreviewedPR72Diff")
 need(git("rev-parse",REV+"^1")==BASE,"UnexpectedPR72Base")
 # Release code must be the exact, separately reviewed tool merged on top of PR72.
 need(set(git("diff","--name-only",REV,current).splitlines())==RELEASE_FILES,"UnreviewedReleaseToolDiff")
 need(git("hash-object","scripts/deploy-web-composite-pr72.py")
      ==git("rev-parse","HEAD:scripts/deploy-web-composite-pr72.py"),"ToolFileNotAccepted")
 pr=json.loads(run(["gh","api","repos/T-bear/BigBrain/pulls/72"]))
 need(pr.get("merged") is True and pr.get("merge_commit_sha")==REV
      and pr.get("head",{}).get("sha")=="47d6b7236dfcd918048d7034629db0b8d50839b7","PRIdentity")
 ci_green(REV);ci_green(current)
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
 need(api["Image"]==API_OLD and web["Image"]==WEB_OLD,"CompositeImageChanged")
 web_shape(web)
 label=web["Config"]["Labels"]
 source=Path(label.get("com.docker.compose.project.config_files",""))
 root=Path.home()/".local/state/bigbrain-standard"
 standard=(source.name=="release.json"
           and re.fullmatch(r"run-[a-f0-9]{32}",source.parent.name)
           and source.parent.parent==root)
 rollback=(source.name=="previous.json"
           and re.fullmatch(r"run-[a-f0-9]{32}",source.parent.name)
           and source.parent.parent==STATE)
 need(standard or rollback,"UntrustedSourceManifest")
 for p in (source.parent,source):
  st=p.lstat()
  need(st.st_uid==os.getuid() and st.st_mode&0o077==0 and not stat.S_ISLNK(st.st_mode)
       and (stat.S_ISDIR(st.st_mode) if p==source.parent else stat.S_ISREG(st.st_mode)),"ManifestPermissions")
 if rollback:trusted_rollback_manifest(source)
 data=json.loads(source.read_text())
 need(data.get("name")=="bigbrain-sprint1"
      and data["networks"]["default"]=={"external":True,"name":"bigbrain-sprint1_default"},
      "UnexpectedComposeTopology")
 spec=data["services"]["web"]
 compose_from_web(spec,WEB_OLD,BASE)
 need(spec["labels"]["org.opencontainers.image.revision"]==BASE,"WebRevisionDrift")
 work=label.get("com.docker.compose.project.working_dir","")
 need(Path(work).is_dir() and Path(work).is_absolute(),"InvalidComposeWorkdir")
 hashtext=dc(source,work,"config","--hash","web").decode().strip().split()
 need(hashtext==["web",label.get("com.docker.compose.config-hash")],"ComposeHashMismatch")
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
def visible_link():
 a=docker("exec",WEB,"sh","-c",
     "if grep -q 'Förbered ljudimport från länk' /usr/share/nginx/html/assets/index-*.js; then echo PRESENT; else echo ABSENT; fi")
 return a.strip()==b"PRESENT"
def preflight_web_image(sha):
 # This catches restrictive git-worktree umask permissions before the first
 # production mutation. No ports, API DNS, networks, secrets or media mounts.
 docker("run","--rm","--network","none","--read-only","--cap-drop","ALL",
        "--security-opt","no-new-privileges","--memory","64m",
        "--memory-swap","64m","--pids-limit","24","--user","101:101",
        "--entrypoint","/bin/sh",sha,"-c",
        "test -r /etc/nginx/conf.d/default.conf && "
        "test -s /usr/share/nginx/html/index.html && "
        "ls /usr/share/nginx/html/assets/index-*.js >/dev/null 2>&1 && "
        "! grep -q 'Förbered ljudimport från länk' /usr/share/nginx/html/assets/index-*.js",
        timeout=45)

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
       original.count("COPY src/BigBrain.Web/nginx.conf /etc/nginx/conf.d/default.conf")==1,
       "UnexpectedDockerfile")
  # COPY otherwise inherits 0600 from the private, umask-077 worktree.
  df.write_text(original.replace("FROM node:20-alpine AS build","FROM "+NODE_BASE+" AS build")
                    .replace("FROM nginxinc/nginx-unprivileged:1.29-alpine AS runtime",
                             "FROM "+NGINX_BASE+" AS runtime")
                    .replace("COPY src/BigBrain.Web/nginx.conf /etc/nginx/conf.d/default.conf",
                             "COPY --chmod=0644 src/BigBrain.Web/nginx.conf /etc/nginx/conf.d/default.conf"))
  pinned_image(NODE_BASE.split("@",1)[1])
  pinned_image(NGINX_BASE.split("@",1)[1])
  tag="bigbrain-web:bb72-"+REV
  docker("build","--pull=false","-f",str(df),"-t",tag,
         "--label","org.opencontainers.image.revision="+REV,str(src),timeout=1200)
  imageid=json.loads(docker("image","inspect",tag))[0]["Id"]
  pinned_image(imageid)
  preflight_web_image(imageid)
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
  need(not visible_link(),"HiddenUIStillVisible")
  healthy(inspect(API))
  need(others()==baseline and inspect(API)["Id"]==api["Id"],"OtherServiceChanged")
  journal(folder,"PASS")
  print("PASS: PR72 Web only; API and data unchanged. iPhone/PWA approval PENDING.")
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
 args=parser.parse_args()
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
  print("PREFLIGHT PASS: exact PR72, green CI and protected Web baseline.")
  print("No build, deployment or data mutation.")
if __name__=="__main__":
 try:main()
 except Blocked as ex:print("BLOCKED: "+str(ex),file=sys.stderr);sys.exit(2)
 except BaseException:print("BLOCKED: unexpected failure; inspect private journal",file=sys.stderr);sys.exit(3)
