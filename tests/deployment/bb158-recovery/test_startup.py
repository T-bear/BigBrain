"""Focused regression tests; no systemd, Docker or privileged execution."""
import ast
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from types import SimpleNamespace

SOURCE = Path(__file__).with_name('isolated-tests.py')
spec = importlib.util.spec_from_file_location('isolated_tests', SOURCE)
driver = importlib.util.module_from_spec(spec)
spec.loader.exec_module(driver)


class StartupTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        self.unit = 'bb158-recovery-test-' + 'a' * 32 + '.service'

    def observe(self, pids, times, parent='PPid:\t1\n', cgroup=None):
        if cgroup is None:
            cgroup = '0::/system.slice/' + self.unit
        with patch.object(driver, 'run', side_effect=[
            SimpleNamespace(stdout=str(pid).encode()) for pid in pids
        ]), patch.object(driver.time, 'monotonic', side_effect=times), \
                patch.object(driver.time, 'sleep'), \
                patch.object(Path, 'read_text', side_effect=[parent, cgroup]):
            driver.verify_detached_process(self.unit, self.folder)

    def test_oneshot_launch_is_explicitly_nonblocking(self):
        tree = ast.parse(SOURCE.read_text())
        command = next(node.value for node in ast.walk(tree)
                       if isinstance(node, ast.Assign)
                       and any(isinstance(t, ast.Name) and t.id == 'command'
                               for t in node.targets))
        literals = [node.value for node in command.elts if isinstance(node, ast.Constant)]
        self.assertIn('--no-block', literals)
        self.assertIn('--property=Type=oneshot', literals)
        self.assertNotIn('--wait', literals)
        self.assertIn('--property=TimeoutStartSec=10s', literals)
        self.assertIn('--property=TimeoutStopSec=30s', literals)

    def test_pending_start_then_manager_process(self):
        self.observe([0, 123], [0, 0.1, 0.2])
        self.assertTrue((self.folder / 'disconnect-observation.json').is_file())

    def test_missing_process_has_finite_deadline(self):
        with self.assertRaises(TimeoutError):
            self.observe([0], [0, 0.1, 5.1])
        self.assertFalse((self.folder / 'disconnect-observation.json').exists())

    def test_completed_work_is_not_disconnect_proof(self):
        (self.folder / 'recovery.json').write_text('{}')
        with self.assertRaisesRegex(AssertionError, 'completed'):
            self.observe([], [0, 0.1])

    def test_wrong_parent_is_rejected(self):
        with self.assertRaises(AssertionError):
            self.observe([123], [0, 0.1], parent='PPid:\t999\n')

    def test_wrong_cgroup_is_rejected(self):
        with self.assertRaises(AssertionError):
            self.observe([123], [0, 0.1], cgroup='0::/other.service')

    def test_disappearing_process_is_not_accepted(self):
        with patch.object(driver, 'run', return_value=SimpleNamespace(stdout=b'123')), \
                patch.object(Path, 'read_text', side_effect=FileNotFoundError):
            with self.assertRaises(FileNotFoundError):
                driver.verify_detached_process(self.unit, self.folder)
        self.assertFalse((self.folder / 'disconnect-observation.json').exists())


if __name__ == '__main__':
    unittest.main()
