#!/usr/bin/env python3
"""Filter owner-local journalctl JSON; never print paths, PIDs or raw messages."""
import json
import re
import sys

count = 0
for line in sys.stdin:
    row = json.loads(line)
    message = row.get('MESSAGE', '')
    if not isinstance(message, str):
        continue
    if 'apparmor="DENIED"' not in message or 'comm="bwrap"' not in message:
        continue
    fields = {}
    for key in ('apparmor', 'operation', 'profile', 'error', 'requested_mask', 'denied_mask'):
        match = re.search(r'\b' + key + r'=(?:"([^"]*)"|([^\s]+))', message)
        if match:
            value = match.group(1) or match.group(2)
            # Only short identifiers; never arbitrary text/path output.
            if re.fullmatch(r'[A-Za-z0-9_.:() -]{1,80}', value):
                fields[key] = value
    print(json.dumps(fields, sort_keys=True))
    count += 1
print(json.dumps({'matchingDenials': count, 'zeroIsNotProofOfNoDenial': True}))
