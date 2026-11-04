# Morgonbladet checkpoint 1 — A4-korrigering för review, 2026-11-04

## Metadata

- Datum: 2026-11-04.
- Risknivå: 1.
- Review: PR #22, befintlig checkpoint-branch.
- Verifierad kodrevision: `9f38e4db742907222b9f7b4c624e6cfa30eb97ce`.
- Omfattning: statisk A4-designmaster och utskrift.

Detta är en sanerad GitHub-version av dokumentationen.

## Status

**REVIEW CHECKPOINT — automatisk A4-verifiering PASS efter separat auktoriserad korrigering.**
Ingen merge, deployment eller slutlig owner UX approval. Verklig iPhone/PWA och fysisk
utskrift kräver fortfarande manuell acceptans. Oberoende review återstår.

## Changes

### Avgränsad korrigering efter `4cc68a4`

Ägaren auktoriserade ett nytt, enda korrigeringsförsök med jämförelse mot
`9631bd328b229263664e9071f2aaf980b89cf0ef`. Aktuell main är fortsatt
`c2d33099fe5af6d51a70a943b4006c7ea8f5f0bd`; samma PR #22 och historik bevaras.

Den äldre mekaniken använde en onamngiven `@page`-regel och återställde `body` till
`overflow: visible`. Appens `styles/modules.css` anger annars globalt A4 med 14 mm
marginal och `overflow-x: clip` på html/body. I `4cc68a4` satt den namngivna nollmarginalregeln
endast på bilden, medan föräldrarna hade `page: auto`. Noll CSS-marginal på `body`
eller CDP:s nollmarginaler tar inte bort CSS-sidans marginaler på den anonyma sidan.

En enda avgränsad CSS-korrigering återanvänder den äldre mekanikens onamngivna
`@page`, här med A4 portrait och margin 0, och `overflow: visible` på tidningens body.
Bildens `page: newspaper-master` tas bort. Detta ger samma sidregel genom det utskrivna
flödet. Komponenten äger fortfarande sin monterade style-tagg, portal och isolerade
utskrift; globala CSS-filer och andra modulers kod har inte ändrats.
Ingen gammal layout eller demotext återinförs. Bildens 210 × 297 mm-ram,
`object-fit: contain`, vita bakgrund och oförändrade originalasset bevaras.

Det observerade felet låg i samspelet mellan dessa utskriftsregler och Chromium-pagineringen:
föregående PDF hade samma bildobjekt fragmenterat över två sidor. Den sammanhållna
korrigeringen ger en sida. Reglerna har inte isolerats i separata experiment, så den
enskilda kausala andelen för overflow respektive sidregel är inte fastställd.

## Evidence

Verifiering 2026-11-04, Chromium 145.0.7632.6 och Node 20.19.2:

| Kontroll efter korrigering | Resultat |
| --- | --- |
| Befintligt, oförändrat `tests/newspaper/verify-print.mjs` | PASS, desktop 1280 × 900 och mobilviewport 390 × 844 |
| Verkliga PDF:er | PASS, exakt 1 sida vardera, 594.96 × 841.92 pt (A4) |
| Originalets SHA-256/mått, proportioner och print-ram | PASS i samma skript |
| PDF-bildobjekt och renderad desktop-PDF | PASS, en 2480 × 3508-bild på sida 1; hela designen synlig vid visuell kontroll |
| Fokuserade Newspaper/App/dashboard-tester | PASS, 26 tester i 5 filer |
| `npm run format:check`, `npm run build`, `git diff --check` | PASS |
| Verklig iPhone/Safari, installerad PWA och fysisk skrivare | PENDING, ingen manuell acceptans registrerad |
| Oberoende review och CI för korrigeringscommiten | PENDING |

Lokala artefakter från oförändrat PDF-test finns i `data/newspaper-checkpoint1/`:
- `desktop.pdf`: SHA-256 `470d5eab90a89ffe8154fcfb06180a54206e648507600e4d854e21e659c406b6`.
- `mobile.pdf`: SHA-256 `800b22fac62e5e6d2c4a2d79c4d510f2a54475110163408ba97525b127d63310`.
- `evidence.json`: browseridentitet och båda mätfallen.
- `desktop-before-correction.pdf`: bevarad tidigare tvåsidig PDF.

## Security

Den avgränsade korrigeringen ändrar inga API:er, databaser,
åtkomstregler eller produktionsinställningar. Inga nya
beroenden eller hemligheter har införts.

## Remaining work

Fysisk skrivares marginaler och Safaris utskriftsdialog är inte bevisade av Chromium-testet.
## Resumption

Ett korrigeringsförsök är förbrukat i detta nya uppdrag. Nästa steg är oberoende review
av publicerad commit och manuell acceptans; ingen checkpoint 2 eller automatisk fortsättning.

## Historisk blockerad checkpoint `4cc68a4`

Följande dokumenterar den ursprungliga implementationen och dess stopp, före det nya
ägaruppdraget ovan. Dess FAIL/SKIP och stoppstatus är historiska, inte aktuella PDF-resultat.

## Scope och revisionsunderlag

Risknivå 1, LOW–MEDIUM. Visa den befintliga oförändrade designmastern i Morgonbladet
och skriva ut exakt en A4-sida utan beskärning eller förvrängning. Inga textzoner,
dynamiska artiklar, integrationer, nya beroenden eller produktionsändringar.

Arbetet fortsätter samma branch och historik som [PR #22](https://github.com/T-bear/BigBrain/pull/22):
`review/chatgpt-20261030-230941-1430648`, föregående remote-SHA
`947e6528019ff48bfd51ef320c8060641c210473`.
Branchens ursprungliga main-baseline/merge-base är
`f8de9c447342f04314b12c59e9dec9f98418230d`.
Aktuell verifierad accepterad `origin/main` är
`c2d33099fe5af6d51a70a943b4006c7ea8f5f0bd`, tree
`021f00fb4d221806c39b6a43b978694b05eb1df2`.
Main har inte mergats in i reviewbranchen; testerna gäller reviewbranchen,
inte ett framtida kombinerat merge-resultat.

[PR #24](https://github.com/T-bear/BigBrain/pull/24), head
`575f254391e76458b5d3a85f33b542e1a827a4f2`, är fortsatt öppen och omergad.
Dess designmaster-kontrakt är läst men inte kopierat eller accepterat genom detta arbete.
Aktuell main och dess styrdokument/frysningsregister lästes före implementation.
Ägarens checkpoint-scope begränsar detta steg till den statiska bilden.

## Implementation och original

- Den äldre demolayouten i `Newspaper.tsx` ersätts av originalbilden och en utskriftsknapp.
- Befintlig widgetregistrering och portal direkt under `body` återanvänds.
- Preview har naturliga proportioner. Utskriftsbilden har `object-fit: contain`
  i en vit 210 × 297 mm-ram med namngiven A4-sida och noll CSS-sidmarginal.
- Knappen är spärrad före bildladdning och vid laddningsfel; portalen tas bort vid unmount.
- Ingen annan modul ändras. Tidigare demoassets i PR #22 bevaras men används inte.

Asset: `src/BigBrain.Web/public/newspaper/Morgonbladet_designmaster_A4.png`,
2480 × 3508 pixlar, 1 500 994 bytes. Byteidentisk kopia av ägarens tillhandahållna fil.
SHA-256: `9c3bb9676c337dc8bf6d97c93befd1fc18e03ef1200c5c089a119f45d9ad4733`.
Ingen bildgenerering, redigering eller omkodning. Visuell inspektion visar vit bakgrund.
Ingen slutlig owner UX approval registreras.

## Verifiering och experiment

Verifierat 2026-11-04 lokalt, utan API-anrop eller produktionsåtkomst:

| Kontroll | Resultat |
| --- | --- |
| Originalets SHA-256 och PNG-mått | PASS |
| `npm test -- src/newspaper/Newspaper.test.tsx src/App.test.tsx src/dashboard` | PASS, 26 tester i 5 filer |
| `npm run format:check` | PASS |
| Prettier-kontroll av `tests/newspaper/verify-print.mjs` | PASS |
| `npm run build` | PASS, TypeScript och Vite 8.1.5 |
| `git diff --check` | PASS |
| Chromium desktop-PDF, exakt en A4-sida | FAIL: 2 sidor, 594.96 × 841.92 pt (A4) |
| Mobilviewport-PDF | SKIP: skriptet stoppar vid desktop-felet |
| Verklig iPhone/Safari, installerad PWA och fysisk utskrift | PENDING, manuell acceptans |
| Oberoende arkitektreview och CI för publicerad commit | PENDING |

Första testkörningen passerade 27 tester men builden misslyckades eftersom det
nya hashtestet importerade Node-moduler utan Node-typer i frontendens tsconfig.
Det enda korrigeringsförsöket flyttade hashkontrollen till det separata Node-skriptet,
utan att installera typer eller beroenden. Första skriptstarten på Node 20.19.2
saknade global WebSocket; körkommandot använder nu den inbyggda experimentflaggan.
Därefter passerade build/tester ovan, men faktisk PDF-rendering visade två sidor.
Ingen ytterligare produktkorrigering eller automatisk retry utfördes.

Reproduktion från repositoryroten med befintlig Chromium och Poppler `pdfinfo`:

```sh
CHROMIUM_BIN=/path/to/chromium node --experimental-websocket tests/newspaper/verify-print.mjs
```

Skriptet renderar den riktiga React-komponenten tillsammans med applikationens CSS
i en tillfällig lokal Vite-harness. Det kontrollerar bildhash, naturliga proportioner,
print-ramens mått, att applikationsroten döljs och faktisk PDF-paginering.
Det skapar endast en lokal server på loopback och en egen Chromium-process;
båda stängs i `finally` och den tillfälliga HTML-filen tas bort.
Utskriften använder `preferCSSPageSize`, utan browser headers/footers eller background graphics.
Chromium/Skia m145, installerad headless shell-build 1208, Node 20.19.2.

Lokal artefakt: `data/newspaper-checkpoint1/desktop.pdf`, 2 275 276 bytes.
SHA-256: `af4354e8587652c5f7dc771bef5ae22932472eea10e5cdc2f376fdfadecf8cff`.
PDF är inte publicerad; reproduktionsskript och denna sanerade evidens är versionshanterade.
JSON-slutrapport skrivs inte när ett påstående fallerar. Orsaken till extrasidan
är inte fastställd. CSS-mått och gröna komponenttester bevisar inte korrekt paginering.

## Kvarstående risk och stopp

Kravet på en sida är blockerande. Fysisk skrivares marginaler och Safari/PWA-beteende
är inte verifierade. Inget godkännande av slutlig design eller utskrift följer av denna
checkpoint. Ingen ytterligare implementation innan ett nytt avgränsat ägarbeslut.
Se [enda recovery-noten](../../../operations/codex-recovery.md) för återupptagningsstatus.
