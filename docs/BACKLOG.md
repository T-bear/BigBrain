# BigBrain Backlog

Senast uppdaterad: 2026-08-05

Detta dokument samlar verifierade buggar, teknisk skuld och framtida förbättringar som ännu inte är implementerade.

Prioritet:

- P0 – kritiskt fel eller risk för dataförlust
- P1 – blockerande eller tydligt störande fel
- P2 – viktigt men arbetet kan fortsätta
- P3 – förbättring eller lågprioriterad teknisk skuld

Status:

- Ny
- Bekräftad
- Planerad
- Pågår
- Klar
- Avvisad

---

## Buggar

### BB-030 – Dashboardinställningar under kugghjulsmeny

- Modul: BigBrain Web / Dashboard
- Typ: UX / navigering och inställningar
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-05

#### Beskrivning

Kontrollerna Tema, Redigera och Widgetbibliotek visas permanent högst upp i varje dashboardvy. De tar mycket vertikalt utrymme på mobil och är sekundära inställningsfunktioner snarare än primärt innehåll.

#### Önskat beteende

- Visa en tydlig kugghjulsknapp för dashboardinställningar.
- Tema, Redigera och Widgetbibliotek öppnas därifrån.
- Menyn gäller aktuell dashboardvy.
- Aktivt redigeringsläge framgår fortfarande tydligt.
- Tema förblir globalt om det är det nuvarande kontraktet.
- Funktionerna försvinner inte och blir inte svårare att nå.
- Menyn fungerar med touch, tangentbord, Escape, fokusfälla och fokusåterställning.
- Lösningen fungerar i alla teman och på mobil och desktop.

#### Avgränsning

Ingen ändring av widgetpersistens, moduldata, dashboardprofiler eller backend.

#### Definition of Done

- Tema, Redigera och Widgetbibliotek ligger bakom en tydlig kugghjulskontroll.
- Dashboardens primära innehåll börjar högre upp på mobil.
- Alla tre funktionerna är fullt åtkomliga.
- Menyn har korrekt tillgänglighetssemantik.
- Aktivt redigeringsläge är tydligt.
- Verifierat i Ljust, Mörkt och Obsidian Gold.
- Regressionstester och manuell mobilverifiering finns.

#### Manuell evidens

På iPhone i Obsidian Gold tar Tema-väljaren samt knapparna Redigera och Widgetbibliotek en stor del av den övre dashboardytan.

### BB-031 – Download Control orsakar horisontell overflow på mobil

- Modul: Media / Download Control
- Typ: Bugg / mobil layout / overflow
- Prioritet: P1
- Status: Bekräftad
- Upptäckt: 2026-08-05

#### Beskrivning

När widgeten Nedladdningar är öppen på mobil blir delar av Download Control bredare än widgeten och viewporten. Informationsrutor, filter, åtgärdsknappar och långa torrentnamn kan fortsätta utanför högerkanten.

#### Nuvarande beteende

- Uppdateringsknappen kapas eller hamnar delvis utanför.
- Filterraden fortsätter utanför widgetens bredd.
- Torrentkort och Hantera-knappar kan bli bredare än tillgängligt utrymme.
- Långa namn pressar layouten horisontellt.
- Innehåll döljs bakom viewportens högra kant.

#### Förväntat beteende

- Ingen sida- eller widgetövergripande horisontell scroll.
- Allt innehåll håller sig inom widgetens inre bredd.
- Långa torrentnamn radbryts eller trunkeras kontrollerat.
- Filter får radbrytas eller ligga i en avsiktlig intern scrollrad utan att hela sidan expanderar.
- Knappar anpassas till mobilbredd.
- Progressindikator och metadata håller sig inom kortet.
- Desktoplayouten försämras inte.

#### Tekniska kontrollpunkter

- `min-width: 0` på grid- och flexbarn.
- `overflow-wrap` och `word-break` för långa release-namn.
- Fasta bredder och `width: max-content`.
- `flex-wrap` för header och filter.
- `max-width: 100%`.
- `box-sizing`.
- Eventuell `overflow-x` på fel container.

#### Definition of Done

- Ingen horisontell dokument-scroll vid 320, 375 och 390 px.
- Header, filter, torrentkort och knappar ryms.
- Mycket långa torrentnamn förstör inte layouten.
- Download Control är användbart i alla teman.
- Regressionstest använder representativt långt torrentnamn.
- Manuell verifiering genomförs på iPhone.

#### Manuell evidens

På Media-vyn i mobilformat går uppdateringsknappen, filterraden och torrentinformationen utanför widgetens högra kant.

### BB-032 – Kalenderns Heroma-importdialog orsakar horisontell scroll

- Modul: Kalender / Heroma-import
- Typ: Bugg / mobil modal / overflow
- Prioritet: P1
- Status: Bekräftad
- Upptäckt: 2026-08-05

#### Beskrivning

När dialogen Importera Heroma-schema öppnas på mobil visas en horisontell scrollbar längst ned. Dialogens innehåll är bredare än den visuella viewporten.

#### Nuvarande beteende

- Importdialogen kan scrollas horisontellt.
- Delar av dialogen sträcker sig utanför skärmen.
- Filväljaren, knappen Förhandsgranska eller dialogens inre panel misstänks skapa en för stor minsta bredd.
- Problemet är särskilt tydligt i mörkt tema på iPhone.

#### Förväntat beteende

- Dialogen är aldrig bredare än mobilens visuella viewport.
- Endast vertikal scroll används när innehållet är långt.
- Filväljare, statusrad, förhandsgranskningsknapp och stängknapp ryms inom dialogen.
- Långa filnamn radbryts eller trunkeras säkert.
- Safe-area-insets respekteras.
- Dialogen fungerar även med flera valda filer och långa filnamn.

#### Tekniska kontrollpunkter

- `width` och `max-width` med hänsyn till viewport och safe area.
- `min-width: 0` på dialogens barn.
- Native `input[type=file]`.
- `box-sizing`.
- Padding plus border.
- `100vw` kontra `100dvw`.
- Långa filnamn och flex-/gridbarn.
- `overflow-x` på dialog, overlay och body.

#### Definition of Done

- Ingen horisontell scrollbar vid 320, 375 och 390 px.
- Dialogen ryms inom viewporten i alla teman.
- Flera filer och långa filnamn förstör inte layouten.
- Endast avsedd vertikal dialogscroll används.
- Bakgrundssidan förblir låst.
- Escape, fokusfälla och fokusåterställning fungerar fortsatt.
- Regressionstest och manuell iPhone-verifiering finns.

#### Manuell evidens

På iPhone visas en tydlig horisontell scrollbar längst ned i dialogen Importera Heroma-schema.

### BB-001 – Ingen synlig återkoppling när en dubblettvara stoppas

- Modul: Inköpslista
- Typ: UX / felhantering
- Prioritet: P2
- Status: Bekräftad
- Upptäckt: 2026-08-02

#### Beskrivning

När användaren försöker lägga till en vara som redan finns i inköpslistan skapas ingen dubblett, vilket är korrekt. Däremot visas inget synligt meddelande eller någon dubblettdialog för användaren.

#### Nuvarande beteende

- API eller datalager stoppar dubbletten.
- Ingen extra vara läggs till.
- Användaren får ingen tydlig förklaring.

#### Förväntat beteende

Användaren ska få en tydlig återkoppling, exempelvis:

- `Korv finns redan på listan.`
- möjlighet att öka antal;
- möjlighet att visa den befintliga varan;
- möjlighet att avbryta och fortsätta skriva.

#### Risk

Användaren kan tro att knapptryckningen inte fungerade och försöka flera gånger.

#### Avgränsning

Ingen ändring av dubblettregeln eller datamodellen krävs. Felsök frontendens hantering av API-svaret och dubblettdialogens rendering.

#### Definition of Done

- Ett stoppat dubblettförsök ger synlig återkoppling.
- Ingen dubblett skapas.
- Fokus återgår till ett logiskt ställe.
- Dialogen visas ovanför handlingsläget.
- Fungerar på mobil och desktop.
- Regressionstest finns.

---

### BB-002 – Bakgrundssidan kan scrollas bakom handlingsläget

- Modul: Inköpslista
- Typ: Mobil UX / modalhantering
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-02

#### Beskrivning

När Inköpslistas fullskärmsläge är öppet går det ibland att scrolla innehållet bakom fullskärmsytan.

#### Nuvarande beteende

- Bakgrundsinnehållet kan ibland förflyttas.
- Felet verkar vara intermittent.
- Det är ännu inte fastställt vilka steg som alltid reproducerar det.

#### Förväntat beteende

När handlingsläget är öppet ska endast inköpslistans interna innehåll kunna scrollas. Sidan bakom ska vara helt låst.

#### Misstänkta utlösare

- mobilens tangentbord öppnas eller stängs;
- radmeny öppnas;
- dubblettdialog öppnas;
- handlingsläget öppnas och stängs flera gånger;
- iOS ändrar höjden på den visuella viewporten.

#### Definition of Done

- Bakgrundsscroll är låst under hela handlingsläget.
- Ursprunglig scrollposition återställs när läget stängs.
- Ingen hoppande sida vid öppning eller stängning.
- Verifierat i riktig Chromium och manuellt på iPhone.
- Regressionstest eller reproducerbar browserkontroll finns.

---

### BB-003 – Scrollindikator visas trots att listan verkar rymmas

- Modul: Inköpslista
- Typ: Mobil layout / overflow
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-02

#### Beskrivning

I handlingsläget kan en scrollbar eller scrollindikator ibland visas på höger sida trots att listans innehåll inte ser ut att kräva scrollning.

#### Nuvarande beteende

- Scrollindikatorn visas intermittent.
- Det är oklart om en liten dold overflow faktiskt finns.
- Felet kan vara kopplat till viewporthöjd, tangentbord eller fokus.

#### Förväntat beteende

Ingen intern scrollindikator ska visas när hela innehållet ryms i viewporten.

#### Tekniska kontrollpunkter

- `100vh` kontra `100dvh`;
- marginaler eller padding som skapar några pixels overflow;
- fokus- eller keyboardförändringar;
- dubbla scrollcontainers;
- `min-height`, footer och safe-area-insets;
- portalerade menyer eller dialoger som påverkar dokumenthöjden.

#### Definition of Done

- En kort lista ger ingen onödig scrollbar.
- En lång lista scrollar endast i avsedd intern container.
- Samma beteende vid 320×844 och 390×844.
- Tangentbord, radmeny och dialog orsakar inte falsk overflow.

---

### BB-016 – Ofta köpt visar varor som redan finns i inköpslistan

- Modul: Inköpslista
- Typ: UX / filtrering
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-04

#### Beskrivning

Sektionen ”Ofta köpt” visar varor som redan finns i den aktiva inköpslistan. När en vara läggs till från sektionen kan förslaget dessutom ligga kvar trots att det inte längre är relevant.

#### Nuvarande beteende

- En vara kan visas samtidigt under ”Ofta köpt” och ”Att köpa”.
- Ett förslag kan ligga kvar efter att användaren har lagt till varan.
- Användaren kan få intrycket att samma vara fortfarande behöver läggas till.
- Gränssnittet använder utrymme till förslag som inte längre kan hjälpa användaren.

#### Förväntat beteende

- Varor som redan finns i inköpslistan filtreras bort från ”Ofta köpt”.
- En vara som läggs till från ”Ofta köpt” försvinner direkt efter lyckat tillägg.
- Tillägg genom vanlig textinmatning uppdaterar också förslagen.
- En borttagen vara kan återkomma när den fortfarande kvalificerar sig som ofta köpt.
- Filtreringen använder samma namnnormalisering som dubblettkontrollen.
- Sektionen visas inte som en tom eller irrelevant förslagsyta när inga förslag återstår.

#### Risk

Dubbel information skapar visuell oreda och kan leda till förvirring eller upprepade försök att lägga till en vara som redan finns i listan.

#### Avgränsning

Denna backlogregistrering bestämmer inte om filtreringen slutligen ska ske i frontend, backend eller båda. Vid implementation ska befintlig datamodell, dubblettregel och källa för ”Ofta köpt” först inspekteras. Ingen ändring av statistik eller historik för ofta köpta varor efterfrågas.

#### Definition of Done

- Ingen vara visas samtidigt i ”Ofta köpt” och den aktiva inköpslistan.
- Ett förslag försvinner direkt efter ett lyckat tillägg från ”Ofta köpt”.
- Ett lyckat tillägg genom vanlig textinmatning uppdaterar också förslagen.
- En borttagen vara kan återkomma om den fortfarande kvalificerar sig.
- Versaler, gemener och omgivande blanksteg hanteras konsekvent med dubblettkontrollen.
- Ingen felaktig dubblett skapas.
- Sektionen hanterar noll återstående förslag enligt befintlig UI-standard.
- Beteendet fungerar på mobil och desktop.
- Regressionstester finns för relevant filtreringslogik och användarflöde.

#### Manuell evidens

På mobilvyn observerades att ”Mjölk” visades både under ”Ofta köpt” och i den aktiva listan ”Att köpa”.

---

### BB-017 – Smart Shuffle kan inte starta uppspelning på verifierad Samsung Tizen-TV

- Modul: Media / Smart Shuffle
- Typ: Produktionsbugg / Jellyfin remote playback
- Prioritet: P1
- Status: Klar
- Upptäckt: 2026-08-04

#### Beskrivning

Smart Shuffle kan läsa Jellyfin-biblioteket, visa serier och identifiera en verifierad och fjärrstyrbar Samsung Smart TV via Jellyfin for Tizen. När användaren trycker ”Starta på TV” startas ingen uppspelning och BigBrain visar det sanerade felet ”Jellyfin kunde inte utföra Smart Shuffle-åtgärden.”

#### Reproduktionssteg

1. Öppna Jellyfin-appen på Samsung-TV:n.
2. Kontrollera att vanlig manuell uppspelning fungerar.
3. Öppna BigBrain → Media → Smart Shuffle.
4. Välj minst två serier.
5. Välj Samsung Smart TV.
6. Tryck ”Starta på TV”.
7. Observera att ingen uppspelning startar och att BigBrain visar standardfelet.

#### Förväntat beteende

- BigBrain revaliderar vald TV-session.
- BigBrain väljer korrekt nästa osedda eller påbörjade episod.
- BigBrain skickar ett versionskorrekt PlayNow-kommando.
- Rätt avsnitt startar på exakt vald TV.
- BigBrain verifierar att samma avsnitt blir NowPlayingItem.
- Smart Shuffle-sessionen övergår till aktiv status.

#### Verifierad grundorsak

Det tidigare UI-styrda försöket nådde aldrig PlayNow. En sekventiell episodfråga timeoutade mot Jellyfin efter den generella tresekundersgränsen och `TaskCanceledException` lämnade Smart Shuffle-felmodellen som ett ohanterat 500-svar.

#### Fix och verifiering

Episodkontrollen körs parallellt med en avgränsad Smart Shuffle-timeout. TV-session, användare och fjärrstyrbarhet revalideras precis före ett versionskorrekt PlayNow-anrop. Accepterad uppspelning skiljs från inväntad och bekräftad uppspelning, med en begränsad verifieringsperiod anpassad för Tizen. Säkra felkategorier, startspärr mot dubbelklick och regressionstester har lagts till.

Ett verkligt UI-styrt knapptryck på den slutliga versionen gav exakt ett startkommando, Jellyfin svarade `204`, rätt avsnitt blev `NowPlayingItem` och BigBrain-sessionen blev `active`. Användaren bekräftade att allt fungerade på Samsung-TV:n. Även användarstyrda hopp till nästa avsnitt accepterades och bekräftades utan terminalbaserad uppspelning.

Testbevis: 186 API-tester och 32 Sentinel-tester godkända; 76 frontendtester och production build godkända. Permanent rapport: `/home/enigma/BigBrain/reports/features/smart-shuffle/smart-shuffle-p1-playback-start-fix-20260804-153939.txt`.

#### Definition of Done

- Exakt grundorsak identifierad.
- Jellyfin 10.11.11-kontraktet verifierat.
- Session, användare och episod revalideras precis före start.
- Rätt session-ID och item-ID används internt.
- Query-parametrar och requestformat är versionskorrekta.
- Upstream-status och säker felkategori loggas utan hemligheter.
- Användarens UI-klick startar rätt avsnitt på Samsung-TV:n.
- NowPlayingItem verifieras efter start.
- Dubbelklick kan inte orsaka dubbla starter.
- Automatiska tester täcker grundorsaken och relevanta fel.
- Permanent verifieringsrapport skapad.
- Buggen markeras som löst först efter verklig UI-styrd TV-verifiering.

---

### BB-018 – Smart Shuffle – Jellyfins ”Nästa avsnitt” visas missvisande mellan serier

- Modul: Media / Smart Shuffle
- Typ: UX / Jellyfin-klientintegration
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-04

#### Beskrivning

När ett avsnitt avslutas under Smart Shuffle visar Jellyfin for Tizen den vanliga ”Nästa avsnitt”-rutan för nästa avsnitt i samma serie. Smart Shuffle väljer däremot nästa serie enligt sin rättvisa shufflealgoritm. Funktionen fortsätter att fungera, men Jellyfins förslag blir missvisande och visuellt störande.

#### Förväntat beteende

Smart Shuffle och Jellyfins klientgränssnitt ska ge en begriplig och konsekvent övergång mellan serier. Jellyfins normala nästa-avsnitt-funktion ska fortsatt fungera vid vanlig Jellyfin-uppspelning utanför Smart Shuffle.

#### Utredningspunkter

- Verifiera om Jellyfin 10.11.11 eller Jellyfin for Tizen dokumenterar ett sessionsavgränsat sätt att stänga av eller undvika klientens vanliga ”Nästa avsnitt”-ruta.
- Utred om ett annat dokumenterat playbackflöde kan göra Smart Shuffle-övergången tydligare utan att störa vanlig Jellyfin-användning.
- Om klientbeteendet inte säkert kan påverkas, utred dokumentation av begränsningen eller en tydlig förklaring i Smart Shuffle-gränssnittet.
- Inför inte odokumenterade Jellyfin-endpoints eller klienthack.

#### Avgränsning

Ingen global Jellyfin-inställning får försämra eller stänga av den normala nästa-avsnitt-funktionen utanför Smart Shuffle. Implementationen ska föregås av verifiering av installerad serverversion, Tizen-klientens beteende och dokumenterade kontrakt. Ingen kod-, runtime-, Compose-, Jellyfin- eller Tizen-konfigurationsändring ingår i denna backlogregistrering.

#### Definition of Done

- Installerad Jellyfin-version och Jellyfin for Tizen-beteendet verifieras.
- Det dokumenterade Jellyfin-kontraktet för completion, autoplay, PlayNext och nästa-avsnitt-UI undersöks.
- Det fastställs om klientens ruta kan påverkas per Smart Shuffle-session.
- Ingen global Jellyfin-inställning ändras utan separat uttryckligt beslut.
- Vanlig manuell Jellyfin-uppspelning påverkas inte.
- Smart Shuffles automatiska seriebyte fortsätter att fungera.
- Ingen odokumenterad eller versionsosäker endpoint används.
- Automatisk övergång, skip och stop regressionstestas.
- Lösningen verifieras manuellt på Samsung Smart TV.
- Relevant dokumentation och permanent verifieringsrapport uppdateras.

---

### BB-019 – BigBrain saknar säker borttagning av oönskad nedladdning

- Modul: Media / Download Control
- Typ: Funktion / säker extern mutation
- Prioritet: P2
- Status: Pågår
- Upptäckt: 2026-08-04

#### Beskrivning

BigBrain saknade ett objektspecifikt och bekräftat sätt att avbryta ett oönskat qBittorrent-jobb utan att exponera rå torrentidentitet eller riskera andra jobb och media.

#### Implementerad MVP

Listning, opaka kortlivade ID:n, live-revalidering, filbevarande standardborttagning, separat riskgrindad destruktiv borttagning, Arr-varning, säkra fel och automatiska fake-baserade tester är implementerade och deployade. Användaren har genom BigBrains UI bekräftat att minst ett fastnat jobb togs bort från qBittorrent med filerna bevarade. Full manuell verifiering av `deleteFiles=true`, samtliga destruktiva riskscenarier och konsekvenser för importerad media/Arr återstår; status förblir därför Pågår.

#### Manuell status och kvarvarande verifiering

- Filbevarande borttagning genom BigBrain UI: verifierad av användaren.
- Borttagning från qBittorrent: verifierad av användaren.
- Destruktiv dataradering: inte fullständigt produktionsverifierad.
- Retry, pausa/återuppta, Arr Recovery, diagnostik, masshantering och retention: separata backlogposter BB-020–BB-026.

#### Definition of Done

- Exakt ett liveverifierat jobb påverkas per request.
- Normal borttagning använder `deleteFiles=false` och bevarar data.
- Destruktiv borttagning är separat, explicit och blockeras vid osäker risk.
- Rå hash, credentials, paths och upstreamfel exponeras inte.
- Sonarr/Radarr-ägarskap varnas för utan Arr-mutation.
- Backend-/frontendtester och production build är gröna.
- Verklig UI-styrd testborttagning verifieras på uttryckligt testjobb.
- Permanent rapport publiceras och indexeras.

---

### BB-020 – Download Control – säker masshantering

- Modul: Media / Download Control
- Typ: Framtida funktion / destruktiv batchhantering
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-04
- Beskrivning: Utred separat preview, målmanifest, bekräftelse och audit för flera torrentjobb. MVP:n får fortsatt endast påverka ett jobb per request.
- Definition of Done: Separat arkitekturbeslut, explicit målmanifest, total riskbedömning, ingen `all`-parameter, batchtester och manuell verifiering.

---

### BB-021 – Download Control – koordinerad Sonarr/Radarr-recovery

- Modul: Media / Download Control
- Typ: Framtida funktion / Arr-orkestrering
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-04
- Beskrivning: Utred ett separat uttryckligt flöde för blocklist, köborttagning och ny sökning när Arr äger jobbet. MVP:n muterar endast qBittorrent.
- Definition of Done: Versionsverifierade Arr-kontrakt, separat preview/bekräftelse, idempotens, ingen dold sökning och end-to-end-test med ofarligt testjobb.

---

### BB-022 – Download Control – säker rensning och retention för avslutade jobb

- Modul: Media / Download Control
- Typ: Framtida funktion / retention
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-04
- Beskrivning: Definiera en separat policy för avslutade jobb, importerad media och eventuell datarensning. MVP:n blockerar destruktiv borttagning av färdiga/importosäkra jobb.
- Definition of Done: Beslutad retentionpolicy, verifierat import- och hårdlänkskontrakt, säkra undantag, audit, rollbackstrategi och manuell verifiering.

---

### BB-023 – Download Control – Försök igen (Retry)

- Modul: Media / Download Control
- Typ: Funktion
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-04

#### Beskrivning

Användaren ska kunna försöka återuppliva en nedladdning som fastnat utan att behöva öppna qBittorrent.

Exempel på åtgärder:

- reannounce mot trackers;
- Force Resume om torrenten är pausad;
- uppdatera status efter utförd åtgärd;
- visa säkra felmeddelanden;
- aldrig exponera hash eller råa API-svar.

#### Definition of Done

- Exakt ett torrentjobb påverkas.
- Säkra felkoder används.
- Ingen påverkan på andra torrents.
- Fullständig backend- och frontendtestning finns.
- Dokumentationen är uppdaterad.

---

### BB-024 – Download Control – Pausa / Återuppta

- Modul: Media / Download Control
- Typ: Funktion
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-04

#### Beskrivning

BigBrain ska kunna pausa och återuppta en enskild nedladdning.

#### Definition of Done

- Pausa fungerar.
- Återuppta fungerar.
- Status uppdateras direkt.
- Ingen massoperation används.
- Endast ett jobb påverkas.
- Tester och dokumentation är uppdaterade.

---

### BB-025 – Download Control – Arr Recovery

- Modul: Media / Download Control
- Typ: Funktion
- Prioritet: P3
- Status: Avvisad
- Upptäckt: 2026-08-04

#### Beskrivning

Skapa ett separat återställningsflöde för nedladdningar som ägs av Sonarr eller Radarr.

Exempel på åtgärder:

- ta bort torrent;
- valfri blocklist;
- starta ny sökning;
- visa tydligt vad som kommer att hända innan något utförs.

Detta ska vara ett eget arbetsflöde och inte blandas ihop med vanlig borttagning.

Posten är en dubblett av BB-021 och ersätts av den mer preciserade posten där. Ingen
implementation eller historik har tagits bort.

#### Definition of Done

- Preview finns.
- Bekräftelse krävs.
- Säker rollback vid fel finns.
- End-to-end-test finns.
- Dokumentationen är uppdaterad.

---

### BB-026 – Download Control – Diagnostik (”Varför laddar den inte ner?”)

- Modul: Media / Download Control
- Typ: UX / funktion
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-04

#### Beskrivning

BigBrain ska analysera varför en nedladdning inte gör framsteg och ge användaren en begriplig förklaring i stället för enbart rå status.

Exempel på diagnoser:

- inga seeders;
- tracker svarar inte;
- torrent pausad;
- väntar på metadata;
- disk full;
- Sonarr/Radarr väntar;
- fel autentisering;
- timeout;
- nätverksproblem.

För varje diagnos ska BigBrain även föreslå en lämplig åtgärd.

#### Definition of Done

- Diagnoser visas med mänskligt språk.
- Felsökningen bygger på verifierad data.
- Ingen rå intern information exponeras.
- Tester finns.
- Dokumentationen är uppdaterad.

---

## Dokumentationsstyrning

### BB-004 – Arkivera ARR-incidentens mellanrapporter

- Modul: Dokumentation/Operations
- Typ: Dokumentationsskuld
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Arkivera mellanrapporter additivt med manifest och checksummor.
- Motiv: Skilja slutrapport och aktiv diagnostik från historisk evidens.
- Avgränsning: Ingen radering; flytt kräver separat review och godkännande.
- Risk: Brutna referenser eller förlorad spårbarhet.
- Definition of Done: Godkänt manifest, checksummor, uppdaterade index och verifierade länkar.
- Relaterade dokument: `docs/indexes/documentation.md`, ADR 0010, extern `arr-incident-index.txt`.

### BB-005 – Revidera README mot aktuell implementation

- Modul: Projektdokumentation
- Typ: Dokumentationsskuld
- Prioritet: P2
- Status: Klar
- Upptäckt: 2026-08-03
- Beskrivning: Uppdatera funktioner, läsordning och länkar efter faktagranskning.
- Motiv: README speglar inte hela verifierade implementationen.
- Avgränsning: Ingen arkitekturändring eller ny funktion.
- Risk: Felaktig onboarding.
- Definition of Done: Kort, kodverifierad README med auktoritativa länkar.
- Relaterade dokument: `README.md`, `docs/indexes/documentation.md`, `docs/history/early-sprints.md`.
- Slutförd: 2026-08-04; långlivad produktöversikt verifierad och relevant tidig historik bevarad separat.

### BB-006 – Dela upp och korta STATUS.md

- Modul: Projektdokumentation
- Typ: Informationsarkitektur
- Prioritet: P2
- Status: Klar
- Upptäckt: 2026-08-03
- Beskrivning: Begränsa STATUS till aktuellt läge och placera historisk verifiering efter review.
- Motiv: Filen blandar sprintlogg, runtimeevidens, problem och produktstatus.
- Avgränsning: Bevara historik; inga flyttar utan review.
- Risk: Förlust av evidens eller dubbla sanningar.
- Definition of Done: Definierat ansvar, indexerad historik och verifierade länkar.
- Relaterade dokument: `docs/STATUS.md`, `docs/reports/REPORT-CATALOG.md`, ADR 0010.
- Slutförd: 2026-08-04; kompakt modulstatus skiljer implementation, test, deployment och manuell verifiering.

### BB-007 – Besluta om en enda produktroadmap

- Modul: Produktstyrning
- Typ: Governance
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Fastställ ROADMAP.md:s ansvar och relation till backlog och stabiliseringsplan.
- Motiv: Parallella planeringsytor skapar otydlig prioritet.
- Avgränsning: Ingen reprioritering före produktägarreview.
- Risk: Konkurrerande planer styr arbetet.
- Definition of Done: En normativ roadmap med ägare, scope och länkar.
- Relaterade dokument: `ROADMAP.md`, `docs/BACKLOG.md`, `STABILIZATION_PLAN.md`.

### BB-008 – Aktivera eller avveckla CHANGELOG-policy

- Modul: Releasehantering
- Typ: Governance
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Besluta om CHANGELOG ska underhållas och definiera trigger och format.
- Motiv: En tom eller oägd changelog ger falska förväntningar.
- Avgränsning: Ingen retroaktiv historik utan verifierbar källa.
- Risk: Releaseförändringar blir svåra att följa.
- Definition of Done: Dokumenterad policy och första tillämpning, eller tydlig avveckling.
- Relaterade dokument: `CHANGELOG.md`, `ROADMAP.md`.

### BB-009 – Klassificera Proposed ADR 0002 och 0006–0009

- Modul: Arkitektur
- Typ: ADR-review
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Granska om förslagen ska accepteras, ersättas eller förbli Proposed.
- Motiv: Långvarigt Proposed-läge gör auktoriteten oklar.
- Avgränsning: Status ändras endast genom explicit arkitekturreview.
- Risk: Implementation och föreslaget beslut divergerar.
- Definition of Done: Varje ADR har dokumenterat reviewbeslut och evidens.
- Relaterade dokument: `docs/adr/0002-sentinel-exclusive-system-access.md`, `docs/adr/0006-sentinel-local-transport-identity-and-request-proof.md`, `docs/adr/0007-sentinel-v1-system-metrics-schemas-and-compatibility.md`, `docs/adr/0008-sentinel-system-metrics-policy-classification-and-audit.md`, `docs/adr/0009-sentinel-system-metrics-packaging-privilege-and-supply-chain.md`.

### BB-010 – Flytta TESTING.md till rätt runbookstruktur

- Modul: Kvalitet
- Typ: Dokumentationsstruktur
- Prioritet: P3
- Status: Klar
- Upptäckt: 2026-08-03
- Beskrivning: Separera testpolicy från körinstruktioner och senare placera dem rätt.
- Motiv: Normativ policy och procedur bör vara tydligt åtskilda.
- Avgränsning: Ingen flytt i denna fas; länkar granskas först.
- Risk: Brutna länkar eller otydlig Definition of Done.
- Definition of Done: Godkänd målstruktur, bevarad historik och gröna länkar.
- Relaterade dokument: `TESTING.md`, `docs/operations/runbooks/dashboard-widget-framework-verification.md`.
- Slutförd: 2026-08-04; rotfilen är en testkarta och Dashboard-proceduren har en verifierad runbook.

### BB-011 – Konsolidera STABILIZATION_PLAN

- Modul: Produktstyrning
- Typ: Dokumentationsskuld
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Separera roadmap, teknisk skuld och verifierade buggar.
- Motiv: En plan ska inte fungera som osorterad felinkorg.
- Avgränsning: Inget stängs eller flyttas utan evidens och review.
- Risk: Prioriteringar och problemstatus misstolkas.
- Definition of Done: Varje punkt har rätt hemvist, ägare och status.
- Relaterade dokument: `STABILIZATION_PLAN.md`, `ROADMAP.md`, `docs/BACKLOG.md`.

### BB-012 – Införa återkommande baseline-review

- Modul: Operations/Dokumentation
- Typ: Governance
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Definiera intervall, scopeägare och current-post per baselinetyp.
- Motiv: Daterad evidens får inte bli odaterad sanning.
- Avgränsning: Ingen automation innan retention och secret-scan godkänts.
- Risk: Föråldrade baselines styr felsökning och beslut.
- Definition of Done: Godkänd kalender, ägare, statusmodell och indexuppdatering per scope.
- Relaterade dokument: `docs/indexes/baselines.md`, ADR 0010.

### BB-013 – Persist Smart Shuffle sessions

- Modul: Media
- Typ: MVP-begränsning
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Ersätt processlokal Smart Shuffle-state först när restartåterställning eller flera API-repliker krävs.
- Motiv: MVP:n förlorar automationstillstånd vid API-restart och stoppar inte redan startad TV-uppspelning.
- Avgränsning: Ingen databas eller distribuerad låsning införs utan verifierat behov och nytt arkitekturbeslut.
- Definition of Done: Godkänd persistensmodell, säkert återupptagande, idempotenta övergångar och multi-replika-test.
- Relaterade dokument: `docs/modules/media.md`, ADR 0011.

### BB-014 – Smart Shuffle – manuell TV-verifiering och MVP-härdning

- Modul: Media / Smart Shuffle
- Typ: Verifiering / härdning
- Prioritet: P2
- Status: Pågår
- Upptäckt: 2026-08-03
- Beskrivning: Smart Shuffle MVP är implementerad, publicerad och automatiskt testad. UI-styrd start, rätt `NowPlayingItem` och användarstyrt skip är verifierade på den verkliga Samsung-TV:n. Naturlig avsnittsövergång, stopplivscykel och API-restartens processlokala beteende återstår för fullständig end-to-end-verifiering.
- Avgränsning: Verifieringen ska utlösas genom användarens BigBrain-gränssnitt; ingen terminal eller automatiskt test får starta verklig uppspelning.
- Definition of Done:
  - TV:n visas som valbar enhet i BigBrain utan rått UserId eller session-ID.
  - Användarens knapptryck startar exakt seriens valda nästa osedda avsnitt.
  - Nästa serie väljs automatiskt när avsnittet slutar och samma serie undviks direkt när alternativ finns.
  - Skip fungerar mot den verkliga TV-sessionen.
  - Stoppa Smart Shuffle förhindrar nya automatiska byten utan att störa vanlig manuell Jellyfin-användning.
  - Telefonens BigBrain-sida behöver inte hållas öppen.
  - API-restartens processlokala MVP-beteende verifieras och dokumenteras.
  - En slutlig, sekretessgranskad verifieringsrapport skapas.
- Relaterade dokument: `docs/modules/media.md`, `docs/STATUS.md`, ADR 0011.

### BB-015 – Design system v1 – manuell visuell och Tizen-verifiering

- Modul: BigBrain Web / Media
- Typ: Verifiering
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-04
- Beskrivning: Kör den dokumenterade visuella kontrollen av samtliga BigBrain-teman och verifiera separat, efter uttryckligt installationsgodkännande för aktuell adaptervariant, om serverbaserad Jellyfin Custom CSS påverkar den verkliga Samsung Tizen-klienten. Obsidian Gold är deployat i BigBrain Web men ännu inte mänskligt visuellt godkänt; dess separata Jellyfin-adapter är inte installerad.
- Avgränsning: Ingen automatisk Jellyfin-publicering, klientfork eller TV-patch. Custom CSS säkerhetskopieras och installeras endast manuellt efter separat godkännande.
- Definition of Done: BigBrains mörka, ljusa och Obsidian Gold-teman är manuellt verifierade vid 320 px, mobil, desktop, tangentbord och 200 % text; aktuell Jellyfin-variant är separat installerad efter backup och Jellyfin Web desktop/mobile är visuellt verifierat; verklig Tizen-effekt och fungerande selectors är dokumenterade eller uttryckligen klassade som ej stödda.
- Relaterade dokument: `docs/design-system/manual-verification.md`, `themes/jellyfin/compatibility.md`, ADR 0012.

### BB-027 – Dashboardprofiler, synkronisering och avancerade widgetlayouter

- Modul: BigBrain Web / Dashboard
- Typ: Framtida arkitektur och funktion
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-04
- Beskrivning: Utred nästa dashboardfas med per-user och delade dashboards, mallar, profiler, rollbaserade layouter, användarvalda widgetstorlekar, verkställda widgetbehörigheter och serversynkronisering. Phase 1 är avsiktligt lokal och enhetsbunden.
- Avgränsning: Ingen backendpersistens, identitetsmodell, synkkonfliktlösning eller behörighetsmotor införs innan separata kontrakt och faktisk användarmodell finns.
- Definition of Done: Godkänd ägarskaps- och identitetsmodell, versionssatt synkkontrakt, konflikt- och migreringsstrategi, behörighetstester, tillgänglig storleksredigering, offlinebeteende, säkerhetsgranskning och manuell fleranvändarverifiering.
- Relaterade dokument: `docs/architecture/dashboard-widget-framework.md`, ADR 0014.
