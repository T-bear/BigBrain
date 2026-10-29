# Granskarbeteende

Bindande detaljer till [AGENTS.md](../../AGENTS.md); avsnittsnummer i den bevarade texten avser AGENTS.md.

P0–P3, P1-definitionen, tvåvarvsregeln och deploymentuppföljningen ligger kvar i [AGENTS §7](../../AGENTS.md#7-review-och-blockerare).

## 7. Granskarbeteende

Högst två avgränsade automatiska reparationsförsök per godkänt uppdrag. Efter policyns uttryckliga
ikraftträdande ska Codex, om felet kvarstår efter högst två försök, genomföra säker rollback
enligt befintligt driftkontrakt om den kan verifieras utan dataförlust eller brott mot bindande
skydd. Kan säker rollback inte styrkas: stoppa, bevara tillståndet och rapportera hindret.
Redovisa försök, evidens, rollbackutfall och kvarstående kontroller; starta ingen ny reparationsloop.
Före ikraftträdandet gäller befintlig auktorisering; kvarstår felet efter två försök: stoppa och
invänta ägarens beslut om fortsättningen. Regeln ger inget nytt drifttillstånd genom publicering.
Ett nytt reviewvarv nollställer inte räknaren. Taket ger inte tillstånd till blinda driftomförsök,
kringgående av säkerhetsgrindar eller att kalla ej genomförd verifiering godkänd.
P2/P3 öppnar inte automatiskt nya Codex-iterationer. Vid uppfylld DoD och riskmotiverad verifiering
ska arkitekten normalt rekommendera godkännande/merge. Ingen sen arkitekturomprövning utan
konkret blockerande evidens eller uttrycklig ägarbegäran. Review ändrar inte scope automatiskt.

För längre, osäkra eller kostsamma uppdrag kan ägaren sätta en kredit- eller resursram.
När ramen nås rapporterar agenten genomfört arbete, kvarstående obligatoriska kontroller,
uppskattad återstående kostnad (med osäkerheter) och handlingsalternativ. Ägaren beslutar om
fortsatt resursåtgång. Resursramen får aldrig användas för att beskriva ej genomförd obligatorisk
verifiering som godkänd eller för att kringgå säkerhetskrav.

När ägaren skriver **”Codex är klar”** ska ChatGPT själv hämta exakt checkpoint-SHA och tree från
GitHub, jämföra med accepterad main-baseline och läsa relevant diff, rapport, evidens, status,
backlog och testunderlag. Det är en oberoende review-trigger, inte färdigbevis. Ge ägaren en kort
bedömning av levererat/återstående, risker, manuella kontroller och rekommenderat nästa steg.
Ägaren ska inte behöva kopiera terminalhistorik. UX-acceptans kräver fortfarande uttryckligt beslut.
