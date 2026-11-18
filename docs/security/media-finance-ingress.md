# Finance HTTP-åtkomstgräns – BigBrain #101

Status: **kandidat, ej merge eller deployment**. Risknivå 3. Beslut: ersätt den tidigare osäkra IP-identifieringen i PR #103 med serververifierad Finance-auktorisering.

## Hot och gräns

Skrivskyddade kontroller 2026-11-18 bekräftade att Media kunde nå Finance-API över både det gemensamma Docker-nätverket och via Web-proxyn (HTTP 200). NAT ger dessutom i vissa fall samma synliga klient-IP för Media och legitima webbläsare. En policy med bara källadress skulle därför antingen blockera riktiga användare eller tillåta otillåtna klienter.

Nya kandidaten kräver ett separat, oförutsägbart 256-bitars åtkomsttoken för **alla** `/api/v1/modules/finance`-anrop. Servern jämför en SHA-256-hash i konstant tid och svarar HTTP 401 om en kod saknas eller är fel. Inget tillstånd läses från `X-Forwarded-For`, User-Agent, Docker-NAT-adress eller klientstyrd URL. HTTP-cache stängs av. API:s direkta hostport binds endast på loopback; dess interna nät för Media-adaptrar kvarstår. Web/Nginx skickar vidare `Authorization` till samma API-guard utan att själv utgöra en särskild bypass. Media har inte åtkomst till token via appens publika kod.

Frontend har en enkel upplåsningspanel för Finance. Koden hålls per flik i sessionStorage och bifogas enbart till Finance-API-anrop. Den lagras inte i permanent localStorage eller källkod. Home-widgeten kan visa Finance som tillfälligt otillgänglig tills samma flik låsts upp. Låsning tömmer aktuell kod. Tidigare cachad Finance-data visas aldrig utanför den låsta Finance-vyn.

## Nödvändig driftskonfiguration

- En unik 64 tecken lång kryptografiskt slumpgenererad hexkod ska lagras i **serverns befintliga Git-ignorerade `.env` (mode 0600)** som `BIGBRAIN_FINANCE_ACCESS_TOKEN`. Värdet får inte publiceras i Git, terminalhistorik, CI, kommentarer eller loggar.
- Compose sätter `Security__FinanceAccessRequired=true`, vilket gör att API:t **vägrar starta** om token är tomt eller för svagt. `docker compose config` validerar syntax, inte att hemligheten är säker.
- Finance-koden ska endast matas in via betrodd BigBrain-adress över skyddad förbindelse (t.ex. befintlig Tailscale WireGuard-länk); HTTP på okrypterat och opålitligt LAN kan röja Bearer-koden.
- Uppdatera inte API, Web eller användarens data förrän exakt SHA/tree, CI, separat risk-3-granskning, runtime-förkontroller och säker återställning är verifierade.

## Obligatoriska verifieringar

1. API-enhets- och integrationstest: utan token/med fel token = 401; med giltig token = 200; vanlig systemhälsa fortfarande 200.
2. Negativa verkliga nätverksprov från Media: via delad Docker-IP/DNS, localhost/publicerad hostport och via Web/Nginx, inklusive alternativa publicerade adresser: samtliga Finance-anrop utan token får 401 eller nekad TCP-anslutning.
3. Positiv Finance-vy från avsedd klient med giltig kod; Web/Media får fungera; inga credentials exponeras av applikationsloggar.
4. Vid deploy av API med oförändrade volymer: inga migreringar, inga ändringar i Finance-data; rollback ska använda tidigare image/Compose medan andra containrar lämnas orörda.
5. Verifiera main-CI på exakt merge-SHA innan drift samt faktisk runtime efter drift. Dokumentera endast sanerade resultat.

Den nya gränsen skyddar Finance HTTP-rutterna mot Media utan token. Den är **inte** ett fullständigt systemomfattande IAM, och blockerar inte andra API-moduler; XSS, komprometterad betrodd klient, stulna nycklar, okrypterad transport och backend-kodens Finance-volymbehörigheter är separata risker. Ingen ny trading- eller marknadsdatabehörighet följer av detta.
