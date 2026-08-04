# Jellyfin adapter compatibility v1

- Adapter version: 1.0.0.
- Selector baseline: installed Jellyfin Server/Web 10.11.11, inspected read-only 2026-08-04.
- Jellyfin Web desktop: selectors and CSS syntax automatically verified; visual/manual verification pending because CSS was not installed.
- Jellyfin Web mobile: responsive-safe rules present; manual visual verification pending.
- Jellyfin for Tizen: manual verification required on the real TV. The official client embeds Jellyfin Web, but this does not prove that this server's Custom CSS is fetched/applied by the installed TV build.
- Other native clients: unsupported/not verified unless they use Jellyfin Web and load server Custom CSS.
- Administration dashboard: server Custom CSS is intentionally not applied by Jellyfin 10.11, so it is outside adapter scope.

Official Jellyfin documentation states that Custom CSS is loaded after default Web styles and only affects clients using Jellyfin Web. Selectors are not part of BigBrain's stable theme contract and must be rechecked for every Jellyfin Web upgrade.
