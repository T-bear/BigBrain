# Finance BB-045 – market-session and historical replay foundation

## Metadata

- Date: 2026-08-11
- Scope: third provider-neutral BB-045 implementation slice
- Related commit: assigned on publication

## Status

The fixture-only market-session, explicit gap-semantics and deterministic historical replay
foundation is implemented and automatically verified. BB-045 remains in progress and
BB-071 remains `Pågår – väntar på leverantörsbekräftelse`. Nothing was deployed or runtime-
enabled, and no external or real market data was acquired or persisted.

## Changes

`MarketSession` represents a local trading date, session ID, venue/MIC, explicit timezone,
evidence and one of Trading, Closed or Unknown. Trading sessions have deterministic UTC
open/close derived by framework `TimeZoneInfo`; nonexistent or ambiguous DST local times
fail rather than selecting an offset. `SyntheticMarketSessionCalendar` knows only supplied
fixture dates, so an absent date resolves Unknown and is never guessed closed/trading.

Gap semantics distinguish ExpectedClosure, MissingObservation, ProviderGap,
InvalidObservation and UnknownSession. ProviderGap requires explicit provider-gap evidence;
generic missing and unknown calendar knowledge cannot be promoted. Invalid evidence prevents
the corresponding bar from being emitted as a valid price. No fill, interpolation, zero,
fabrication, repair or raw-data mutation exists.

`DeterministicHistoricalReplay` accepts one immutable dataset revision, instruments,
calendar, canonical bars/actions/findings and a supplied UTC range. It rejects mixed
revisions, resolves provider symbols at each historical session date and emits explicit
session, quality/gap, observation-availability, dividend and split events. Ordering uses UTC
effective time, fixed event priority, canonical instrument and ordinal stable tie-breakers.
Corporate actions never rewrite raw bars, and future dates/actions/symbols remain outside an
earlier replay window. This is an M2 data primitive, not M3 strategy/portfolio simulation.

Raw observations remain immutable market truth. Future adjustments, repairs, indicators,
features, labels and model inputs must be new derived artifacts with complete lineage to
immutable revisions; accumulating market knowledge never authorizes silent history mutation.

## Evidence

- `dotnet restore BigBrain.slnx` — PASS
- `dotnet build BigBrain.slnx -c Release --no-restore` — PASS, zero warnings/errors
- `dotnet test BigBrain.slnx -c Release --no-build --no-restore` — PASS
- API/module: 286 passed; Sentinel: 32 passed; total: 318 passed, 0 failed/skipped
- `node scripts/verify-documentation.mjs` — PASS
- `git diff --check` — PASS
- `docker compose config --quiet` — PASS

The 13 new tests are synthetic, deterministic, network-free, storage-free and independent
of wall clock and host-local timezone. They cover normal sequences, explicit closure,
unknown session, missing observation, explicit provider gap, invalid observation, ticker
change, dividends, exact splits, same-time ordering, repeated replay, UTC/DST conversion,
mixed revisions, future-information exclusion and late-quality-evidence no-lookahead.

## Security

Detta är en sanerad GitHub-version. It contains no key, credential, account, real provider
payload, private evidence, internal address, raw log or sensitive path. No provider call,
broker connection, order, PAPER/LIVE/AUTO promotion, migration, paid service, new dependency,
runtime change or deployment was introduced. Finance retains zero real-money authority.

## Remaining work

BB-045 still needs correction/supersession availability across immutable revisions, richer
quality aggregation, measured persistence design/implementation, an authorized provider
adapter, actual ingestion and external dataset acceptance. Production exchange-calendar
coverage is absent by design. BB-071 continues to block provider activation and retaining
real provider data.

## Resumption

The next safe provider-neutral slice is an in-memory immutable dataset revision assembler
that models corrections/supersession and their availability times without lookahead. After
that, measure replay/query/backup/deletion needs before selecting persistence. Do not create
an adapter, account, subscription or durable provider store while BB-071 remains open.
