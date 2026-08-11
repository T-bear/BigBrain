# BB-076 owner-accepted zero-cost personal-research policy

## Metadata

- Date: 2026-08-11
- Previous state: BB-075 strict gate, no authorized zero-cost source
- Result: policy implemented; activation remains fail-closed at the technical/provider gate
- Runtime data: no real observation acquired
- Budget: 0 SEK

## Status

The governance model and focused tests are implemented. No provider is activated, no real
data exists, and nothing was deployed or manually verified as a real-data runtime.

## Policy decision

The product owner accepts residual licensing uncertainty only for legitimate 0-SEK sources,
private non-commercial read-only personal research, local memory/replay/backtesting where no
identified term prohibits it, and no redistribution. The acceptance version is
`BB-076/2026-08-11`. It does not override explicit negative terms, required permission,
payment requirements, automation prohibitions or technical access controls. It grants no
broker, order or trading authority.

Finance now models `ExplicitProviderGrant`, `OwnerAcceptedPersonalResearch`,
`HumanConfirmationRequired` and `Denied` separately. Decisions remain provider/product- and
capability-specific. Owner acceptance requires exactly 0 SEK and a durable rationale/version;
known denied capabilities still fail closed.

## Evidence

The targeted reassessment below reuses BB-075 evidence under the new owner policy and adds
one bounded Stooq endpoint observation. It does not treat technical reachability as a license.

### Targeted reassessment

| Source/product | BB-075 state | BB-076 evidence class and capability result |
| --- | --- | --- |
| Stooq official daily download | Insufficient explicit lifecycle wording | **OWNER-ACCEPTED PERSONAL RESEARCH** is supportable for bounded private daily historical download/storage/replay: legitimate public download, informational/personal context, no identified ban on that bounded behavior. Activation nevertheless failed safely because the tested official CSV request returned a JavaScript proof-of-work verification page, not CSV. Circumvention is prohibited. Live is unavailable. |
| Alpaca Basic/free IEX | Human confirmation required | **HUMAN CONFIRMATION REQUIRED** for BigBrain's retained live/history path. API and personal/non-commercial access are offered, but the agreement's reproduction restrictions and unresolved retained-copy/IEX lifecycle are material rather than mere silence. Account/key and brokerage-linked eligibility are also absent. |
| EODHD Free Starter | Human confirmation required | **EXPLICIT PROVIDER GRANT, CONDITIONALLY BOUNDED** for personal storage/manipulation/analysis while subscribed, with mandatory deletion of all copies within one month after expiry. It is not activated because a free account/key is absent and BigBrain has no verified termination-deletion lifecycle. |
| Nasdaq Data Link free/open datasets | No qualifying dataset | **INSUFFICIENT EVIDENCE / NO PRODUCT**: no named free US equity/ETF OHLCV dataset suitable for this scope was identified. |
| Finnhub free | Insufficient lifecycle evidence | **HUMAN CONFIRMATION REQUIRED** for retained memory/replay; free personal/API availability does not resolve retained-copy and termination scope. |
| Yahoo Finance / yfinance | Denied/incompatible | **DENIED / INCOMPATIBLE** preserved: the identified automated-collection restriction is explicit, not residual silence. |
| Alpha Vantage free | Denied/incompatible | **DENIED / INCOMPATIBLE** preserved: identified terms classify the target research/testing/investment-analysis behavior outside the ordinary personal grant; current/delayed US equity data is also paid. |
| Financial Modeling Prep Basic | Prior approval required | **HUMAN CONFIRMATION REQUIRED / BLOCKED** preserved: copying/downloading and derivative-work permission is an explicit issue. |
| Twelve Data Personal | Cleared paid fallback | **EXPLICIT PROVIDER GRANT, INACTIVE**: positive human evidence remains, but payment violates the 0-SEK requirement. Basic remains insufficient. |

## Stooq runtime evidence

One bounded read-only request was made on 2026-08-11 to the official `stooq.com/q/d/l/`
surface for AAPL daily rows over 2026-08-01 through 2026-08-11. The response was an HTML
JavaScript verification/proof-of-work challenge (`noindex,nofollow`), not market-data CSV.
No challenge was solved or bypassed, no retry loop was used and no payload was ingested.

This separates two gates: the owner accepts the bounded residual entitlement uncertainty,
but BigBrain still requires an ordinary supported technical mechanism. A client library
such as pandas-datareader would not grant data rights and does not justify bypassing the
current verification control.

## Changes

- Added capability evidence classification and zero-cost/version/rationale invariants to
  the provider-neutral Finance entitlement policy.
- Added tests showing owner-accepted capabilities work only when declared, paid sources
  cannot use owner acceptance, and human-confirmation/denied evidence fails closed.
- No provider adapter, account, credential, production memory, real observation, replay
  dataset, UI real-data state or deployment was created.
- The existing Finance API/UI remain RESEARCH, read-only, no-provider/no-real-data, with no
  synthetic production fallback and no broker/order capability.

## Verification

- Focused entitlement tests: 26/26 passed.
- Full backend regression: 356 API + 32 Sentinel = 388/388 passed.
- Web tests: 106/106 passed.
- .NET Release build and Web production build: passed.
- Documentation validation: passed for 130 Markdown files and 76 unique BB IDs.
- Compose configuration and `git diff --check`: passed.
- Bounded Stooq smoke: technical gate failed with verification HTML; no data ingested.
- Deployment/runtime restart/replay/UI-real-data verification: not run because no source was
  activated and no production code or configuration changed.

## Remaining work

Obtain a normal supported Stooq automation route or written technical clarification; or
obtain the required free-account evidence/credential and implement the full lifecycle for a
separately cleared source. The closest bounded alternative is EODHD Free Starter only after
the owner supplies a free key and termination deletion can be enforced and verified. Alpaca
retained IEX use still needs human clarification. Then implement adapter, immutable local
memory, real API/UI projection and deterministic replay in one separately verified slice.

## Resumption

Resume from ADR 0022, the provider-selection document and this report. The smallest safe
next step is a supported Stooq route or a source-specific credential/lifecycle decision.

## Security

Detta är en sanerad GitHub-version. It contains no secret, account identity, private correspondence, challenge token,
raw market payload, internal address or sensitive header. Finance remains RESEARCH. No
broker, order endpoint, PAPER/LIVE execution, BUY/SELL or AUTO capability exists.
