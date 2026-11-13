"""Exercise the actual TEST image HTML injector without a build or browser."""
from html.parser import HTMLParser
import json
from pathlib import Path
import shlex
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
SHA = "1234567890abcdef1234567890abcdef12345678"


class Page(HTMLParser):
    def __init__(self):
        super().__init__()
        self.banner = None
        self.styles = {}
        self.current = None
        self.text = ""

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == "aside":
            self.banner = attrs
        if tag == "style":
            self.current = attrs.get("id")
            self.styles[self.current] = ""

    def handle_data(self, data):
        self.text += data
        if self.current is not None:
            self.styles[self.current] += data

    def handle_endtag(self, tag):
        if tag == "style":
            self.current = None


class TestBanner(unittest.TestCase):
    def inject(self, root, revision):
        lines = (ROOT / "infrastructure/test-environment/Dockerfile.web").read_text().splitlines()
        snippets = [shlex.split(line.removeprefix("RUN "))
                    for line in lines if line.startswith("RUN node -e ")]
        self.assertEqual(len(snippets), 1)
        argv = snippets[0]
        self.assertEqual(argv[:2], ["node", "-e"])
        self.assertEqual(argv[-1], "$BIGBRAIN_REVISION")
        return subprocess.run([*argv[:-1], revision], cwd=root, capture_output=True, timeout=10)

    def fixture(self, root):
        dist = root / "dist"
        dist.mkdir()
        # Real source structure plus a built stylesheet to check override order.
        html = (ROOT / "src/BigBrain.Web/index.html").read_text()
        html = html.replace("</head>", '<link rel="stylesheet" href="/assets/app.css"></head>')
        (dist / "index.html").write_text(html)
        (dist / "manifest.webmanifest").write_text(json.dumps({
            "name": "BigBrain", "short_name": "BigBrain",
            "icons": [{"src": "/icons/bigbrain-32.png", "sizes": "32x32"}]}))
        return dist, html

    def test_built_test_page_reserves_banner_space_for_body_portaled_modal(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            dist, _ = self.fixture(root)
            result = self.inject(root, SHA)
            self.assertEqual(result.returncode, 0, result.stderr.decode())
            html = (dist / "index.html").read_text()
            page = Page()
            page.feed(html)
            self.assertEqual(page.banner["id"], "bb-test-banner")
            self.assertIn("TEST · " + SHA[:12] + " · synthetic data", page.text)
            self.assertIn("position:fixed", page.banner["style"])
            self.assertIn("white-space:nowrap", page.banner["style"])
            self.assertIn("height:var(--bb-test-banner-height)", page.banner["style"])
            self.assertIn("clamp(10px,3vw,16px)", page.banner["style"])
            css = page.styles["bb-test-layout"]
            self.assertIn(":root{--bb-test-banner-height:28px}", css)
            self.assertIn("body{padding-top:var(--bb-test-banner-height)}", css)
            # The modal is a body portal: a root-only offset cannot fix this defect.
            self.assertIn(".shopping-mode{top:var(--bb-test-banner-height)}", css)
            self.assertGreater(html.index('id="bb-test-layout"'), html.index("/assets/app.css"))
            self.assertEqual((dist / "test-revision.txt").read_text(), SHA + "\n")
            manifest = json.loads((dist / "manifest.webmanifest").read_text())
            self.assertEqual(manifest["short_name"], "BB TEST")
            self.assertEqual(manifest["name"], "BigBrain TEST " + SHA[:12])
            self.assertEqual(manifest["icons"][0]["src"], "/icons/bigbrain-32.png")

    def test_invalid_revision_rejected_before_modifying_generated_page(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            dist, original = self.fixture(root)
            self.assertNotEqual(self.inject(root, "$(touch should-not-exist)").returncode, 0)
            self.assertEqual((dist / "index.html").read_text(), original)
            self.assertFalse((dist / "test-revision.txt").exists())
            self.assertFalse((root / "should-not-exist").exists())


if __name__ == "__main__":
    unittest.main()
