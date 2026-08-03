# ADR 0010: Documentation ownership, hierarchy, baseline lifecycle and retention

- Status: Proposed
- Date: 2026-08-03

## Context

BigBrain har normativa repositorydokument och en stor extern evidenskedja. Scope, auktoritet, current-status och retention behöver vara explicita så att historiska rapporter inte blir odaterad runtime-sanning.

## Proposed decision

### Dokumenttyper och ägarskap

Produktägaren prioriterar roadmap och backlog. Systemarkitekten granskar arkitektur och ADR:er. Lead Developer håller kodnära modulkontrakt, runbooks och verifiering synkroniserade. Kunskapsbasen sammanfattar evidens men skapar inte beslut.

### Repository och extern rapportyta

Reviewbar, bestående och normativ dokumentation lagras i repositoryt. Runtime-baselines, incidenter, diagnostik och stora inventeringar lagras under `/home/enigma/BigBrain/reports/` utanför Git. Hemligheter publiceras inte.

### Dokumentauktoritet

Inom samma scope: Accepted ADR; aktuell normativ arkitektur; kod och commitbunden kodbaseline; modulkontrakt; godkänd runbook; daterad baseline; incidentens slutrapport; diagnostik/historisk evidens; Proposed design/rekommendation. Datum ensamt avgör inte konflikt mellan olika scope.

### Baseline- och incidentlivscykel

Varje baseline anger scope, tid, källa, status och checksumma. Varje scope har högst en current-post; projekt-, kod-, runtime-, Docker- och media-stackbaselines hålls separata. Incidenter får indexerad evidenskedja, slutrapport och separat post-incidentdiagnos. Mellanrapporter arkiveras additivt med manifest och checksummor efter review.

### Publicering, retention och säkerhet

- Rapporter skrivs temporärt, secret-scannas, publiceras atomiskt och får SHA-256.
- Tills retention accepterats bevaras original och arkivering sker utan radering.
- Current-poster granskas minst kvartalsvis och efter relevant incident eller större release.
- Massflyttar görs i separata, reviewbara steg med manifest och länkkontroll.
- ADR-status ändras aldrig implicit genom dokumentomorganisation.

## Consequences

Konflikter kan bedömas per scope och evidens bevaras, men index och review kräver löpande ägarskap.

## Alternatives considered

En global current-state-fil och omedelbar massflytt avvisas eftersom de blandar scope respektive riskerar evidenskedjor och länkar.

## Review and adoption

Förslaget granskas mot ADR 0001–0009. Ingen befintlig ADR-status ändras.
