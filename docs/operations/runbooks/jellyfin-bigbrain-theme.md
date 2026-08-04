# Jellyfin BigBrain theme installation and rollback

## Scope and current state

This runbook applies only to Jellyfin's named Branding `CustomCss` setting. Adapter
version 1.0.0 in `themes/jellyfin/bigbrain-jellyfin.css` is standalone and was installed
additively on Jellyfin 10.11.11 on 2026-08-04. A server/container restart is normally not
needed; clients reload the web application instead.

Never place credentials in the CSS, command history, reports or repository. Before every
write, read the current Branding DTO, create a new 0600 CSS backup outside Git, calculate
its SHA-256, and verify the target server version and identity. Preserve every byte outside
the marked block and preserve all non-CSS Branding fields.

## Install or update

1. Read `GET /Branding/Configuration` through the locally authorized administration
   path and extract `CustomCss` without logging authentication material.
2. Back it up as
   `/home/enigma/BigBrain/backups/jellyfin-custom-css/jellyfin-custom-css-before-bigbrain-theme-YYYYMMDD-HHMMSS.css`
   with mode 0600; refuse overwrite and record SHA-256.
3. Validate the standalone adapter: balanced braces, no empty rulesets, imports, URLs,
   secrets or hiding/pointer-blocking declarations; verify its selectors against the
   installed Jellyfin Web assets.
4. If a marked block exists, replace only its contents. Otherwise append exactly:

   ```css
   /* BEGIN BIGBRAIN THEME */
   /* contents of themes/jellyfin/bigbrain-jellyfin.css */
   /* END BIGBRAIN THEME */
   ```

5. Send the complete Branding DTO to Jellyfin's documented named configuration endpoint,
   `POST /System/Configuration/branding`. Do not edit the database or XML directly.
6. Read Branding back. Require one begin/end marker, the expected canonical CSS hash,
   byte-identical pre-existing CSS, and unchanged `LoginDisclaimer` and
   `SplashscreenEnabled`.
7. Verify health, logs, container ID, start time and restart count. Reload Jellyfin Web;
   do not start media as part of theme verification.

## Rollback

Preferred rollback removes the complete marked BigBrain block, including its separating
newlines, and leaves every other byte unchanged. Post the resulting complete Branding DTO,
then read it back and verify that no BigBrain marker remains and that the remaining CSS
matches its pre-install hash.

For exact recovery, load the selected 0600 backup as `CustomCss`, retain the current
non-CSS Branding fields, post the complete DTO, and verify the read-back CSS against the
backup SHA-256. Do not restore the whole `branding.xml` and do not manipulate Jellyfin's
database. No container restart should be required. In Jellyfin Web reload the page; on
Samsung TV close and reopen Jellyfin for Tizen, and if necessary sign out/in or reload the
app.

Stop before writing if the target instance, authenticated write contract, backup, existing
CSS preservation or selector compatibility cannot be proven.
