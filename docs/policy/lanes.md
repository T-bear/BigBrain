# Spår: Web-only (förslag, ej ikraftträtt)

Detta är ett T2/T7-granskningsunderlag, inte en aktivering. Gällande policy
fortsätter styra tills ägarens uttryckliga ersättning har accepterats i main.
T1–T8 och deras framtida sökvägslistor får bara ändras av produktägaren.

## STANDARD — Web-only

Endast UI-/teständringar i tillåtna filer under `src/BigBrain.Web/src/`,
`src/BigBrain.Web/public/icons/`, `src/BigBrain.Web/public/manifest.webmanifest`
och `src/BigBrain.Web/index.html`. All annan diff: `STRIKT`.
API-klienter, API-kontrakt, Dockerfile, nginx, Compose, databas, rättigheter,
nätverk och funktioner som kräver nytt API ingår inte. Sökvägstestet kan inte
i sig bevisa API-kompatibilitet; nya API-behov får aldrig döljas som UI-ändring.

Efter grön PR-CI kör ägaren ett enda kommando:
`scripts/deploy-web-only.sh <PR-nummer>`. Kommandot är godkännandet.
Det kontrollerar diff och Compose före merge, bygger en kandidat medan gamla
Web kör, mergar via `gh`, inväntar grön main-CI och byter enbart Web.
Samma kommando kan återupptas om PR är mergad men CI dröjde.
Föregående image sparas som `bigbrain-sprint1-web:previous` och återställs
vid misslyckad start/hälsa. Kontrollera HTTP `/` och
`/icons/bigbrain-32.png`. Git main backas inte automatiskt vid rollback.
Ägarens iPhone/PWA-acceptans sker efter deployment. PR-beskrivningen räcker;
ingen separat granskningsrapport för kommande Web-only-leveranser.
