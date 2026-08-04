# Design system v1 manual verification

## BigBrain Web

1. Open BigBrain on a phone and confirm the dark default theme is readable.
2. Choose `Ljust` under `Tema`, reload and confirm the selection persists.
3. Verify navigation, Media, Smart Shuffle, Shopping List and Meal Planner.
4. Verify forms, primary/secondary/danger/ghost buttons, dialogs, badges and status messages.
5. Test at 320 px and a normal phone width without horizontal loss of actions.
6. Navigate by keyboard on desktop and confirm every interactive element has visible focus.
7. Enlarge text to 200% and confirm content/actions remain usable.

## Jellyfin (installed; manual visual approval pending)

The adapter was backed up and installed additively on 2026-08-04. Follow the
[operations runbook](../operations/runbooks/jellyfin-bigbrain-theme.md) for any future
install, update or rollback.

1. Check home/library views, a detail page, forms and dialogs.
2. Confirm playback controls remain visible and usable; stop and remove the theme if readability/function degrades.
3. Check mobile Jellyfin Web.
4. Open Jellyfin on Samsung Tizen, determine whether server CSS is actually applied, and record working/non-working selectors.
5. If anything degrades, use the documented block removal or backup restore; no database
   or client patch is involved and a server restart is normally unnecessary.
