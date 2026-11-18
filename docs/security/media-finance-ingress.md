# Media → Finance: ingressgräns (BigBrain #101)

Status: **REVIEW CHECKPOINT / ej merge eller deployment**. Risknivå 3. Ägarmandat finns för en avgränsad korrigering men inte för att kringgå säkerhetsgrindar.

## Observerat 2026-11-18

- En Media-container kunde anropa ett läsbaserat Finance-endpoint direkt och via publicerad Web/Nginx-port; HTTP 200 i båda fallen. Ingen data skrevs.
- Docker host/NAT kan göra att Media och legitima webbläsare ses med samma käll-IP. **IP-blockering är inte autentisering**.
- API behöver Media-nätet för utgående adapteranrop; direkt bortkoppling skulle bryta åtminstone en icke-hostpublicerad tjänst.

## Kandidat

- API-guard nekar Finance GET-rutter från konfigurerade källsubnät oavsett förfalskade proxyheaders.
- Web/Nginx blockerar Finance via den host-publikationsväg där Media-källan observerats som Docker gateway.
- API:s publicerade hostport begränsas till localhost; intern Web → API och API → Media behålls.
- Miljöberoende Docker-adresser ska tillföras **lokalt vid drift**, aldrig checkas in: `MEDIA_FINANCE_DENIED_CIDRS` (API) och `MEDIA_FINANCE_DENIED_GATEWAY` (Nginx). Nginx-mallen använder filtrerad `envsubst`. API-guard fail-closed om CIDR-konfiguration sätts till tom sträng. Utebliven gateway kan göra Nginx-konfigurationen ogiltig och måste fångas i preflight.

## Kontrollgräns

Den här kandidatens käll-IP-spärr är ett **begränsat riskreducerande lager**. Host-NAT kan dessutom ge felaktiga 403 för legitima klienter. Den ersätter inte verifierad användaridentitet/auktorisering vid Finance och stoppar inte klienter som kan nå Finance via en annan betrodd mellanhand. Den ger inte isolation av andra API-rutter och ger ingen ny Finance- eller tradingauktoritet.

**NO-GO för produktionsdrift tills:** runtime-gateway och CIDR-värden verifierats; legitima användares Finance fungerar; Media → Finance nekas på direkt API, hostport och Web via samtliga verifierade vägar; Media-adaptrar och Web-health behålls; rollback finns; faktisk separat nivå-3-granskning av exakt SHA/tree, CI och main sker. Om tester visar att IP-skiktet är otillräckligt måste Finance få en autentiserad gräns i stället för ett osäkert specialundantag.
