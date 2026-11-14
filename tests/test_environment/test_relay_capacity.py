"""Local TCP concurrency regression; no Docker, providers or application execution."""
import importlib.util
import json
from pathlib import Path
import socket
import tempfile
import threading
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("relay_capacity_control", ROOT / "scripts/test-environment.py")
tool = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(tool)


class RelayCapacityTests(unittest.TestCase):
    def test_browser_six_persistent_connections_and_bounded_overflow(self):
        real_socket = socket.socket
        ready, release = threading.Event(), threading.Event()
        events, errors, address, clients = [], [], [], []
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "state.json").write_text(json.dumps({"containers": {"web": "fixture-web"}}))
            slot = object.__new__(tool.Slot)
            slot.root = root
            slot.p = {"bind": "127.0.0.1"}
            slot.seal = {"allowed_clients": ["127.0.0.1"]}
            slot.preflight = lambda: None
            slot.inventory = lambda: {}
            slot.audit = lambda _: {"web": "fixture-web"}
            slot.relay_target = lambda _: None
            slot.internal_health = lambda _: None

            def receipt(event, **fields):
                events.append((event, fields))
                if event == "relay-listening":
                    ready.set()

            def hold(client, web, cancelled, capacity):
                try:
                    client.sendall(b"accepted")
                    release.wait(10)
                finally:
                    client.close()
                    capacity.release()

            slot.receipt = receipt
            slot.relay_connection = hold

            class Listener:
                def __init__(self, *args, **kwargs):
                    self.socket = real_socket(*args, **kwargs)

                def bind(self, requested):
                    # Exercise real relay logic while avoiding a fixed-port test conflict.
                    if requested != ("127.0.0.1", 13001):
                        raise AssertionError("relay endpoint changed")
                    self.socket.bind(("127.0.0.1", 0))
                    address.append(self.socket.getsockname())

                def __getattr__(self, name):
                    return getattr(self.socket, name)

                def __enter__(self):
                    return self

                def __exit__(self, *_):
                    self.socket.close()

            def run():
                try:
                    slot.relay()
                except BaseException as error:
                    errors.append(error)
                    ready.set()

            def connect():
                client = real_socket(socket.AF_INET, socket.SOCK_STREAM)
                clients.append(client)
                client.settimeout(3)
                client.connect(address[0])
                return client

            with patch.object(tool.socket, "socket", Listener):
                worker = threading.Thread(target=run)
                worker.start()
                try:
                    self.assertTrue(ready.wait(3), "relay did not listen")
                    self.assertFalse(errors, errors)
                    for _ in range(6):
                        self.assertEqual(connect().recv(32), b"accepted",
                                         "ordinary six-connection browser burst was reset")
                    limit = next(fields["max_connections"] for event, fields in events
                                 if event == "relay-listening")
                    self.assertGreaterEqual(limit, 6)
                    self.assertLessEqual(limit, 8)
                    for _ in range(limit - 6):
                        self.assertEqual(connect().recv(32), b"accepted")
                    for _ in range(2):
                        self.assertEqual(connect().recv(32), b"")
                    capacity_events = [fields for event, fields in events
                                       if event == "relay-capacity-rejected"]
                    self.assertEqual(capacity_events, [{"max_connections": limit}])
                finally:
                    for client in clients:
                        client.close()
                    release.set()
                    (root / "state.json").write_text(json.dumps({"containers": {"web": "retired"}}))
                    worker.join(4)
                self.assertFalse(worker.is_alive(), "fixture relay leaked")
                self.assertFalse(errors, errors)


if __name__ == "__main__":
    unittest.main()
