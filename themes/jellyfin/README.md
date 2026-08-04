# BigBrain Jellyfin theme adapter v1

`bigbrain-jellyfin.css` is a standalone, manually installable adapter for Jellyfin Web. It shares BigBrain's visual principles and palette through `--bb-jf-` tokens but has no dependency on BigBrain Web CSS or `bb-` classes. `tokens.css` is the editable token reference; the same values are embedded in the standalone file so it can later be copied as one unit.

The standalone adapter was installed additively in the approved Jellyfin 10.11.11
instance on 2026-08-04. Publication is still an explicit operator action: preserve the
existing Custom CSS byte-for-byte and place this file inside exactly one
`BEGIN BIGBRAIN THEME` / `END BIGBRAIN THEME` block. Do not use external imports, URLs
or fonts.

The adapter changes color, header, drawer/navigation, cards/posters, buttons, forms, dialogs, details, lists and focus states. It does not hide controls, modify video/subtitles, resize posters globally or patch a native client.

See [compatibility](compatibility.md), the [install/rollback runbook](../../docs/operations/runbooks/jellyfin-bigbrain-theme.md)
and the [manual verification plan](../../docs/design-system/manual-verification.md).
