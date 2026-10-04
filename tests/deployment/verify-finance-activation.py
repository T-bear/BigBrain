#!/usr/bin/env python3
"""Model/provider-free Compose/build-gate checks. Never use the operator's .env or Docker daemon."""
import json
import os
from pathlib import Path
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
base_env = {k: os.environ[k] for k in ("PATH", "HOME") if k in os.environ}

def configuration(extra):
    result = subprocess.run(
        ["docker", "compose", "--env-file", "/dev/null", "-f", str(root / "compose.yaml"), "config", "--format", "json"],
        env=base_env | extra, capture_output=True, text=True, check=False)
    assert result.returncode == 0, "Compose validation failed (resolved content intentionally withheld)"
    return json.loads(result.stdout)

config = configuration({})["services"]["api"]
assert config["environment"]["Finance__ObservationRuntime__Enabled"] == "false"
assert config["environment"]["Finance__AlpacaDailyObservation__Enabled"] == "false"
assert config["environment"]["Finance__ObservationRuntime__InstrumentsJson"] == ""
assert config["build"]["args"]["BIGBRAIN_REVISION"] == "UNKNOWN"
entries = [dict(InstrumentId="US:XNAS:" + symbol, DisplayName=name, ProviderSymbol=symbol,
                Mic="XNAS", VenueCode="NASDAQ", VenueName="Nasdaq", ValidFrom="2026-10-02",
                MappingEvidence="fixture:reviewed-mapping") for symbol, name in [("AAPL", "Apple Inc."), ("MSFT", "Microsoft Corp.")]]
config = configuration({
    "FINANCE__OBSERVATIONRUNTIME__INSTRUMENTSJSON": json.dumps(entries),
    "FINANCE__ALPACADAILYOBSERVATION__ENABLED": "true",
    "FINANCE__ALPACADAILYOBSERVATION__APIKEY": "fixturekey",
    "FINANCE__ALPACADAILYOBSERVATION__APISECRET": "fixturesecret",
})["services"]["api"]
assert config["environment"]["Finance__ObservationRuntime__Enabled"] == "false"
assert config["environment"]["Finance__AlpacaDailyObservation__ApiKey"] == "fixturekey"
assert config["environment"]["Finance__AlpacaDailyObservation__ApiSecret"] == "fixturesecret"
assert json.loads(config["environment"]["Finance__ObservationRuntime__InstrumentsJson"]) == entries
assert not any("fixturekey" in str(v) or "fixturesecret" in str(v) for v in config["build"].values())

# Exercise the actual build script against a disposable Git repo and Docker double.
# No image build, provider call, application launch, deployment or real .env inspection.
with tempfile.TemporaryDirectory(prefix="bb132f1-build-gate-") as temp:
    repo = Path(temp)
    (repo / "scripts").mkdir()
    (repo / "scripts/build-bigbrain-api.sh").write_bytes((root / "scripts/build-bigbrain-api.sh").read_bytes())
    (repo / "compose.yaml").write_bytes((root / "compose.yaml").read_bytes())
    (repo / ".gitignore").write_text(".env\nignored.cs\n")
    def git(*args):
        return subprocess.check_output(["git", "-C", str(repo), *args], env=base_env, stderr=subprocess.DEVNULL, text=True).strip()
    git("init"); git("add", ".")
    git("-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid", "commit", "-m", "fixture")
    sha = git("rev-parse", "HEAD"); git("update-ref", "refs/remotes/origin/main", sha)
    (repo / ".env").write_text("PRIVATE_SENTINEL=must-not-enter-build\n")
    (repo / "ignored.cs").write_text("must not enter build\n")
    (repo / "untracked.cs").write_text("must not enter build\n")
    fakebin = repo / "fakebin"; fakebin.mkdir()
    docker = fakebin / "docker"
    docker.write_text('''#!/usr/bin/env python3
import os,sys
from pathlib import Path
a=sys.argv[1:]
assert a[0]=='compose'
p=Path(a[a.index('--project-directory')+1])
assert not (p/'.env').exists() and not (p/'ignored.cs').exists() and not (p/'untracked.cs').exists()
assert a[a.index('--env-file')+1]=='/dev/null'
assert a[-4:]==['build','--build-arg','BIGBRAIN_REVISION='+os.environ['EXPECTED_REVISION'],'api']
Path(os.environ['CALL_MARKER']).write_text('build-only')
''')
    docker.chmod(0o700)
    marker = repo / "called"
    env = base_env | {"PATH": str(fakebin) + ":" + base_env["PATH"], "CALL_MARKER": str(marker), "EXPECTED_REVISION": sha}
    def build(expected):
        return subprocess.run(["bash", str(repo / "scripts/build-bigbrain-api.sh"), expected], env=env, capture_output=True).returncode
    assert build(sha) == 0 and marker.read_text() == "build-only"
    marker.unlink()
    assert build("0" * 40) != 0 and not marker.exists()
    (repo / "compose.yaml").write_text("dirty\n")
    assert build(sha) != 0 and not marker.exists()
print("PASS: Compose defaults/pass-through; credentials do not enable runtime; immutable build archive/gate (Docker double).")
