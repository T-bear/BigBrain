# ADR 0037: Native audiobook playback boundary

- Status: Accepted
- Date: 2026-08-29

## Context

Audiobookshelf owns audiobook files, playback sessions and durable per-user progress. The existing restricted BigBrain integration identity has catalogue responsibility and no owner progress. BB-112 verified a separate active restricted playback identity with real progress; the integration credential was not repurposed.

## Decision

BigBrain Web owns the playback experience while Audiobookshelf remains the playback engine and progress source of truth. Both credentials remain server-side: `ApiKey` is catalogue/integration identity and `PlaybackApiKey` is the owner playback identity. Web calls only the versioned same-origin BigBrain API.

Session start accepts only a validated Audiobookshelf item ID. API creates a random process-local opaque session ID, binds it to exactly one upstream session, item and allowlisted track set, and expires it after a bounded lifetime. Track delivery constructs the upstream session route server-side, accepts one explicit byte range, caps it to 8 MiB, preserves media/Range headers and never accepts an upstream URL. Sync and close accept bounded position, duration and listening deltas and always use the configured playback credential. Audiobookshelf remains authoritative after reload.

Playback state and the audio element live at the existing AppShell lifetime so route changes do not stop audio. No new state framework or BigBrain progress database is introduced. Current BigBrain deployment is a single-owner appliance without a general authenticated-user mapping; opaque session ownership follows that existing deployment boundary. A future multi-user authentication model must add authenticated BigBrain-user to playback-identity/session ownership before sharing this capability.

## Consequences and rollback

API restart safely invalidates BigBrain session IDs and Audiobookshelf progress remains durable. Removing the playback environment value and recreating API disables native session creation without affecting catalogue reads. Rollback must not remove Audiobookshelf media or progress and must preserve the integration credential.
