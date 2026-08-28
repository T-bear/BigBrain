# BB-110 — Audiobook Owner UX Consolidation & Native Playback Foundation

## Metadata

- Date: 2026-08-28
- Scope: audiobook-local Web UX consolidation, tests, architecture gate and deployment evidence
- Baseline: `81395d5d3352303320e1a0f32b814091a7cfda86`
- Sanitization: no private identities, item IDs, addresses, tokens, stream URLs or provider payloads are published.

Detta är en sanerad GitHub-version. Lokal rå runtime- och browser-evidens publiceras inte.

## Status

**IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / OWNER UX REVIEW PENDING**

## Changes

BB-108/109:s positiva audiobook-evidens bevaras: Media är bounded, lång katalogscroll börjar först i Library och scroll-till-top är användbar. BB-110 konsoliderar återstående fysisk iPhone-feedback:

- **Bibliotek** använder `BBButton` secondary. Rotorsaken till den stora guldtexten var lokal Media-CSS som återfärgade tertiary och förstorade chevronen.
- Discovery visar en rubrik, ett universellt fält, språk och **Sök**. Library visar **Bibliotek**, tyst count, lokal input/sort och **Filtrera**.
- Ordningen är discovery → owned library → downloads.
- Hela den semantiska bokraden öppnar detail; separat **Visa ljudbok** är borttagen.
- Mobil jobbvy staplar titel, status och orsak med konsekvent padding och robust wrapping.
- Failed attention kan döljas device-localt. Det bounded presentationstillståndet raderar eller muterar aldrig jobb, audit, provider eller media.

Buttoninventeringen fann ett komplett aktuellt `BBButton`-kontrakt: primary, secondary, tertiary, icon, danger och contextual med gemensam busy/disabled/focus/press. Det finns legacy raw buttons utanför BB-110:s scope; de registreras som design-systemskuld och motiverar inte en okontrollerad global rewrite.

### Playback architecture gate

Audiobookshelf 2.36.0-kontrakten som verifierades i BB-109 kvarstår: user-owned session start, session tracks, sync/close och Range-sensitive seeking gör native playback tekniskt möjlig. De löser inte identiteten. Commissioning-serviceprofilen har ingen owner-progress medan en annan personlig profil har verklig progress.

Owner direction — service identity för katalog och user-mapped playback identity för lyssning — är sund men kräver ett varaktigt beslut om BigBrain user mapping, credential enrollment/lifecycle, authorization samt same-origin stream/Range/session/progress-sync. Nuvarande BigBrain-authmodell äger inte detta kontrakt. BB-110 stoppar därför player-implementationen vid arkitekturgaten i stället för att använda fel identitet eller exponera token. Native player, mini-player och korrekt owner Continue Listening är **BLOCKED** tills beslutet finns.

## Security

Inget media, providerjobb, acquisitionjobb eller auditrecord raderas, avbryts eller startas. Finance, scheduler, governor och Sentinel ändras inte. Audiobook-navigationen och whole-row-principen är inte globalt antagna. BB-110 kräver fysisk owner review i samtliga teman; automatiserad evidens innebär inte owner approval.

## Evidence

Fokuserade tester täcker canonical Bibliotek-variant, accessible compact copy, sektionsordning, semantic row navigation och persistent presentation-only dismiss: 25/25 passerade. Full regression passerade 147 Web, 558 API och 32 Sentinel. Release build hade 0 warnings/errors; Vite, Compose och 191-filers dokumentationsverifiering passerade.

Endast Web byggdes/återskapades och blev healthy. Deployad browsermatris 390×844, 430×932 och 1440×900 i Obsidian Gold, Forest Night och Arctic Wind passerade: overview hade 0 katalograder, collection 20 bounded rows, korrekt discovery → library → downloads, whole-row semantic targets, robust jobbwrapping, ingen overflow, 112 px mobil dock-clearance och scroll-top efter lång scroll. En första transient tom 430px-read stabiliserades vid identisk sekventiell omkörning; ingen mutation gjordes. Implementation-run `33179994941` passerade. En senare docs-run exponerade två testassertions som inte väntade på den separata provider-status-rendern; den scoped fixen upprepades två gånger fokuserat och full Web, varefter slutrun `33180923644` passerade backend, frontend, documentation och secrets för `87d8528791a32a0e4d0e66de400d5ca4d5c392c9`.

## Remaining work

På fysisk iPhone/PWA: bedöm Bibliotek-kontrollen, informationsdensiteten, discovery → library → downloads, whole-row tap/focus, långa jobbtitlar, Dölj-semantiken och dock/safe-area i tre teman. Native playback ska inte förväntas förrän identitetsbeslutet fattats.

### Owner follow-up captured by BB-111

Den fysiska BB-110-granskningen registrerade **NEEDS ITERATION** för fem punkter: falsk gul programmatisk route-focus på touch, forward detail som ärvde collection-scroll, **Bibliotek** som actionlik CTA i stället för navigation, lös/misaligned detail hero med deformerat/stort cover samt onödig `Språk okänt`-copy. Positiv evidens för bounded overview, separat collection, whole-row detail, sektionsordning, scroll-top och icke-destruktiv historypresentation kvarstår. BB-111 hanterar iterationen; navigationen är fortsatt inte globalt antagen och owner approval är fortsatt NO.

## Resumption

Utgå från publicerad BB-110-SHA och aktuell status. Implementera inte playback innan identitets- och same-origin-kontraktet har ett uttryckligt arkitekturbeslut. Registrera endast owner UX approval efter fysisk owner-feedback.
