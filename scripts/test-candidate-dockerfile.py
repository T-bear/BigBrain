"""Derive a TEST-only API build file; never edit the production Dockerfile."""
import hashlib
import json
from pathlib import Path
import sys


def derive(source, sdk):
    if sdk != {"version": "10.0.300", "rollForward": "latestPatch"}:
        raise ValueError("Review TEST SDK pin after global.json changes")
    original = "FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build"
    lines = source.splitlines(keepends=True)
    if not lines or lines[0] != original + "\n" or sum(
            line.upper().startswith("FROM ") and "/dotnet/sdk:" in line for line in lines) != 1:
        raise ValueError("Unexpected production SDK stage; review TEST derivation")
    lines[0] = "FROM mcr.microsoft.com/dotnet/sdk:10.0.300 AS build\n"
    return "".join(lines)


if __name__ == "__main__":
    source = Path("src/BigBrain.Api/Dockerfile").read_text()
    result = derive(source, json.loads(Path("global.json").read_text())["sdk"])
    destination = Path(sys.argv[1])
    with destination.open("x") as output:
        output.write(result)
    print(json.dumps({"sdk_image": "mcr.microsoft.com/dotnet/sdk:10.0.300",
                      "source_dockerfile_sha256": hashlib.sha256(source.encode()).hexdigest(),
                      "derived_dockerfile_sha256": hashlib.sha256(destination.read_bytes()).hexdigest()}))
