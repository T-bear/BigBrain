#!/usr/bin/env python3
"""Fail-closed dependency contract for the two SYNTHETIC offline audit libraries.

Not a classification of the API, hosting process or all Finance research.
Package/version changes require explicit review of this contract as risk 3.
"""
from pathlib import Path
import json
import re
import sys
import xml.etree.ElementTree as ET

PROJECTS = {
    "OutcomeAudit": {
        "PackageReference": {("Microsoft.Data.Sqlite", "10.0.10"),
                             ("SQLitePCLRaw.lib.e_sqlite3", "2.1.12")},
        "ProjectReference": set(),
        "Compile": {"UsMarketCalendar.cs"},
    },
    "ProvenanceAudit": {
        "PackageReference": set(),
        "ProjectReference": {"../BigBrain.Finance.OutcomeAudit/BigBrain.Finance.OutcomeAudit.csproj"},
        "Compile": {"DailyFeatureEngine.cs", "FinancePrimitives.cs", "MarketData.cs",
                    "MarketDataEntitlements.cs"},
    },
}
FORBIDDEN = re.compile(
    r"\b(?:System\s*\.\s*(?:Net|Reflection)|DllImport|LibraryImport|NativeLibrary|"
    r"ProcessStartInfo|Process\s*\.|Assembly\s*\.|Activator\s*\.|"
    r"AlpacaClient|BrokerClient|OrderClient|TradingClient)\b")
PACKAGES = {
    "Microsoft.Data.Sqlite/10.0.10", "Microsoft.Data.Sqlite.Core/10.0.10",
    "SQLitePCLRaw.bundle_e_sqlite3/2.1.11", "SQLitePCLRaw.core/2.1.11",
    "SQLitePCLRaw.lib.e_sqlite3/2.1.12", "SQLitePCLRaw.provider.e_sqlite3/2.1.11",
}


def verify_resolved(root):
    """Check restored transitive closure, including inherited MSBuild references."""
    for name in PROJECTS:
        assets = root / "tools" / ("BigBrain.Finance." + name) / "obj/project.assets.json"
        actual = set(json.loads(assets.read_text())["libraries"])
        expected = PACKAGES | ({"BigBrain.Finance.OutcomeAudit/1.0.0"}
                               if name == "ProvenanceAudit" else set())
        if actual != expected:
            raise ValueError("unreviewed resolved dependency")


def verify(root):
    sources = set()
    for name, expected in PROJECTS.items():
        directory = root / "tools" / ("BigBrain.Finance." + name)
        project = directory / (directory.name + ".csproj")
        tree = ET.parse(project).getroot()
        allowed = {"Project", "PropertyGroup", "TargetFramework", "ItemGroup",
                   "PackageReference", "ProjectReference", "Compile", "InternalsVisibleTo"}
        if any(e.tag not in allowed for e in tree.iter()):
            raise ValueError("unreviewed project element")
        if tree.attrib != {"Sdk": "Microsoft.NET.Sdk"}:
            raise ValueError("unreviewed SDK")
        for tag in ("PackageReference", "ProjectReference", "Compile"):
            actual = set()
            for item in tree.iter(tag):
                include = item.attrib.get("Include", "")
                if tag == "PackageReference":
                    if set(item.attrib) != {"Include", "Version"}:
                        raise ValueError("unreviewed package attributes")
                    actual.add((include, item.attrib["Version"]))
                elif tag == "ProjectReference":
                    if set(item.attrib) != {"Include"}:
                        raise ValueError("unreviewed project attributes")
                    actual.add(include)
                else:
                    filename = item.attrib.get("Link", "")
                    if item.attrib != {"Include": "../../src/BigBrain.Modules/Finance/" + filename,
                                       "Link": filename}:
                        raise ValueError("unreviewed linked source")
                    actual.add(filename)
                    sources.add(directory / include)
            if actual != expected[tag]:
                raise ValueError("unreviewed " + tag)
        sources.update(p for p in directory.rglob("*.cs")
                       if not {"bin", "obj"}.intersection(p.relative_to(directory).parts))
    for source in sources:
        if source.is_symlink() or FORBIDDEN.search(source.read_text()):
            raise ValueError("forbidden dependency or dynamic execution in research source")
    return len(sources)


if __name__ == "__main__":
    try:
        count = verify(Path(__file__).resolve().parents[1])
        if sys.argv[1:] == ["--resolved"]:
            verify_resolved(Path(__file__).resolve().parents[1])
        elif sys.argv[1:]:
            raise ValueError("unsupported arguments")
        print(f"PASS: offline research dependency boundary ({count} source files)")
    except (ValueError, KeyError, OSError, ET.ParseError):
        print("FAIL: offline research dependency boundary", file=sys.stderr)
        sys.exit(1)
