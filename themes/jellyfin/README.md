# BigBrain Jellyfin theme adapter v1

`bigbrain-jellyfin.css` is a standalone, manually installable adapter for Jellyfin Web. It shares BigBrain's visual principles and palette through `--bb-jf-` tokens but has no dependency on BigBrain Web CSS or `bb-` classes. `tokens.css` is the editable token reference; the same values are embedded in the standalone file so it can later be copied as one unit.

Nothing in this directory installs or publishes the theme. After separate approval, an administrator may back up current Custom CSS and paste the standalone file into Jellyfin's Branding Custom CSS field. Do not use external imports, URLs or fonts.

The adapter changes color, header, drawer/navigation, cards/posters, buttons, forms, dialogs, details, lists and focus states. It does not hide controls, modify video/subtitles, resize posters globally or patch a native client.

See [compatibility](compatibility.md) and the [manual verification plan](../../docs/design-system/manual-verification.md).
