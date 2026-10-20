import hashlib
import importlib.util
from pathlib import Path
import sqlite3
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('dbcheck', Path(__file__).with_name('check_databases.py'))
m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m)


class ActiveDatabaseChecks(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.addCleanup(self.tmp.cleanup)
        self.path = Path(self.tmp.name)/'fixture.db'
        self.writer = sqlite3.connect(self.path)
        self.addCleanup(self.writer.close)
        self.writer.executescript('CREATE TABLE parent(id INTEGER PRIMARY KEY); CREATE TABLE child(id INTEGER REFERENCES parent(id)); INSERT INTO parent VALUES(1); INSERT INTO child VALUES(1);')
        s=self.path.stat(); self.identity=[s.st_dev,s.st_ino]
    def check(self, **kw):
        return m.check_database(self.path,self.identity,require_readonly=False,**kw)
    def test_current_delete_database_unchanged(self):
        before=self.path.read_bytes();self.assertEqual(self.check()['integrity'],'PASS')
        self.assertEqual(before,self.path.read_bytes())
    def test_current_committed_wal_is_included(self):
        self.writer.execute('PRAGMA journal_mode=WAL')
        self.writer.execute('INSERT INTO child VALUES(999)');self.writer.commit()
        with self.assertRaisesRegex(AssertionError,'ForeignKeyFailed'):self.check()
    def test_uncommitted_wal_writer_not_interrupted(self):
        self.writer.execute('PRAGMA journal_mode=WAL')
        self.writer.execute('INSERT INTO parent VALUES(2)')
        self.assertEqual(self.check()['foreignKeys'],'PASS')
        self.writer.commit()
        self.assertEqual(self.writer.execute('SELECT count(*) FROM parent').fetchone()[0],2)
    def test_delete_writer_can_commit_between_backup_steps(self):
        self.writer.execute('CREATE TABLE payload(value)')
        self.writer.executemany('INSERT INTO payload VALUES(?)',[('x'*1000,)]*500)
        self.writer.commit()
        original=sqlite3.connect; committed=[]
        class Source:
            def __init__(inner,c):inner.c=c
            def set_progress_handler(inner,*a):inner.c.set_progress_handler(*a)
            def close(inner):inner.c.close()
            def backup(inner,target,**kw):
                progress=kw['progress']
                def observe(status,remaining,total):
                    if remaining and not committed:
                        self.writer.execute('INSERT INTO parent VALUES(2)')
                        self.writer.commit();committed.append(True)
                    progress(status,remaining,total)
                kw['progress']=observe
                return inner.c.backup(target,**kw)
        def connect(*args,**kwargs):
            c=original(*args,**kwargs)
            return Source(c) if kwargs.get('uri') else c
        with patch.object(m.sqlite3,'connect',side_effect=connect):self.check()
        self.assertEqual(committed,[True])
    def test_busy_source_fails_without_retry(self):
        self.writer.execute('BEGIN EXCLUSIVE')
        with self.assertRaisesRegex(RuntimeError,'DatabaseBusyNoRetry'):self.check()
        self.assertTrue(self.writer.in_transaction)
        self.writer.rollback()
    def test_wrong_identity_rejected(self):
        self.identity[1]+=1
        with self.assertRaisesRegex(AssertionError,'DatabaseIdentityMismatch'):self.check()
    def test_production_requires_readonly_mount(self):
        with self.assertRaisesRegex(AssertionError,'ReadOnlyMountRequired'):
            m.check_database(self.path,self.identity)
    def test_missing_source_not_created(self):
        with self.assertRaises(FileNotFoundError):m.check_database(self.path.with_name('absent'),self.identity,False)
        self.assertFalse(self.path.with_name('absent').exists())
    def test_deadline(self):
        with self.assertRaises(TimeoutError):self.check(seconds=-1)
    def test_source_uri_cannot_write(self):
        original=sqlite3.connect; observed=[]
        def connect(*args,**kwargs):
            c=original(*args,**kwargs)
            if kwargs.get('uri'):
                observed.append(args[0])
                with self.assertRaises(sqlite3.OperationalError):c.execute('CREATE TABLE forbidden(id)')
            return c
        with patch.object(m.sqlite3,'connect',side_effect=connect):self.check()
        self.assertTrue(observed[0].endswith('?mode=ro'))

if __name__=='__main__':unittest.main()
