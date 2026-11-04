# Morgonbladet checkpoint 1 — blockerad review, 2026-11-04

**REVIEW CHECKPOINT — BLOCKED / NOT MERGEABLE.** Exakt en A4-sida är inte
verifierad: Chromium producerade två sidor. Ingen merge eller deployment.
Ägaren begränsade arbetet till ett implementationsförsök och ett korrigeringsförsök;
båda är förbrukade. Nästa steg är oberoende arkitektreview och ett nytt uttryckligt
beslut om eventuell avgränsad korrigering, inte checkpoint 2.

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
