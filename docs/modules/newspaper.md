# Morgonbladet – produkt- och designkontrakt

**Beslutsdatum:** 2026-10-31.
**Ägare:** BigBrains produktägare.
**Status:** Produktinriktning och vald designreferens bekräftade.
**Implementation:** N1 är ännu inte visuellt accepterad, mergad
eller deployad. Dokumentet ger ingen exekveringsauktorisering.

Detta är den centrala produktspecifikationen för Morgonbladet.
Vid framtida uppdrag ska denna fil läsas tillsammans med aktuell
`main`, `AGENTS.md`, tillämpliga regler och berörd kod.

## 1. Produktvision

Morgonbladet är en personlig morgontidning som kan sammanställas
för olika personer i hushållet och skrivas ut på A4.

- Varje person väljer sitt eget maximala sidantal: 1–3 sidor.
- Innehållet utgår från personens valda intressen och önskemål.
- Tidningen ska vara lugn, trevlig, källkritisk och lättläst.
- Sidantalet är en faktisk utskriftsgräns, inte bara ett UI-val.
- Layouten ska vara fördefinierad och konsekvent mellan utgåvor.
- Säsongs- och högtidsteman får förändra de dekorativa detaljerna,
  inte den grundläggande sidstrukturen.

## 2. Vald designriktning – produktägarens beslut

**Klassisk morgontidning med varm, personlig akvarellkänsla.**

Den valda visuella referensen är den mer traditionella
Morgonbladet-framsidan, inte den mer magasinlika varianten.
Referensbilden valdes av produktägaren 2026-10-31.

### Visuell beskrivning av referensen

- Mycket stor, klassisk serif-vinjett med texten "Morgonbladet".
- Mörkgrön eller nästan svart typografi.
- Små botaniska akvarelldetaljer kring vinjetten, exempelvis
  kvistar, blommor och en liten fågel.
- En kompakt rad med datum, utgåva och korta notiser.
- Redaktionell spaltlayout med en tydligt dominerande huvudartikel.
- Huvudartikel med stor rubrik, ingress, längre text och stor bild.
- Mindre artiklar i olika storlekar runt huvudartikeln.
- Små sidospalter för korta, relevanta faktarutor eller notiser.
- Tunna avdelare, diskreta bildtexter och tydlig sidnumrering.
- Flera innehållsblock använder A4-ytan effektivt.
- En personlig, varm känsla utan att sidan blir plottrig.

Referensens förstasida visar bland annat en stor artikel om ett
rum av levande pil, en astronomispalt och mindre block om
retrospel och vardagsteknik. Dessa ämnen är exempel, inte
obligatoriskt innehåll i varje utgåva.

De påhittade nyhetstexterna, citaten, vädret, namnen, bilderna
och sidnumren i illustrationsreferensen är endast designexempel.
De ska inte behandlas som verkliga uppgifter eller källor.

Denna text är en beskrivning av referensbilden; själva bildfilen
är ännu inte versionshanterad i detta repository. Inga senare
implementatörer ska anta att bilden redan finns som en asset.

## 3. Pappersval och bläcksnål utskrift

Primärt mål är naturvitt eller krämvitt A4-papper.

- Papprets egen färg skapar den varma grundtonen.
- Ingen heltäckande krämfärgad eller annan färgad sidbakgrund
  får skrivas ut.
- Utskriftsbakgrunden ska vara vit eller transparent.
- Brödtext har hög kontrast och trycks helst i svart.
- Mörkgröna rubriker och tunna färgade detaljer är tillåtna.
- Färgbilder används där de faktiskt tillför innehållsvärde.
- Akvarellornament ska vara små och bläcksnåla.
- Stora mörka eller täckande färgfält ska undvikas.
- Tidningen ska fungera även på vanligt vitt kopieringspapper.

Naturvitt papper är ett önskat materialval, inte ett krav på att
skrivaren eller PDF-generatorn ska simulera pappersfärgen.

## 4. Fast layout och innehållshierarki

### Förstasidan

- Stor tidningsvinjett och datumrad.
- En tydligt utvald huvudartikel.
- Stor bild eller illustration när relevant material finns.
- Kortare artiklar och notiser i mindre spalter.
- Diskreta dekorativa detaljer som stödjer årstid och tema.
- Källhänvisningar och sidnumrering utan onödigt visuellt brus.

### Efterföljande sidor

- Mindre sidhuvud än på förstasidan.
- Samma typografi och visuella identitet.
- Fördefinierade spalter och artikelblock.
- Ingen hänvisning till sidor utanför den faktiska utgåvan.

Mallar och innehållsbudget ska hantera både korta och längre
texter utan ofrivillig klippning, tomma extrasidor eller spill.

## 5. Personliga inställningar – framtida funktioner

Varje person ska kunna ange intressen och ett maximalt sidantal.

Det ska också gå att välja positiva eller "glada" nyheter,
antingen som ett övergripande önskemål eller för utvalda
nyheter eller ämnesområden.

Positivitetsval får aldrig innebära fabricering, obalanserad
faktapresentation eller att källkritiska krav åsidosätts.

Inställningar, personprofiler och automatiserad nyhetsinhämtning
är framtida uppgifter och inte redan levererade N1-funktioner.

## 6. Källkritik och redaktionell integritet

- Nyhetsartiklar ska ha spårbara och identifierbara källor.
- Källornas aktualitet, tillförlitlighet och rättigheter ska
  bedömas innan material används.
- Fakta ska skiljas från åsikter, prognoser och illustrationer.
- Påhittade citat, lokala händelser eller väderdata får aldrig
  presenteras som verkliga nyheter.
- Fiktivt demonstrationsinnehåll ska tydligt märkas som demo.
- Bilder ska ha lämplig proveniens och användningsrätt.
- Vid osäker källa eller verifiering utelämnas påståendet.

## 7. Årstider och lekfulla illustrationer

Små akvarellillustrationer får anpassas efter exempelvis vår,
sommar, höst, vinter, halloween, jul och nyår.

Även lekfulla detaljer som små katter, oväntade djur eller
fantasifulla figurer får förekomma.

Dekorationerna får inte:
- försämra läsbarheten,
- dominera över nyheterna,
- förstöra A4-sidbudgeten,
- orsaka hög färgförbrukning,
- eller förväxlas med redaktionella fakta.

## 8. N1 – teknik, verifiering och kvarstående acceptans

N1 är en statisk Web-only-prototyp med uttryckligt märkt
demoinnehåll. Ingen verklig nyhetsinhämtning krävs i N1.

2026-10-31 verifierades i separata PDF-prov från mobilens
webbläsare att befintlig utskriftsmekanik kunde generera
exakt 1, 2 och 3 A4-sidor, med två demoartiklar på varje
sida och utan observerade extrasidor eller avklippt innehåll.

Detta verifierar enbart den dåvarande tekniska utskriften,
inte den nya designens framtida sidfördelning, utskrift
med verkliga nyhetstexter eller acceptans i installerad PWA.

Den aktuella review-implementationen finns i:
[PR #22](https://github.com/T-bear/BigBrain/pull/22).

Produktägaren har uttryckligen inte accepterat den äldre,
glest satta N1-designen. Den tekniskt fungerande
utskriftsmekaniken ska bevaras så långt det är rimligt,
medan den visuella layouten omarbetas.

### Kvarvarande acceptanskriterier

- Den nya förstasidan bedöms mot designriktningen i avsnitt 2.
- Efterföljande sidor får samma genomarbetade tidningskänsla.
- 1, 2 och 3 faktiska A4-sidor fungerar utan spill eller klipp.
- Ingen heltäckande bakgrundsfärg trycks.
- Ny typografi, bildplacering och spalter förblir lättlästa.
- Mobil/webbläsare och dator kontrolleras separat.
- Installerad BigBrain-PWA får separat manuell ägaracceptans.
- Produktägaren godkänner uttryckligen den slutliga designen.

Grön CI, publicerad PR eller fungerande PDF är inte automatiskt
visuell produktacceptans eller deploymenttillstånd.

## 9. Etapper och avgränsning

- **N1:** Statisk tidningsprototyp, design och A4-utskrift.
- **N2:** Verklig, källkritisk nyhetsinhämtning. Separat uppdrag.
- **N3:** Individanpassning, intresseval och positivt nyhetsfilter.
  Separat uppdrag.

Inget i detta dokument auktoriserar nya API:er, databaser,
kostnader, nya tjänster, merge eller deployment.

## 10. Ändringsprincip

Ändrade produktbeslut dokumenteras här eller i senare
auktoritativt modulkontrakt med tydlig hänvisning.

Särskilj alltid:
- beslutade krav,
- föreslagna lösningar,
- implementerad men ej accepterad kod,
- verifierad teknik,
- manuellt accepterad design,
- och faktiskt deployad funktion.

Projektets permanenta arbets- och säkerhetsregler gäller
oförändrat.
