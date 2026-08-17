# Authentication

## Syfte

Skilja implementerade skydd från framtida identitets- och behörighetsmål.

## Verifierade fakta

- Kontrollerade mediarequests använder preview, kortlivad bekräftelse och idempotens.
- Sentinel använder ett autentiserat lokalt protokoll.

## Viktiga tekniska lärdomar

- Rekommendation: identitet, auktorisation och audit ska bindas till deklarerade capabilities och minsta behörighet.
- Ännu ej verifierat/implementerat: generell OIDC/OAuth-baserad användarautentisering och full persistent, identitetsbunden audit.

## Vanliga feltolkningar

- Bekräftelsetoken för en mediarequest är inte generell användarautentisering.
- En nätverksgräns ersätter inte auktorisation.
- Enhets- eller nätverksidentitet är inte användaridentitet. MAC-adress får inte användas som
  betrodd autentiseringsidentitet, och BigBrain ska aldrig ta emot eller lagra biometriska data.
- Passkeys, trusted-device och step-up är en framtida standardsbaserad utredning, inte en
  implementerad autentiseringsförmåga eller Finance-exekveringsauktoritet.

## Relaterade runbooks

- [Operationsindex](../indexes/operations.md)

## Relaterade dokument och ADR:er

- [Sentinels säkerhetsmodell](../security/sentinel-security-model.md)
- [Arkitektur](../../ARCHITECTURE.md)
- [Future product planning](../reports/documentation/product-ux-auth-school-meals-backlog-capture-20260817.md)

## Senast verifierad

2026-08-03.

## Källa och evidens

Arkitektur, säkerhetsdokument, ADR:er, kod och `docs/STATUS.md`.
