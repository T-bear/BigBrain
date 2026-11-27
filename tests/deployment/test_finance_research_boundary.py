"""Negative dependency mutations use fresh synthetic project trees only."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location(
    "research_boundary", ROOT / "scripts/verify-finance-research-boundary.py")
boundary = importlib.util.module_from_spec(spec)
spec.loader.exec_module(boundary)


class FinanceResearchBoundaryTests(unittest.TestCase):
    def setUp(self):
        self.fixture = tempfile.TemporaryDirectory(prefix="bb-research-boundary-", dir="/tmp")
        self.addCleanup(self.fixture.cleanup)
        self.root = Path(self.fixture.name)
        for name, contract in boundary.PROJECTS.items():
            directory = self.root / "tools" / ("BigBrain.Finance." + name)
            directory.mkdir(parents=True)
            project = directory / (directory.name + ".csproj")
            project.write_text((ROOT / project.relative_to(self.root)).read_text())
            for filename in contract["Compile"]:
                source = self.root / "src/BigBrain.Modules/Finance" / filename
                source.parent.mkdir(parents=True, exist_ok=True)
                source.write_text("// Synthetic dependency fixture\n")
        self.project = self.root / "tools/BigBrain.Finance.OutcomeAudit/BigBrain.Finance.OutcomeAudit.csproj"

    def test_repository_boundary(self):
        self.assertGreater(boundary.verify(ROOT), 5)

    def test_synthetic_allowed_graph(self):
        self.assertEqual(boundary.verify(self.root), 5)

    def test_package_project_link_and_build_hook_mutations_are_rejected(self):
        original = self.project.read_text()
        for mutation in (
            '<ItemGroup><PackageReference Include="Synthetic.Broker" Version="1" /></ItemGroup>',
            '<ItemGroup><ProjectReference Include="../../src/BigBrain.Api/BigBrain.Api.csproj" /></ItemGroup>',
            '<ItemGroup><Compile Include="synthetic-order.cs" Link="Order.cs" /></ItemGroup>',
            '<Import Project="synthetic.targets" />',
        ):
            with self.subTest(mutation=mutation):
                self.project.write_text(original.replace("</Project>", mutation + "</Project>"))
                with self.assertRaises(ValueError):
                    boundary.verify(self.root)

    def test_network_order_and_dynamic_loading_mutations_are_rejected(self):
        source = self.project.parent / "SyntheticMutation.cs"
        for mutation in ("using System.Net.Http;", "new OrderClient();",
                         "System.Reflection.Assembly.Load(bytes);", "[DllImport(\"fake\")]",
                         "Process.Start(info);"):
            with self.subTest(mutation=mutation):
                source.write_text(mutation)
                with self.assertRaises(ValueError):
                    boundary.verify(self.root)

    def test_transitive_client_dependency_is_rejected_after_restore(self):
        for name in boundary.PROJECTS:
            assets = self.root / "tools" / ("BigBrain.Finance." + name) / "obj/project.assets.json"
            assets.parent.mkdir()
            libraries = boundary.PACKAGES | ({"BigBrain.Finance.OutcomeAudit/1.0.0"}
                                             if name == "ProvenanceAudit" else set())
            assets.write_text(json.dumps({"libraries": dict.fromkeys(libraries, {})}))
        boundary.verify_resolved(self.root)
        document = json.loads(assets.read_text())
        document["libraries"]["Synthetic.OrderClient/1.0.0"] = {}
        assets.write_text(json.dumps(document))
        with self.assertRaises(ValueError):
            boundary.verify_resolved(self.root)


if __name__ == "__main__":
    unittest.main()
