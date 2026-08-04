# BigBrain Status

## Projektstatus

- Version: `0.1.0-alpha`
- Senaste uppdatering: 2026-08-04
- Senaste commit före denna sprint: `4c020b5`
- Aktiv branch: `main`
- Senaste verifierade build: 2026-08-02, backend Release, BigBrain API-/Web-images och frontend production build OK

---

## Nuvarande sprint

Kort sammanfattning:

- Mål: införa den nya, uttryckligen beställda modulen Inköpslista som ett smalt permanent och mobilvänligt vertikalt snitt.
- Definition of Done: aktiv lista, säkra dubblettflöden, lokala förslag och historik, handlingsläge, deterministisk inlärd butiksordning, separat SQLite-volym samt grön test-, runtime- och browserverifiering.
- Resultat: klart och verifierat. Den generella UX-feature-freezen är fortsatt oförändrad för övriga flöden.

---

## Implementerat

- Modulärt ASP.NET Core-API och React/Vite-dashboard.
- Modulregister och versionssatta API:er för System, Docker och Media.
- Sentinel som separat process med autentiserat lokalt protokoll över Unix-socket.
- `Host.ReadUptime@1`, `Host.ReadCpu@1`, `Host.ReadMemory@1` och `Host.ReadDisk@1`.
- System Dashboard med uptime, CPU, minne och allowlistade diskar.
- Media Dashboard för Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent.
- Media Search, extern film-/seriesökning och säker posterproxy.
- Media Jobs med köer, filter, detaljvy, SSE/polling och verifierad Jellyfin-uppspelning.
- Smart Shuffle MVP med rättvis viktad shuffle, user-data-baserat episodval,
  bakgrundskoordinator, versionssatt API och mobilanpassat frontendflöde.
- Download Control MVP med säker qBittorrent-listning, kortlivade opaka ID:n,
  filbevarande standardborttagning och separat riskgrindad destruktiv bekräftelse.
- Förhandsgranskade och explicit bekräftade Sonarr-/Radarr-begäranden med kortlivad token och idempotens.
- Service Overview, media health score och regelbaserade insights.
- Prioriterad dashboard med Media Search och snabbval före detaljerad system- och Dockertelemetri.
- Minimerbara dashboardmoduler med tillgängliga kontroller och versionssatt lokal layout-persistence.
- Docker-, API- och Web-images med health checks för API och Web.
- FlareSolverr `3.5.0` som Compose-tjänst med healthcheck, konfigurerbar hostport/loggnivå och anslutning till medianätverket.
- Prowlarr indexer proxy `FlareSolverr` på `http://flaresolverr:8191/`, avgränsad med matchande tagg till Torrent[CORE].
- `meal-planner` med UI-namnet Matlista: maträtter, taggar, deterministisk generering, automatiskt och manuellt byte, permanenta matsedlar samt valbar browserutskrift.
- `shopping-list` med UI-namnet Inköpslista: en permanent aktiv lista, mängd, redigering, borttagning med bekräftelse, köp/återställning, lokal autocomplete, ofta köpt, inköpssessioner och inlärd butiksordning.

### Publicerade media- och temafunktioner 2026-08-04

- **Smart Shuffle:** implementerad och publicerad. En verklig UI-styrd start på Samsung
  Tizen gav rätt `NowPlayingItem`; även användarstyrt hopp verifierades. Naturlig
  avsnittsövergång, stopplivscykel och övrig härdning ligger kvar i BB-014 och BB-018.
  Se [Mediamodulen](modules/media.md), [ADR 0011](adr/0011-smart-shuffle-jellyfin-remote-playback-boundary.md)
  och rapporterna under `/home/enigma/BigBrain/reports/features/smart-shuffle/`.
- **Download Control:** implementerad och deployad i API/Web. Listning, opaka ID:n,
  preview/bekräftelse och filbevarande borttagning är automatiskt verifierade. Användaren
  har genom BigBrains UI bekräftat minst en borttagning av ett fastnat jobb från
  qBittorrent med filerna bevarade. Destruktiv `deleteFiles=true` är inte fullständigt
  manuellt produktionsverifierad; BB-019 är därför fortsatt Pågår och BB-020–BB-026
  beskriver härdning och framtida funktioner. Se [runbook](operations/runbooks/download-control-safe-removal.md),
  [ADR 0013](adr/0013-safe-qbittorrent-download-removal-boundary.md),
  [qBittorrent-kunskap](knowledge/qbittorrent.md) och rapporten under
  `/home/enigma/BigBrain/reports/features/download-control/`.
- **Obsidian Gold:** `bigbrain-obsidian-gold` är implementerat, automatiskt verifierat
  och deployat i BigBrain Web. Deployat UI bekräftade tema-ID, svensk etikett,
  localStorage-persistens och säker omladdning; mänsklig visuell bedömning återstår.
  Den separata Jellyfin-adaptern är skapad men har inte installerats som Custom CSS och
  är inte Tizen-verifierad. Se [temakontraktet](design-system/theme-contract-v1.md),
  [manuell kontrollplan](design-system/manual-verification.md) och rapporterna under
  `/home/enigma/BigBrain/reports/features/design-system/`.

Publiceringen ändrade inga mediafiler eller externa tjänsters konfiguration. Jellyfin
Custom CSS för Obsidian Gold är fortsatt oinstallerad.

### Smart Shuffle MVP

Status: IMPLEMENTERAD – P1-startflödet verifierat på Samsung Tizen-TV 2026-08-04.

- Backend, frontend, viktad algoritm med anti-repeat och starvation-skydd,
  bakgrundskoordinator samt automatiska tester är implementerade.
- Jellyfin Server 10.11.11 och exakt en fjärrstyrbar Samsung Smart TV-session via
  Jellyfin for Tizen har verifierats read-only med `SupportsRemoteControl=true`.
- `Media:Jellyfin:UserId` är bundet från säker lokal runtimekonfiguration i den
  Git-ignorerade `.env`-filen; värdet exponeras inte i Git, API eller frontend.
- P1-felet i startflödet berodde på att den sekventiella episodkontrollen överskred den
  generella tresekunderstimeouten innan PlayNow. Parallell kontroll, avgränsad timeout,
  sessionsrevalidering och Tizen-anpassad bekräftelsemodell är nu implementerade.
- Ett uttryckligt knapptryck i BigBrain skickade exakt ett kommando, Jellyfin svarade
  `204`, rätt `NowPlayingItem` bekräftades och sessionen blev aktiv. Användaren bekräftade
  TV-resultatet; användarstyrda hopp verifierades också. Ingen uppspelning startades från
  terminal eller test. Automatisk övergång vid naturligt avsnittsslut återstår enligt BB-014.
- Se [Mediamodulen](modules/media.md), [verifieringsrunbook](operations/runbooks/media-integration-verification.md),
  [Proposed ADR 0011](adr/0011-smart-shuffle-jellyfin-remote-playback-boundary.md) och den
  externa rapportserien under `/home/enigma/BigBrain/reports/features/smart-shuffle/`.

### Inköpslista – första vertikala versionen

Status: Implementerad och runtimeverifierad 2026-08-02

- Dashboarden visar modulen som en sammanhållen enhet. Minimerat läge visar antal kvar, direkt `+ Lägg till vara`, högst tre första oköpta varor och ett tydligt tomläge; normal `Available`-status visas inte.
- Handlingsläget är en egen fixed appyta utan Fullscreen API. Dashboardroten blir inert, bottennavigation och FAB döljs, bakgrundsscroll låses, safe-area respekteras och fokus fångas säkert samt återgår vid `Tillbaka` eller Escape.
- Aktiv lista: namn 1–120 tecken, antal 1–999, stora checkboxmål, redigering, bekräftad borttagning, separat minimerbar `Köpta`-sektion och återställning utan automatisk radering.
- Dubbletter jämförs efter trimning, hopslagning av upprepade mellanslag, Unicode-normalisering och svensk skiftlägesokänslig normalisering. UI kontrollerar före POST; API och ett partiellt unikt SQLite-index försvarar samma invariant. Oköpt dubblett erbjuder `Öka antal`, `Visa varan` och `Avbryt`; köpt dubblett erbjuder återaktivering.
- Autocomplete prioriterar faktisk lokal historik/frekvens/senaste köp och kompletterar med en liten statisk svensk grundlista. Grundlistan skrivs inte till historiken innan användaren faktiskt lägger till varan. Listboxen kan styras med tangentbord och stängas med Escape.
- `Ofta köpt` rankas deterministiskt efter antal avslutade köp, senaste köptid och namn. Antal tillägg lagras separat för lokal historik/förslag. Inga tekniska poäng visas.
- En session skapas när första varan bockas av och lagrar normaliserad identitet, UTC-tid och avbockningsposition. `Avsluta inköp` kräver bekräftelse, arkiverar köpta varor, avslutar sessionen och låter användaren uttryckligen behålla eller ta bort oköpta varor; historiken raderas inte.
- Inlärd ordning: varje avslutad session bidrar med observerad position. Senaste nya observationen väger 1,15 mot äldre genomsnitt. Historisk position används först efter två observationer; annars används stabil skapelse-/ordinalordning, skapad tid och namn. Sortering läses vid öppning/laddning och tillägg; vid avbockning flyttas bara varan till `Köpta`, så återstående rader hoppar inte runt.
- Ingen extern AI, extern produktdatabas eller internetberoende används. Modellen är lokal, deterministisk och förklarbar.
- Persistens: separat SQLite-schema version 1 med tabellerna `Items`, `Sessions`, `CheckEvents`, `ItemStats` och `SchemaInfo`, idempotent initiering, parameteriserad SQL, UTC-tider och named Compose-volym `shopping-list-data` på `/shopping-data`. Databasen är inte Git-spårad. En API-instans stöds i v1; flerinstansdrift kräver ett separat arkitekturbeslut.
- API: `GET/POST /items`, `PUT/DELETE /items/{id}`, `POST /items/{id}/purchase|restore|increase|reactivate`, `GET /suggestions`, `GET /frequent` och `POST /finish` under `/api/v1/modules/shopping-list`. Validering ger 400, saknad vara 404, dubblett 409 och lagringsfel 503 som Problem Details med stabil kod.
- Backend: hela lösningen byggd i Release och 192 tester gröna (160 API + 32 Sentinel). Frontend: 62 tester i 10 filer gröna och production build grön. `git diff --check` och `docker compose config --quiet` gröna.
- Compose/runtime: API- och Web-images byggda; endast API och Web återskapades. Båda är healthy. Inköpslista rapporterar `Available`, den permanenta listan är tom, grundlisteförslag fungerar och Matlista, System och samtliga fem mediaproviders fortsätter fungera.
- Browser: isolerad Chromium-mock vid 390 × 844 och 1440 × 1000 med 50 varor, långt namn, åtta köpta, ofta köpt och historik. Mobil gav viewport/dokumentbredd 390/390, 16 px input, 48 × 48 checkboxmål, förslagspanel 366 px inom viewporten, dold bottennavigation, inert bakgrund, korrekt Escape/fokus och inga konsolfel. Desktop gav 1440/1440 och samma gröna handlingsflöden.
- Kända begränsningar: en aktiv lista, en butiksgemensam ordningsmodell, ingen backup/restore ännu och ingen flerinstanssamordning. Nästa rekommenderade lilla mål är backup/restore för den separata moduldatabasen.
- Framtidsplaner, inte implementerade: recept-/ingredienskoppling från Matlista och automatisk generering från matsedel; enheter som kg, liter och paket; manuella butikskategorier, egen avdelningsordning, olika ordning per butik och drag-and-drop; priser/budget; streckkodsläsning; skafferi/lager; delning/realtidssynk; användarprofiler; molnsynk; röstinmatning; AI-baserade men alltid godkända förslag; notiser om återkommande behov.

### Matlista – första vertikala versionen

Status: Implementerad och runtimeverifierad 2026-08-01

- Familjeschemat är en ren tvåveckorsfunktion förankrad i `2026-08-03`, måndag i Vecka A. Cykeln returnerar 4, 6 respektive 3 personer enligt det beslutade dagsschemat och fungerar även för datum före ankaret.
- Maträtter kan skapas, redigeras, tas bort, ha noll eller flera taggar och filtreras med en eller flera taggar.
- Sex skyddade standardtaggar skapas reproducerbart: `3–4 personer`, `6 personer`, `Fredagsmat`, `Lättlagat`, `Helgmat` och `Lunch`. `Lunch` har den stabila kategorin `mealType`, visas i hantering/filter/redigering och kan inte tas bort. Egna taggar kan skapas och tas bort; borttagning kopplar säkert loss taggen från maträtter.
- Varje planerad post har den stabila måltidstypen `lunch` eller `dinner`. Generatorn skapar sju middagar och två helgluncher, totalt nio måltider per full vecka. Lunch-taggen prioriteras för helglunch men är inte obligatorisk eller exklusiv; portionstagsregler, otaggad fallback, middagens dagstagsprioritering och seedad injicerbar randomisering bevaras.
- Automatiskt byte och manuellt val adresserar `schedule + datum + måltidstyp`, ändrar endast den valda måltiden och behåller datum och personantal. Äldre bytes-URL:er finns kvar kompatibelt och avser middag.
- Matlista använder dashboardens befintliga minimering. Minimerat vardagsläge visar dagens middag; på lördag och söndag visas både lunch och middag med samma personantal. Om dagens datum saknas visas närmast relevanta matsedelsdag; utan matsedel visas ett tydligt tomläge.
- Öppnat läge har fyra lokala, tillgängliga arbetslägen: `Matsedel`, `Maträtter`, `Generera` och `Sparade`. Endast valt arbetsläge renderas synligt och `Matsedel` är standard. Tagghantering ligger stängd inne i `Maträtter`.
- Veckovyn visar endast en vecka, två små gränsmedvetna navigeringsknappar och sju kompakta dagsrader. Vardagar har en middagspost; helgkort har separata kompakta lunch- och middagsposter med varsin byteskontroll. Permanent maträttsväljare är borttagen och endast vald måltids inline-yta öppnas.
- Maträttsbiblioteket har kombinerbar textsökning och flertaggsfilter, träffantal, kompakta rader och åtgärdsmenyer som skalar till längre listor utan paginering.
- Generering återgår direkt till den nya aktiva matsedeln med bekräftelse. Sparade matsedlar visas som kompakta poster och kan öppnas, väljas för utskrift eller tas bort med bekräftelse.
- `POST /api/v1/modules/meal-planner/meals/seed-examples` lägger idempotent till 24 varierade exempelrätter utan att ändra eller ta bort användarrätter och utan att generera matsedlar. UI-åtgärden visas i tomt maträttsbibliotek och kräver bekräftelse.
- Sparade matsedlar kan fortsatt väljas för en ren A4-anpassad browserutskrift.
- Persistens: en modullokal SQLite-databas med schemaversion 2 och namngiven Compose-volym. Den versionssatta, idempotenta migreringen från schema 1 lägger till `mealType=dinner` på äldre planerade poster utan att ändra eller radera matsedlar eller maträtter. Lösningen valdes för minsta permanenta datalager utan att införa en generell dataplattform eller ändra andra modulers lagring.
- Modulstatus är oberoende av mediatjänster. Om SQLite-filen inte kan öppnas registreras Matlista som `Unavailable`; övriga moduler påverkas inte.

Kvarvarande begränsningar: SQLite-lösningen är avsiktligt enkel och förutsätter en API-instans. Backup/restore, fleranvändarkonflikter, generell autentisering/auktorisering och persistent audit är inte lösta. Raderingar kräver bekräftelse i UI och loggas strukturerat av API:t, men projektet saknar ännu identitetsbunden audit.

### KISS-fokuserad familje-UX

Status: Implementerad och verifierad 2026-08-01

- Permanent UX-princip: `BigBrain ska utgå från vad användaren vill göra, inte från hur de underliggande systemen är byggda. Standardvyn visar handling, resultat och nödvändig återkoppling. Teknisk konfiguration och diagnostik visas först på uttrycklig begäran eller i Administration.`
- Startsidan prioriterar Matlista, mediesökning och Pågående. Server-, Docker-, Sentinel-, provider-, health-, versions- och diagnostikinformation ligger i en tillgänglig `Administration`-sektion som är stängd som standard men öppnas direkt från mobilens Admin-ingång. Verkliga funktionsfel visas fortfarande kort i berört vardagsflöde.
- Normala `Available`-/`Healthy`-etiketter är borttagna från vanlig navigation och Matlistas rubrik. Avvikande modulstatus visas fortsatt.
- Mediesökfältet ligger före valet `Jag söker`. Resultatrubrikerna är `Serier` och `Filmer`; exakt titelmatch rankas först, bara bästa träffen per kategori visas initialt och fler resultat kan visas, döljas eller rensas lokalt.
- Lägg-till-flödet behåller befintlig preview/confirm och idempotens. Rekommenderade val används i standardsteget, `Börja söka efter filer direkt` är på från början men kan ändras under stängda `Avancerade inställningar`, och tekniska värden ligger under stängda `Tekniska detaljer` i bekräftelsen.
- Pågående mediakort visar användartitel, mediatyp, svensk status, progress, procent och tillgänglig återstående tid. Provider, fullständig release-/torrenttitel och interna statusvärden är fortsatt åtkomliga under `Visa tekniska detaljer`; filtren heter `Pågår`, `Bearbetas`, `Klara`, `Problem` och `Alla`.
- Matlistas maträttsbibliotek har en kompakt `+ Lägg till`-åtgärd och en grupperad filterpanel för Måltid, Antal personer, Tillfälle och Övrigt. Panelen har valt antal, `Rensa filter`, tydlig stängning och Escape-stöd; textsökning och taggfilter kombineras som tidigare.
- Alla `input`, `select` och `textarea` beräknas till minst 16 px på mobil. Viewporten tillåter fortsatt användarzoom.

### Slutlig UX-polering och feature-freeze

Status: Implementerad och verifierad 2026-08-01

- Mobilens redundanta modulnavigation i sidhuvudet är dold; BigBrain-identiteten finns kvar. Bottennavigationen når fortsatt `Hem`, `Sök`, `Pågår` och `Admin`. Desktopnavigationen finns kvar.
- Mediesökningen visar ett lokalt rensningskryss när text, resultat eller sökfel finns. Rensning avbryter pågående sökanrop och återställer endast söktext, resultat, expansion, lokalt fel, laddningsstatus och FAB-state; bibliotek, providers och jobb påverkas inte.
- Varje provider visar faktisk mängd dolda resultat som `Visa N fler träffar`; bästa träffen ligger kvar först. Expanderat läge heter `Visa färre`.
- En lokal sök-FAB visas endast på mobil när resultat finns och sökfältet har scrollats ur direkt räckhåll. Menyn erbjuder `Till sökfältet`, `Rensa sökning` och relevant fler/färre-handling, kan stängas med FAB, Escape, val eller klick utanför och ligger ovanför bottennavigationen.
- Ett aktivt mediajobb visas fortsatt direkt som befintligt jobbkort. Minst två aktiva jobb visas initialt som en kompakt, expanderbar sammanfattning med antal, normaliserad titel, status och procent. Fulla jobbkort och deras stängda tekniska detaljer är fortsatt nåbara via `Visa nedladdningar`.
- Kompakt titel normaliserar välkända releaseformat med årtal och teknisk suffixmarkör. Om API:t endast levererar ett okänt format behålls originaltiteln för att undvika felaktig trunkering; full originaltitel finns alltid i `title`-attribut och i expanderat jobbkort.
- UX är nu feature-fryst. Fortsatt UX-arbete ska baseras på ett konkret användartest, ett reproducerbart fel eller ett tydligt nytt användarbehov, inte allmän polering.

### Officiell BigBrain-ikon

Status: Implementerad och ompublicerad från ny masterkälla 2026-08-02

- Den blå–lila neonikonen med hjärna och B är BigBrains officiella huvudikon och ersätter den tidigare textbaserade B-symbolen i headern utan ändrad headerhöjd.
- `branding/bigbrain-icon-source.png` är enda masterkälla. Favicon, Apple touch icon, header-/PWA-ikonerna i `192×192` och `512×512` samt maskable-ikonen genererades om från denna bild. Den ospårade, äldre reservkopian `branding/bigbrain-icon-sourceX.png` saknade referenser och togs bort efter bild- och referenskontroll.
- Webbassets finns som PNG i `32×32`, `180×180`, `192×192` och `512×512`. En separat `512×512` maskable-version centrerar samma oförändrade motiv med extra säkerhetsmarginal på konsekvent svart bakgrund.
- Browserfliken använder `32×32`-favicon, iOS använder `180×180` Apple touch icon och webbmanifestet deklarerar `192×192` och `512×512` med `purpose: any` samt maskable-versionen med `purpose: maskable`.
- Samtliga fem bilder, manifestet och Web health svarade `200` i den ombyggda Web-containern. Chromium verifierade den nya `192×192`-bilden i oförändrade `34×34` i headern.
- Redan installerade PWA-versioner kan behålla den gamla ikonen i operativsystemets cache. På iPhone kan appen därför behöva tas bort från hemskärmen och installeras om manuellt för att den nya ikonen ska visas.

### Inköpslista – korrigeringsuppföljning

Status: Implementerad och verifierad 2026-08-02

- Efter ett lyckat tillägg töms snabbfältet och återfår fokus efter att den uppdaterade listan renderats, utan timeout eller scrollryck. Flödet fungerar med Enter och knapp. Valideringsfel lämnar ett återställbart fält, serverfel stjäl inte fokus och dubblettdialogen behåller fokus tills användaren slutför eller avbryter den.
- Varuradens tidigare radbundna meny ersattes med en lokal fixed portalmeny som högerjusteras, klampas inom viewporten och vänder uppåt när utrymmet nedåt inte räcker. Menyn har högst en öppen instans, kontrollerad radbrytning, 44 px tryckmål, utanförstängning, Escape och fokusåtergång utan autoscroll.
- Frontend: 67 tester i 10 filer gröna och production build grön. Compose-konfiguration och Web-image är gröna; endast Web återskapades och är healthy. Inga backend- eller API-filer ändrades.
- Isolerad Chromium-verifiering med 46 startvaror genomfördes vid `390×844`, `320×844` och `1440×1000`: fem följande Enter-tillägg utan nytt tryck i fältet, meny på första och nedersta rad, uppåtvänd meny vid nederkant, korrekt Escape/fokusåtergång, dold bottennavigation i handlingsläget, ingen horisontell overflow och inga konsolfel.

Framtida planer, inte implementerade i denna sprint:

- recept;
- ingredienser;
- automatisk portionsskalning;
- inköpslista;
- kostnad och budget;
- allergier;
- individuella kostpreferenser;
- favoriter och blockerade rätter;
- betyg;
- historik och regler för hur nyligen en rätt använts;
- låsta dagar;
- drag-and-drop;
- delning mellan användare;
- användarprofiler;
- molnsynk;
- export till PDF;
- koppling till kalender;
- AI-förslag baserade på befintliga rätter och preferenser.

---

## Kända problem

Öppna ärenden och framtida förbättringar dokumenteras permanent i [`docs/BACKLOG.md`](BACKLOG.md).

### Docker inventory unavailable

Status: Open

Beskrivning: Docker-endpoint och UI finns, men providern returnerar en tom lista och `Docker inventory requires Sentinel integration.` Ingen Docker-capability är implementerad.

### Jellyfin degraded

Status: Resolved 2026-08-01

Grundorsak: BigBrains Jellyfin-adapter använde `GET /Items/Latest` utan användar-ID. Jellyfin 10.11.11 svarade `400` och loggade `Guid can't be empty` från `UserLibraryController.GetLatestMedia`. De övriga fyra Jellyfin-anropen svarade `200`, men adapterns avsiktliga partial-failure-regel gjorde ett enda misslyckat delanrop till providerstatus `degraded`.

Åtgärd: adaptern använder nu det API-nyckelkompatibla `GET /Items` med rekursiv sökning, samma typfilter och explicit sortering på `DateCreated` fallande. Svaret normaliseras genom adapterns befintliga `Items`-mappning. Ingen Jellyfin-konfiguration eller data ändrades.

### Prowlarr update available

Status: Open

Beskrivning: Prowlarr är online men rapporterar att version `2.5.2.5491` finns tillgänglig.

---

## Senaste verifiering

Datum: 2026-08-01

Verifierat:

- Backend build: Release OK i .NET 10 SDK-container, 0 varningar och 0 fel.
- Backend tester: 186 OK, 0 failed, 0 skipped (154 API/modul + 32 Sentinel), inklusive 28 fokuserade Matlista-testfall för familjeschema, nio måltider, lunchurval, exakt byte, schema 1→2, CRUD, exempeldata och API-fel.
- Frontend tester: 59 OK i 9 testfiler. Utöver tidigare Matlista-, mediarequest-, Administration- och mobilregression täcks dynamiskt `N`-antal, rensningskryss, villkorad FAB inklusive tomt resultatsvar, FAB-meny/fokus/rensning/fler-färre samt ett kontra flera aktiva jobb och kompaktvyns expansion.
- Frontend production build: OK.
- Branding: samtliga fem PNG-filer och `manifest.webmanifest` svarade `200` från Web. Manifestets ikonposter, favicon, Apple touch icon och headerreferens verifierades i körande app.
- Branding browser: riktig Chromium vid 390 × 844 och 1440 × 1000 gav dokumentbredd lika med viewport, inga konsolfel och en skarp `192×192` headerbild renderad i oförändrade `34×34`; brandradens höjd var fortsatt 34 px.
- Matlista runtime: modulen rapporterar `Available`; schema 1 migrerades automatiskt till schema 2. Den befintliga matsedelns 14 poster och användarrätter bevarades, samtliga äldre poster läses som `dinner`, och den sjätte skyddade standardtaggen `Lunch` finns genom både direkt API och frontendproxy.
- Matlista mobil: verifierad i riktig Chromium vid 390 × 844 med isolerad mockdata; dokumentbredd och viewport är båda 390 px. Minimerat helgläge visar lunch och middag, veckovyn har sju dagsrader, lördagens kompakta dubbelpost är 111 px hög och endast lunchens bytesyta öppnades (`dinner` förblev stängd).
- Matlista bibliotek: browserflödet lade 24 exempelrätter i en isolerad mock, kombinerade textsökning med `Lättlagat` och visade `1 av 24 maträtter` utan runtime-mutation.
- Matlista utskrift: Chromium print-media visar nio måltidsrader för en full vald vecka inklusive måltidstyp; dashboardnavigationen är dold och CSS behåller A4-layout och sidbrytning per vecka.
- KISS UX browser: riktig Chromium vid 390 × 844 gav viewport/dokumentbredd `390/390`, visuell skala `1` före och efter fokus samt beräknad fältstorlek `16px`. Filterpanelen låg inom viewporten (`366px` bred), dialogen låg inom viewporten (`366px` bred), Administration var stängd initialt och öppnades via Admin, bästa medieträffen var ensam initialt och fler träffar kunde visas.
- Desktop browser: riktig Chromium vid 1440 × 1000 gav viewport/dokumentbredd `1440/1440`; Administration var stängd initialt.
- UX-freeze mobil: riktig Chromium vid 390 × 844 gav viewport/dokumentbredd `390/390`; headernavigationen var dold och bottennavigationens fyra ankare var nåbara. FAB låg på `y=716..768`, bottennavigationen började på `y=781`, och menyn låg helt inom viewporten. `Visa 2 fler träffar` expanderade till tre kort, sökfokus återställdes och lokal rensning gav tom text och noll resultat.
- UX-freeze jobb: två verkliga read-only runtimejobb visades kompakt som `Mamma Mia (2008)` och `Mamma Mia Here We Go Again (2018)` med status/procent; inga fulla kort syntes före expansion, två syntes efter expansion och tekniska detaljer var stängda.
- UX-freeze desktop: riktig Chromium vid 1440 × 1000 gav viewport/dokumentbredd `1440/1440`; desktopnavigationen var synlig, bottennavigationen dold och ingen FAB renderades utan relevant sökkontext.
- Matlista kompaktvy: isolerad browserdata visade lördagens lunch och middag i minimerat läge; helgens dubbelrad var `103px` hög. Print-media visade nio rader och dold mobilnavigation.
- Jellyfin direkt: `/health` svarade `200 Healthy`; `System/Info` svarade med version `10.11.11`; den nya `/Items`-frågan svarade `200` med åtta sorterade poster.
- BigBrain API: `/api/v1/modules/media` rapporterade Jellyfin `online`, åtta nyligen tillagda poster och Media overall `online`.
- Frontendproxy: samma mediaendpoint via Web rapporterade Jellyfin och Media overall `online`.
- Providerregression: Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent rapporterade samtliga `online`; provideranropen svarade `200`.
- Mobil verifiering: 2026-08-01, 390 × 844 utan horisontell scroll; filterpanel, mediedialog, bottennavigation, minimerad/expanderad Matlista och utskriftsläge verifierade.
- Compose build: Web-imagen byggdes om och endast Web återskapades för den frontend-only slutpoleringen; API och Web startade healthy.
- Compose-konfiguration: verifierad 2026-08-01 med `docker compose config --quiet`.
- Runtime: Sentinel, API, Web och FlareSolverr kör; API, Web och FlareSolverr är healthy.
- FlareSolverr: `/health` svarar på hostport 8191 och från Prowlarr via `http://flaresolverr:8191/health`.
- Prowlarr: FlareSolverr-proxytest och `Test All Indexers` OK; fem av fem indexers giltiga.
- Torrent[CORE]: Cloudflare-challenge upptäckt och löst via FlareSolverr med efterföljande `200 OK`.
- API: health, System, Docker och Media svarar.
- Frontend: svarar och renderar BigBrain-applikationen.
- Regression: befintliga Jellyfin-, Sonarr-, Radarr-, Prowlarr- och qBittorrent-containrar är fortsatt running; Jellyfin är container-healthy.

---

## Runtime

- System: Healthy; uptime, CPU, minne och `BigBrain Storage` verifierade.
- Media: Online; Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent online. Health score är `68`/`actionRecommended` på grund av separata provider-healthvarningar, inte providerstatus.
- Docker: Unavailable i BigBrain; inventory-capability saknas.
- Matlista: Available; SQLite schema 2 öppet via namngiven Compose-volym, sex skyddade standardtaggar och migrerade äldre middagsposter verifierade.
- Inköpslista: Available; separat SQLite schema 1 öppet via namngiven Compose-volym och tom permanent aktiv lista verifierad.
- Sentinel: Running.
- API: Healthy.
- Web: Healthy.
- FlareSolverr: Healthy; nåbar från Prowlarr på det externa medianätverket.
- Prowlarr: Online; FlareSolverr-proxy aktiv för Torrent[CORE].
- Senast verifierad: 2026-08-02.

---

## Nästa sprint

UX är feature-fryst. Nästa UX-mål ska endast öppnas av konkret användartest, reproducerbart fel eller tydligt nytt användarbehov. För Matlista är nästa separata datamål fortsatt backup/restore för modulens SQLite-data.

Designsystem v1 är implementerat i källkod med mörkt/ljust tema och stabilt tokenkontrakt.
Den separata Jellyfin 10.11.11-adaptern installerades additivt 2026-08-04 och verifierades
tekniskt i Jellyfin Web utan restart eller uppspelning. Manuell visuell BigBrain- och
Samsung Tizen-verifiering återstår enligt BB-015.

Övriga verifierade öppna kandidater, utan inbördes prioritering:

1. Docker inventory via Sentinel.
2. Persistent audit och idempotens för mediabegäranden.
3. Drag-and-drop för användarstyrd modulordning.
4. PWA update-flöde med service worker och tydlig användarkontroll.

---

## Teknisk skuld

- Hostname och temperature samlas inte in.
- Docker inventory och samtliga Dockeråtgärder saknas.
- Autentisering, auktorisering, användare och generell auditlogg saknas.
- Mediabegärandetokens och idempotensresultat lagras endast i minnet.
- Browser-E2E och verkliga integrationstester mot mediatjänster saknas.
- Jellyfin-dashboardens delanropsfel loggar HTTP-status men inte ett säkert operation-ID per endpoint, vilket gjorde det misslyckade delanropet svårare att identifiera. Ingen bredare observabilityändring gjordes i denna sprint.
- Dashboardlayouten lagras endast lokalt per webbläsare och saknar användarbunden synkronisering.
- Drag-and-drop för modulordning och PWA auto-update är inte implementerade.
- Brain och Worker är inte implementerade. Matlista har en avgränsad SQLite-databas; någon generell databasplattform finns inte.
- Cloudflare-challenges kan vara intermittenta; första Torrent[CORE]-testet nådde 60 sekunders timeout, medan fyra efterföljande försök lyckades på cirka 15–16 sekunder.

---

## Senaste Codex-session

- Datum: 2026-08-01
- Syfte: separat brandingändring för BigBrains officiella huvudikon.
- Resultat: favicon, Apple touch icon, PWA-standardikoner, maskable-ikon och headerbranding använder den nya ikonen. Frontendtest, production build, Web-runtime och mobil-/desktopbrowser är verifierade utan annan UX- eller backendändring.
- Commitmeddelande: `feat(brand): add official BigBrain app icon`.
- Nästa rekommenderade steg: inga generella UX-poleringar under feature-freeze; invänta konkret användartest, reproducerbart fel eller tydligt nytt användarbehov.
