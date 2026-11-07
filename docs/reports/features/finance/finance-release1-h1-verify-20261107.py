#!/usr/bin/env python3
import datetime as dt
from decimal import Decimal
import hashlib
import json
import os
from pathlib import Path
import re
import sqlite3
import stat
import sys

# Review artifact only. Approval of this exact file is required before Finance use.
# Source contract: BB-084 baseline d9eadbb3f3533e70e57cd12ad6890b06e36e4772.
SNAPSHOT_NAME = 'bb-091-pre-migration-20260817.db'
SNAPSHOT_SHA256 = '84ef36437bdcd8b762b0a3243c6af705def029e036749ecce701264c28405986'
DEDICATED_UID = 1000  # Proposed, owner-approved H1 exception required.
EVIDENCE_ROOT = Path("/var/lib/bigbrain-finance-h1-20261107")
CUSTODY_NAME = "h1-custody.json"

RUNS = tuple('backtest-' + x for x in (
    'f9da712aacbe9d80', '01c5c0c7c63971db', '8a8b9dbcbe468652',
    'ddbc1348b3779bc5', '7590336784fae4e0', '98fc189d4e9a4bc1'))
FEATURE = 'feature-3833eb92bb641e51'
MARKET = 'wiki-5713d7dccfa38f56'
UNIVERSE = {'US:XNAS:AAPL', 'US:XNAS:MSFT', 'US:XNYS:JPM',
            'US:XNYS:XOM', 'US:XNYS:JNJ'}
BACKUPS = ('finance-backup-3b093584173d74f6',
           'finance-backup-8b549915b4e4129c')
TABLES = ('backtest_runs', 'backtest_events', 'backtest_fills',
          'backtest_equity', 'feature_revisions', 'feature_values',
          'revisions', 'observations')


def need(condition):
    if not condition:
        raise ValueError('invalid')


def identifier(value):
    need(isinstance(value, str) and
         re.fullmatch(r'[A-Za-z0-9_:.-]{1,160}', value) is not None)
    return value


def checksum(value):
    need(isinstance(value, str) and
         re.fullmatch(r'sha256:[0-9a-f]{64}', value) is not None)
    return value


def day(value):
    need(isinstance(value, str) and
         re.fullmatch(r'\d{4}-\d{2}-\d{2}', value) is not None)
    dt.date.fromisoformat(value)
    return value


def timestamp(value):
    need(isinstance(value, str))
    m = re.fullmatch(r'\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}'
                     r'(?:\.(\d{1,7}))?(?:Z|[+-]\d{2}:\d{2})', value)
    need(m is not None)
    parsed = dt.datetime.fromisoformat(value.replace('Z', '+00:00'))
    return parsed, (m.group(1) or '').ljust(7, '0')[6]


def pairs(items):
    result = {}
    for key, value in items:
        need(key not in result)
        result[key] = value
    return result


def invalid_constant(_):
    raise ValueError('nonfinite')


def parse(text):
    return json.loads(text, object_pairs_hook=pairs,
                      parse_float=Decimal, parse_constant=invalid_constant)


def equal(a, b):
    if isinstance(a, bool) or isinstance(b, bool):
        return type(a) is type(b) and a == b
    if isinstance(a, (int, Decimal)) and isinstance(b, (int, Decimal)):
        return a == b
    if type(a) is not type(b):
        return False
    if isinstance(a, dict):
        return a.keys() == b.keys() and all(equal(a[k], b[k]) for k in a)
    if isinstance(a, list):
        return len(a) == len(b) and all(equal(x, y) for x, y in zip(a, b))
    return a == b


def path_for(value, directory=False):
    p = Path(os.path.abspath(value))
    need(not any(x.is_symlink() for x in (p, *p.parents)))
    mode = p.stat().st_mode
    need(stat.S_ISDIR(mode) if directory else stat.S_ISREG(mode))
    return p


def signature(p):
    s = p.stat()
    return s.st_dev, s.st_ino, s.st_size, s.st_mtime_ns, s.st_ctime_ns


def no_sidecars(p):
    need(not any(os.path.lexists(str(p) + suffix)
                 for suffix in ('-wal', '-journal', '-shm')))


class StopVerification(Exception):
    pass


def stream_hash(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def expected_configuration(rid):
    index = RUNS.index(rid)
    strategy, parameters = (
        ('buy-and-hold', {}),
        ('sma-crossover', {'fastPeriod': 10, 'slowPeriod': 20}),
        ('momentum', {'period': 20}))[index % 3]
    # Constants reproduce the historical .NET canonical JSON, not a new run.
    cfg = dict(
        marketRevisionIds=[MARKET], featureRevisionId=FEATURE,
        strategy=dict(id=strategy, version='v1'), strategyParameters=parameters,
        simulationModel='daily-next-session-open-v1',
        costModel=dict(id='zero-cost' if index < 3 else 'conservative-cost',
                       version='v1', commissionPerShare=0 if index < 3 else 0.01,
                       minimumCommission=0 if index < 3 else 1,
                       slippageBasisPoints=0 if index < 3 else 5),
        initialCapital=100000, universe=sorted(UNIVERSE),
        **{'from': '2014-01-02', 'to': '2016-12-19'},
        sizingPolicy='equal-initial-capital-whole-shares-v1', seed=0,
        evaluationContext=None)
    raw = json.dumps(cfg, separators=(',', ':'), ensure_ascii=True)
    digest = hashlib.sha256(raw.encode('utf-8')).hexdigest()
    need(rid == 'backtest-' + digest[:16])
    return parse(raw), digest


class Report:
    def __init__(self):
        self.counts = {'PASS': 0, 'FAIL': 0, 'NOT CHECKED': 0}

    def emit(self, status, label, details=''):
        self.counts[status] += 1
        print(status + ' ' + label + (' ' + details if details else ''))
        if status == 'FAIL':
            raise StopVerification()

    def check(self, condition, label, details=''):
        self.emit('PASS' if condition else 'FAIL', label, details)

    def finish(self):
        c = self.counts
        print(f"SAMMANFATTNING: {c['PASS']}/{c['FAIL']}/{c['NOT CHECKED']}")
        return int(c['FAIL'] > 0)


def children(c, r, rid, result, table, array, column, field):
    try:
        items = result[array]
        need(isinstance(items, list) and all(isinstance(x, dict) for x in items))
        keys = [x[field] for x in items]
        need(len(set(keys)) == len(keys))
        if field == 'sequence':
            need(all(type(k) is int for k in keys))
            ordered = keys == list(range(1, len(keys) + 1))
        elif field == 'session':
            for key in keys:
                day(key)
            ordered = keys == sorted(keys)
        else:
            for key in keys:
                identifier(key)
            chronology = [(day(x['fillSession']), identifier(x['instrumentId']))
                          for x in items]
            ordered = chronology == sorted(chronology)
        expected = sorted(zip(keys, items), key=lambda x: x[0])
        payload = {'events': 'event_json', 'fills': 'fill_json',
                   'equityCurve': 'point_json'}[array]
        rows = c.execute(f'SELECT {column},{payload} FROM {table} '
                         f'WHERE run_id=? ORDER BY {column}', (rid,))
        count = 0
        matches = ordered
        for key, text in rows:
            obj = parse(text)
            matches = matches and count < len(expected)
            if count < len(expected):
                matches = matches and equal(key, expected[count][0])
                matches = matches and equal(obj, expected[count][1])
            count += 1
        matches = matches and count == len(expected)
        r.check(matches, rid + ':' + array + ':identity_order_content',
                f'json={len(items)} db={count}')
        return count
    except (ValueError, TypeError, KeyError, sqlite3.Error, RecursionError):
        r.emit('FAIL', rid + ':' + array + ':unverifiable')
        return None


def verify_run(c, r, rid):
    try:
        rows = c.execute('SELECT * FROM backtest_runs WHERE run_id=?', (rid,)).fetchall()
        need(len(rows) == 1)
        row = rows[0]
        raw = row['result_json']
        obj = parse(raw)
        need(isinstance(obj, dict))
        cfg = obj['configuration']
        expected, config_hash = expected_configuration(rid)
        r.check(equal(cfg, expected) and obj['status'] == 'RESEARCH',
                rid + ':full_historical_configuration_and_run_identity',
                'config_sha256=' + config_hash)
        universe = [identifier(x) for x in cfg['universe']]
        markets = [identifier(x) for x in cfg['marketRevisionIds']]
        feature = identifier(cfg['featureRevisionId'])
        strategy = identifier(cfg['strategy']['id'])
        version = identifier(cfg['strategy']['version'])
        cost = identifier(cfg['costModel']['id']) + '-' + identifier(cfg['costModel']['version'])
        start, end = day(cfg['from']), day(cfg['to'])
        stored = checksum(row['checksum'])
        header = (obj['runId'] == rid and obj['checksum'] == stored and
                  row['feature_revision_id'] == feature and
                  equal(parse(row['market_revisions_json']), markets) and
                  row['strategy_id'] == strategy and row['strategy_version'] == version and
                  row['cost_model'] == cost and row['from_date'] == start and row['to_date'] == end and
                  feature == FEATURE and markets == [MARKET] and
                  len(universe) == 5 and set(universe) == UNIVERSE and
                  start == '2014-01-02' and end == '2016-12-19')
        r.check(header, rid + ':json_header_lineage',
                f'checksum={stored} feature={feature} markets={",".join(markets)} '
                f'strategy={strategy}:{version} cost={cost} universe={len(universe)} '
                f'ids={",".join(universe)} from={start} to={end}')
    except (ValueError, TypeError, KeyError, IndexError, sqlite3.Error, RecursionError):
        r.emit('FAIL', rid + ':missing_or_invalid_row_json_metadata')
        return None

    # Preserve original JSON bytes; only empty the root checksum string token.
    match = re.match(r'\{"runId":"backtest-[0-9a-f]{16}","checksum":'
                     r'("sha256:[0-9a-f]{64}")', raw)
    if match:
        a, b = match.span(1)
        digest = 'sha256:' + hashlib.sha256((raw[:a] + '""' + raw[b:]).encode('utf-8')).hexdigest()
        r.check(digest == stored, rid + ':result_checksum', 'computed=' + digest)
    else:
        r.emit('NOT CHECKED', rid + ':checksum_unsupported_serialization')

    counts = [children(c, r, rid, obj, *spec) for spec in (
        ('backtest_events', 'events', 'sequence', 'sequence'),
        ('backtest_fills', 'fills', 'fill_id', 'fillId'),
        ('backtest_equity', 'equityCurve', 'session_date', 'session'))]
    try:
        event_ids = {x['instrumentId'] for x in obj['events']} - {'PORTFOLIO'}
        fill_ids = {x['instrumentId'] for x in obj['fills']}
        lineage = all(x['featureRevisionId'] == FEATURE and
                      x['marketRevisionId'] == (None if x['instrumentId'] == 'PORTFOLIO' else MARKET)
                      for x in obj['events'])
        r.check(event_ids == UNIVERSE and fill_ids <= UNIVERSE and lineage,
                rid + ':instrument_presence_AAPL',
                'event_ids=' + ','.join(sorted(identifier(x) for x in event_ids)))
        sessions = {day(x['session']) for x in obj['equityCurve']}
        return counts, sessions, set(universe)
    except (ValueError, KeyError, TypeError):
        r.emit('FAIL', rid + ':instrument_or_session_metadata')
        return None


def novelty(c, r, table, key, reference):
    try:
        rows = c.execute(f'SELECT {key},created_utc FROM {table}').fetchall()
        values = [(identifier(x[0]), x[1], timestamp(x[1])) for x in rows]
        baseline = [x[2] for x in values if x[0] in reference]
        others = [x for x in values if x[0] not in reference]
        latest = max(values, key=lambda x: x[2])[1] if values else 'NONE'
        newer = sum(x[2] > max(baseline) for x in values) if baseline else 'UNKNOWN'
        r.emit('PASS', table + ':snapshot_inventory',
               f'count={len(values)} latest={latest} non_BB084={len(others)} newer={newer}')
        r.emit('PASS', table + ':non_BB084_ids',
               ','.join(x[0] + '@' + x[1] for x in sorted(others)))
    except (ValueError, TypeError, sqlite3.Error):
        r.emit('FAIL', table + ':inventory_unverifiable')


def verify_db(c, r):
    found = {x[0] for x in c.execute("SELECT name FROM sqlite_master WHERE type='table' AND rootpage>0")}
    for table in TABLES:
        if table not in found:
            r.emit('FAIL', table + ':missing_table')
        else:
            n = c.execute(f'SELECT count(*) FROM {table}').fetchone()[0]
            r.emit('PASS', table + ':schema', f'rows={n}')
    try:
        f = c.execute('SELECT source_revisions_json FROM feature_revisions '
                      'WHERE revision_id=?', (FEATURE,)).fetchall()
        m = c.execute('SELECT checksum FROM revisions WHERE revision_id=?', (MARKET,)).fetchall()
        source = c.execute('SELECT DISTINCT instrument_id,session_date FROM observations '
                           'WHERE revision_id=?', (MARKET,)).fetchall()
        days = {day(x[1]) for x in source}
        ids = {identifier(x[0]) for x in source}
        r.check(len(f) == 1 and parse(f[0][0]) == [MARKET] and len(m) == 1 and
                len(days) == 748 and len(source) == 3722 and ids == UNIVERSE, 'source_feature_binding',
                f'feature={FEATURE} market={MARKET} sessions={len(days)} instruments={len(ids)}')
    except (ValueError, TypeError, sqlite3.Error, IndexError):
        days = None
        r.emit('FAIL', 'source_feature_binding_unverifiable')
    # Only identifiers, lineage dates and states are read here, never feature values/prices.
    feature_rows = c.execute(
        'SELECT instrument_id,session_date,source_revision_id,source_from,source_to,state,quality '
        'FROM feature_values WHERE revision_id=?', (FEATURE,)).fetchall()
    source_pairs = {(x[0], x[1]) for x in source}
    feature_pairs = {(x[0], x[1]) for x in feature_rows}
    available = sum(x[5] == 'Available' for x in feature_rows)
    warmup = sum(x[5] == 'Warmup' for x in feature_rows)
    issues = sum(x[6] != 'Good' for x in feature_rows)
    meta = c.execute('SELECT value_count,available_count,warmup_count,quality_issue_count,checksum '
                     'FROM feature_revisions WHERE revision_id=?', (FEATURE,)).fetchone()
    totals = (len(feature_rows), available, warmup, issues)
    r.check(totals == tuple(meta[:4]) == (78162, 76567, 1595, 0) and
            feature_pairs == source_pairs and
            all(x[2] == MARKET and day(x[3]) <= day(x[4]) <= day(x[1]) and
                (x[0], x[3]) in source_pairs and (x[0], x[4]) in source_pairs
                for x in feature_rows),
            'feature_rows_lineage_counts',
            f'values={totals[0]} available={available} warmup={warmup} issues={issues} '
            'stored_checksum=' + checksum(meta[4]))
    for instrument in sorted(UNIVERSE):
        n = sum(x[0] == instrument for x in source)
        k = sum(x[0] == instrument for x in feature_rows)
        r.check(n > 0 and k > 0, instrument + ':source_feature_presence',
                f'source_rows={n} feature_rows={k}')
    r.emit('NOT CHECKED', 'market_feature_numeric_checksum_recomputation_no_engine_replay')
    results = [verify_run(c, r, rid) for rid in RUNS]
    if all(x is not None and None not in x[0] for x in results) and days is not None:
        events = sum(x[0][0] for x in results)
        fills = sum(x[0][1] for x in results)
        valid = (events == 27992 and fills == 1172 and len(days) == 748 and
                 all(x[0][2] == 748 and x[1] == days and x[2] == UNIVERSE for x in results))
        r.check(valid, 'BB084_totals', f'events={events} fills={fills} sessions={len(days)} instruments=5')
    else:
        r.emit('NOT CHECKED', 'BB084_totals_incomplete_evidence')
    r.emit('NOT CHECKED', 'fills_original_insertion_order_no_persisted_ordinal')
    novelty(c, r, 'feature_revisions', 'revision_id', {FEATURE})
    novelty(c, r, 'backtest_runs', 'run_id', set(RUNS))


def verify_manifest(root, r, bid):
    try:
        p = path_for(root / (bid + '.manifest.json'))
        before = signature(p)
        need(before[2] <= 1024 * 1024)
        with p.open('rb') as stream:
            manifest = parse(stream.read(1024 * 1024 + 1).decode('utf-8'))
        need(isinstance(manifest, dict) and signature(p) == before)
        keys = ','.join(sorted(identifier(k) for k in manifest))
        need(manifest['backupId'] == bid and manifest['status'] == 'Complete')
        artifacts = manifest['artifacts']
        need(isinstance(artifacts, list) and len(artifacts) == 1)
        a = artifacts[0]
        name = bid + '.data.json'
        need(a['path'] == name and type(a['bytes']) is int and a['bytes'] >= 0)
        declared = checksum(a['sha256'])
        fingerprint = checksum(manifest['contentFingerprint'])
        r.emit('PASS', bid + ':manifest_structure', 'keys=' + keys)
        fields = ('revisions', 'featureRevisionIds', 'backtestRunIds', 'robustnessEvaluationIds')
        need(all(isinstance(manifest.get(k), list) for k in fields))
        counts = {k: len(manifest[k]) for k in fields}
        observations = [x['observationCount'] for x in manifest['revisions']]
        need(all(type(n) is int and n >= 0 for n in observations))
        r.emit('PASS', bid + ':declared_counts',
               ' '.join(f'{k}={v}' for k, v in counts.items()) +
               f' observations={sum(observations)} bytes={a["bytes"]} checksum={declared}')
        r.emit('NOT CHECKED', bid + ':table_row_counts_not_in_known_manifest_contract')
        data = path_for(root / name)
        old = signature(data)
        h, total = hashlib.sha256(), 0
        with data.open('rb') as stream:
            while True:
                block = stream.read(1024 * 1024)
                if not block:
                    break
                h.update(block)
                total += len(block)
        digest = 'sha256:' + h.hexdigest()
        r.check(signature(data) == old and signature(p) == before and total == old[2] == a['bytes'] and
                digest == declared == fingerprint and bid == 'finance-backup-' + digest[7:23],
                bid + ':stream_hash_size_stability', f'bytes={total} computed={digest}')
    except (OSError, ValueError, TypeError, KeyError, UnicodeError, RecursionError):
        r.emit('FAIL', bid + ':manifest_or_artifact_unverifiable')


def closure(root, r):
    try:
        p = path_for(root / 'bb090-closure-20260816', directory=True)
        entries = []
        for item in sorted(p.iterdir()):
            s = item.lstat()
            # Filenames are JSON-escaped; no child is opened or followed.
            name = json.dumps(item.name, ensure_ascii=True)
            when = dt.datetime.fromtimestamp(s.st_mtime, dt.timezone.utc).isoformat()
            entries.append(f'{name}:bytes={s.st_size}:mtime={when}')
        r.emit('PASS', 'closure_metadata_only', f'count={len(entries)} ' + ' '.join(entries))
    except (OSError, ValueError):
        r.emit('NOT CHECKED', 'closure_directory_unavailable')



def reader_group():
    # Local account files only, no NSS/network lookup and no account changes.
    with Path('/etc/passwd').open('rb') as stream:
        users = [line.decode('utf-8').rstrip('\n').split(':') for line in stream if line.strip()]
    with Path('/etc/group').open('rb') as stream:
        groups = [line.decode('utf-8').rstrip('\n').split(':') for line in stream if line.strip()]
    need(all(len(row) == 7 for row in users) and all(len(row) == 4 for row in groups))
    reader = [row for row in users if row[2] == '1000']
    need(len(reader) == 1 and reader[0][3] == '1000')
    name = reader[0][0]
    group = [row for row in groups if row[2] == '1000']
    need(len(group) == 1 and set(filter(None, group[0][3].split(','))) <= {name})
    need({row[0] for row in users if row[3] == '1000'} == {name})


def no_acl(path):
    for attribute in ('system.posix_acl_access', 'system.posix_acl_default'):
        try:
            os.getxattr(path, attribute)
        except OSError as exc:
            need(exc.errno == 61)  # Linux ENODATA; fail on unsupported metadata.
        else:
            need(False)


def evidence_access(db, root):
    need(root == EVIDENCE_ROOT and db == root / SNAPSHOT_NAME)
    # Root-owned ancestors prevent replacement/unlink, including via named ACLs.
    for parent in root.parents:
        s = parent.lstat()
        need(stat.S_ISDIR(s.st_mode) and s.st_uid == 0 and not s.st_mode & 0o022)
        no_acl(parent)
    reader_group()
    names = {SNAPSHOT_NAME, CUSTODY_NAME}
    names.update(b + suffix for b in BACKUPS for suffix in ('.manifest.json', '.data.json'))
    need(set(os.listdir(root)) == names)
    for p in (root, *(root / n for n in sorted(names))):
        s = p.lstat()
        isdir = p == root
        need((stat.S_ISDIR(s.st_mode) if isdir else stat.S_ISREG(s.st_mode)))
        need(s.st_uid == 0 and s.st_gid == 1000 and (isdir or s.st_nlink == 1))
        need(stat.S_IMODE(s.st_mode) == (0o550 if isdir else 0o440))
        no_acl(p)
        need(not os.access(p, os.W_OK))
    marker = root / CUSTODY_NAME
    need(marker.stat().st_size <= 16384)
    with marker.open('rb') as stream:
        custody = parse(stream.read())
    need(custody['status'] == 'complete' and custody['readerUid'] == DEDICATED_UID)
    expiry = dt.datetime.fromisoformat(custody['readUntil'])
    need(expiry.tzinfo is not None and dt.datetime.now(dt.timezone.utc) < expiry)
    need(expiry <= dt.datetime.fromisoformat('2026-11-09T20:00:00+01:00'))
    need(set(custody['files']) == names - {CUSTODY_NAME})
    # Manifest hashes in the root-owned inventory protect the full data hashes.
    for name, spec in custody['files'].items():
        need(set(spec) == {'bytes', 'sha256'})
        need((root / name).stat().st_size == spec['bytes'])
        if name.endswith('.manifest.json'):
            need(stream_hash(root / name) == spec['sha256'])
    need(custody['files'][SNAPSHOT_NAME]['sha256'] == SNAPSHOT_SHA256)


def run(argv, r):
    try:
        need(len(argv) == 4 and set(argv[::2]) == {'--db', '--manifest-dir'})
        args = dict(zip(argv[::2], argv[1::2]))
        need(os.getuid() == os.geteuid() == DEDICATED_UID and DEDICATED_UID != 0)
        db = path_for(args['--db'])
        need(db.name == SNAPSHOT_NAME and not os.access(db, os.W_OK))
        no_sidecars(db)
        root = path_for(args['--manifest-dir'], directory=True)
        evidence_access(db, root)
        before = signature(db)
    except (OSError, ValueError):
        r.emit('FAIL', 'arguments_or_regular_file_or_sidecar_guard')
        return r.finish()
    digest = stream_hash(db)
    no_sidecars(db)
    r.check(signature(db) == before and digest == SNAPSHOT_SHA256,
            'documented_static_snapshot_hash', 'sha256=' + digest)
    c = None
    try:
        c = sqlite3.connect(db.as_uri() + '?mode=ro&immutable=1', uri=True)
        c.execute('PRAGMA query_only=ON')
        c.row_factory = sqlite3.Row
        allowed = {sqlite3.SQLITE_SELECT, sqlite3.SQLITE_READ}
        def authorize(action, one, two, database, source):
            if action in allowed:
                return sqlite3.SQLITE_OK
            if action == sqlite3.SQLITE_FUNCTION and two == 'count':
                return sqlite3.SQLITE_OK
            return sqlite3.SQLITE_DENY
        c.set_authorizer(authorize)
        r.emit('PASS', 'connection_ro_immutable_query_only')
        r.emit('NOT CHECKED', 'live_state_engine_replay_creator; gäller bara snapshotens datum')
        verify_db(c, r)
    except (sqlite3.Error, ValueError, OSError, TypeError, RecursionError):
        r.emit('FAIL', 'database_unverifiable')
    finally:
        if c is not None:
            c.close()
    try:
        no_sidecars(db)
        r.check(signature(db) == before, 'database_metadata_unchanged')
    except (OSError, ValueError):
        r.emit('FAIL', 'database_changed_or_sidecar_appeared')
    for bid in BACKUPS:
        verify_manifest(root, r, bid)
    r.emit('NOT CHECKED', 'closure_directory_unavailable')
    evidence_access(db, root)
    return r.finish()


def main(argv):
    r = Report()
    try:
        return run(argv, r)
    except StopVerification:
        return r.finish()
    except (Exception, KeyboardInterrupt):
        try:
            r.emit('FAIL', 'unexpected_error_or_interrupted')
        except StopVerification:
            pass
        return r.finish()


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
