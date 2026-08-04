# Report Schema

A sanitized report must contain:

1. Metadata: title, date, scope and related commit when known.
2. Status: implemented, tested, deployed and manually verified as separate facts.
3. Evidence: commands or observations without sensitive runtime identities.
4. Changes: relevant repository paths and contracts.
5. Security: sanitization statement and confirmation that prohibited data is absent.
6. Remaining work: concrete limitations and backlog references.
7. Resumption: authoritative documents and the smallest safe next step.

Reports must never contain secrets, raw identifiers, private addresses, raw logs,
private media names or machine-specific sensitive paths.
