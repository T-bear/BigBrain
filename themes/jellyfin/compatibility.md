# Jellyfin adapter compatibility v1

- Adapter version: 1.0.0.
- Selector baseline: installed Jellyfin Server/Web 10.11.11, inspected read-only 2026-08-04.
- Server Custom CSS: installed additively 2026-08-04 through Jellyfin's named Branding
  configuration API; the previous CSS was preserved byte-for-byte and no restart occurred.
- Jellyfin Web desktop/mobile: selectors and syntax verified; headless login-page DOM at
  1440 x 1000 and 390 x 844 loaded the adapter token without horizontal overflow.
  Authenticated home/card/detail/dialog views still require manual visual approval.
- Jellyfin for Tizen: an active, remote-controllable Samsung session was confirmed
  read-only after installation, without playback. Visual/navigation verification on the
  real TV remains required.
- Other native clients: unsupported/not verified unless they use Jellyfin Web and load server Custom CSS.
- Administration dashboard: server Custom CSS is intentionally not applied by Jellyfin 10.11, so it is outside adapter scope.
- Obsidian Gold variant: `bigbrain-obsidian-gold.css` uses the same verified 10.11.11 selector set. It is technically self-contained but is not installed or visually approved; authenticated Jellyfin Web and real Samsung Tizen verification remain manual gates.

Official Jellyfin documentation states that Custom CSS is loaded after default Web styles and only affects clients using Jellyfin Web. Selectors are not part of BigBrain's stable theme contract and must be rechecked for every Jellyfin Web upgrade.
