# Family View & Family Coordination epic

## BB-097 presentation boundary

The normal Family route uses a dedicated `FamilyExperience` composition rather than the generic dashboard-widget frame. This is a frontend presentation boundary only: Meal Planner, Shopping List and Calendar retain their existing modules, API contracts and data ownership. Widget registry metadata continues to provide visibility/order preferences and edit mode, but it does not dictate Family's visible card anatomy, headings or grouping. Other views continue to use `DashboardWorkspace` and `DashboardWidget`; no shared component is deleted. This exception is intentionally narrow until the product owner approves the Family/Obsidian Gold reference direction.

## Status, purpose and priority

This is a future product epic and planning baseline, not an approved sprint, architecture schema or
implementation. Its product goal is to reduce the family logistics that a user must remember,
manually compare and coordinate. It does not change the active Finance direction: BB-091 remains
complete and the next Finance slice remains the smallest deterministic foundation toward
`FINANCE AUTONOMOUS RESEARCH v1` unless later repository evidence and product-owner prioritization
say otherwise.

Family View must not become a dashboard of unrelated widgets. It should eventually turn facts owned
by several modules into shared family context and answer what is happening, who it affects, when it
happens, what surrounds it and whether attention is useful:

```text
Family data -> shared family context -> cross-module coordination
            -> daily/weekly understanding -> conflicts, warnings and useful context
```

No Family implementation, provider research, Figma work, notification rule or implementation slice
is authorized by this epic. The planning areas below are not slice IDs or an approved delivery order.

## Product and architecture principles

- Source modules remain authoritative. Calendar owns events and time semantics; Meal Planner owns
  household meals; Shopping List owns list state; school integrations or manual sources own school
  evidence. Family View references or normalizes those facts for coordination and never silently
  clones, edits or replaces their source records.
- Modules communicate through future versioned public contracts or published facts, never by reading
  another module's storage. Family View should consume capabilities rather than reproduce them.
- Do not prematurely force every source into one giant database entity. Investigate a normalized,
  coordination-only representation such as `FamilyContextItem` with source module and identity,
  type, start/end, affected members, location, importance, confidence, provenance and freshness.
  This name and shape are illustrative, not an approved schema.
- Deterministic source facts and scheduling semantics are authoritative. AI may later summarize,
  explain conflicts or suggest plans and menus, but Family View is useful at a glance and is not a
  chatbot-first experience.
- Users remain authoritative over household planning. Future design must distinguish source facts
  from manual corrections or overrides and may support dismissing a warning, accepting a known
  conflict or choosing a similar meal without silently modifying schedules.

Conceptual relationship:

```text
Calendar -----------+
School schedules ---+
School calendar ----+
School meals -------+
Meal Planner -------+--> FAMILY CONTEXT --> coordination evaluation --> FAMILY VIEW
Activities ---------+
Reminders ----------+
Household context --+
```

## Epic inventory and existing-item disposition

### Already existing / consolidated

| Existing work | Classification | Family-epic relationship |
| --- | --- | --- |
| External school meals in Meal Planner | `MOVE_UNDER_FAMILY_EPIC` | Consolidated under this umbrella while Meal Planner retains ownership. Initial schools remain Rosenfeldtskolan and Musikugglan, Karlskrona. |
| School-aware household menu generation | `MOVE_UNDER_FAMILY_EPIC` | Consolidated as cross-module Family context consumed by Meal Planner; Shopping List retains its own state. |
| Home Calendar past-day presentation and mobile swipe investigation | `REFERENCE_FROM_FAMILY_EPIC` | Home/Calendar UX remains owned there; Family View does not redefine Calendar. |
| BB-029 Calendar external sync and personal-calendar capabilities | `REFERENCE_FROM_FAMILY_EPIC` | Calendar/provider dependency. Its ID, P3/New status, scope and history remain unchanged. |
| BB-036 real-time synchronization of shared family data | `REFERENCE_FROM_FAMILY_EPIC` | Optional platform dependency when live shared updates are designed; its first consumer remains Shopping List. |
| BB-027 dashboard profiles, synchronization and advanced layouts | `PARTIALLY_RELATED` | Relevant to roles, shared layouts and member-aware presentation, but remains a Dashboard concern. |
| Passwordless, trusted devices and step-up investigation | `REFERENCE_FROM_FAMILY_EPIC` | Security dependency for household roles, shared/kiosk devices and sensitive actions; remains BigBrain-wide. |
| BigBrain-wide Obsidian Gold / less-is-more direction | `REFERENCE_FROM_FAMILY_EPIC` | Family UX should follow the candidate design direction; no redesign or Figma change is authorized. |
| Implemented Meal Planner and Shopping List plus their open module backlog | `KEEP_IN_EXISTING_EPIC` | Family coordination consumes relevant context; module behavior, bugs and ownership stay local. |
| Implemented Calendar/Heroma work, including BB-028 | `KEEP_IN_EXISTING_EPIC` | Existing adult work-schedule evidence remains Calendar-owned and is not reclassified as school/family data. |

This disposition is the repository-wide candidate classification for this planning task. Generic
Calendar, Home, Meal Planner and Shopping List items that do not contribute to coordination are not
Family requirements. Finance, Media, Download Control, Sentinel and the separately intended Alpaca
idea are `NOT_RELATED` and were not moved or changed.

### Newly captured capability areas

- explicit household/member context and affected-person associations;
- children's school schedules, school calendars, holidays, study/closed days and attendance state;
- normalized Family Context and generic conflict/warning evaluation;
- focused Family Day and Family Week understanding;
- school attendance as Meal Planner lunch context;
- sparse reminders/notifications fed by useful Family context;
- provenance, freshness, unavailable/stale state and manual overrides;
- privacy, household-role authorization and shared-device restrictions.

### Dependencies

- Calendar and future provider-neutral event capability;
- Meal Planner and Shopping List;
- authoritative school integrations or manual import/entry;
- household/member identity and authorization;
- activities, reminders and notifications;
- passkeys, trusted devices, cross-device and step-up investigation where appropriate;
- accessible Home summary and BigBrain-wide design-system direction.

## School and attendance context

For each configured child, where evidence exists, future coordination may need school days,
start/end time, useful lesson detail, deviations, school-free periods, source/provenance and last
successful update. Do not assume that schools expose APIs. Before unattended acquisition, each
source must be investigated legally and technically. Prefer an official API, structured or calendar
feed, or official school/municipality source over scraping. Manual entry/import remains a fallback.

School-calendar evidence should represent generic typed events rather than hardcode Swedish names.
A future `SchoolCalendarEvent` with `type = HOLIDAY` may illustrate start/end, school/source,
affected members and provenance, but is not an approved model. Relevant examples include autumn,
Christmas, winter/sports, Easter and summer breaks plus other school-specific holidays.

Study days, planning days, teacher-training days and other school-specific closed/deviation days
need especially visible treatment: an otherwise normal-looking calendar can conceal that a child
is home. Normal school days, holidays and study/closed days form an attendance context. School-free
periods may later inform Calendar, reminders, meals, activities, travel and childcare/logistics,
without presuming that childcare exists or does not exist.

## Meal Planner and Shopping List coordination

The existing school-meal decision is preserved, not duplicated. On weekdays, Lunch in the weekly
Meal Planner may show authoritative meals for Rosenfeldtskolan and Musikugglan; weekends retain
normal household Lunch planning. School meals are not a separate Home module. Source/provider,
automated-access rights, caching and truthful unavailable state remain future provider-specific
work. The two schools must not be assumed to share a provider.

School meals are contextual input to household menu generation. Similarity should eventually
consider protein, carbohydrate/base, dish type, preparation, sauce/style and cuisine/flavor family,
not only dish names. The qualitative intent remains strongest avoidance on the same day, strong the
day before/after, softer two to three days away and normally acceptable later. These are not final
weights. Manual household choice always wins.

Attendance adds a separate input: a normal school day may provide school-lunch context, while a
holiday or study day may mean household Lunch is needed. Do not assume that every configured child
eats school lunch; exact member configuration is open. Generated household meals may add household
ingredients to Shopping List, but school meals themselves must never add ingredients to it.

## Family Day, Family Week and Home

`Family Day` should answer “What does today look like for the family?” by associating relevant
school, school-free, appointment, activity, meal and household facts with one member, several
members, the whole household or an external institution. Names, avatars, colors or icons come from
configured members; generic architecture never hardcodes actual people.

`Family Week` should make school, unusual/free days, appointments, activities, meals, important
household events and conflicts understandable without becoming a wall of schedules. Apply
`LESS IS MORE` and progressive disclosure:

```text
today -> what matters now
week -> meaningful overview
attention -> sparse and useful
details and source/sync evidence -> on demand
```

Home should not duplicate the detailed Family View. It may eventually surface only the most useful
family summary, such as today's important deviation, the next important event or a conflict needing
attention. Exact Family navigation remains open. Calendar remains a module in Home; its existing
past-day recession and accessible swipe investigations remain separate UX work.

## Coordination and conflict semantics

Future design should evaluate normalized relevant facts generically, not accumulate source-pair
conditionals such as `if SchoolEvent && CalendarEvent`. Candidate outcomes are:

- `HARD CONFLICT`: an actual incompatible obligation, for example one person in two places, a child
  expected at school during a mandatory appointment, one adult required simultaneously by two
  activities, or incompatible transport needs. Travel/routing is not implemented or assumed.
- `SOFT WARNING`: a plan may be difficult but possible, for example short transfer time after school,
  dinner overlapping activities, an unusually busy day or similar school/home meals.
- `INFORMATION`: useful context without a problem, for example a study day tomorrow, holiday starting
  Friday, earlier school finish, unavailable school meal or a quiet evening.
- `NO ISSUE`: no useful attention signal.

Warnings must be sparse, useful and explainable: what conflicts, who is affected, when and why it
matters, plus what the user may want to inspect where possible. Sharing a date alone is insufficient.
Family View informs; it does not silently cancel or change plans.

Representative future scenarios:

1. School until 14:30 plus dentist at 14:00 may be a hard conflict.
2. A study day plus relevant adults' commitments is useful warning/context; childcare is unknown.
3. School ending 15:30 plus an activity at 15:45 elsewhere may be a soft warning when future travel
   semantics support it.
4. School lasagne plus proposed household lasagne should cause Meal Planner to prefer another
   suitable option, subject to manual choice.
5. Household dinner at 17:30 plus activities starting at 17:15 may be impractical.
6. A school holiday suppresses normal attendance assumptions and may change household Lunch needs.

Longer term, coordination intelligence may summarize why a day is difficult, include Lunch on a
school-free day or suggest inspecting dinner timing. These are future outcomes, not implemented AI
reasoning or approved automated actions.

## Provenance, freshness, privacy and notifications

Imported facts must expose enough metadata to distinguish source, manual entry, last successful
sync, stale/unknown state and unavailable source. Stale school evidence must never be presented as
confidently current. Manual corrections or assumptions must remain distinguishable from source data.

Family context contains sensitive household and child scheduling information. Future design must
cover household-role authorization, child-data privacy, least privilege, shared/kiosk-device
restrictions, unnecessary external-disclosure prevention and auditability for important changes.
Coordinate with planned passkeys, trusted devices, cross-device authentication and step-up; this
epic changes no authentication behavior.

Family context may later feed quiet, configurable reminders such as “Studiedag imorgon” or “Två
aktiviteter krockar på torsdag”. Notification thresholds, channels, acknowledgement and anti-noise
behavior require separate design. No notification rule is authorized here.

## Deferred planning areas and open questions

A later owner-approved planning session may consider family/member context, school acquisition,
Family Context normalization, conflict evaluation, daily/weekly UX, cross-module Meal Planner
behavior, notifications, provenance/freshness and accessibility. Before slices are designed, keep
these questions open:

- exact navigation and daily versus weekly layout;
- useful depth of school timetable/lesson detail;
- legal and technical acquisition per school schedule, calendar and meal source;
- exact external-calendar integration (Google, Apple, ICS or another provider is not mandatory);
- travel-time calculation, transport responsibility and childcare assumptions;
- hard/soft/information and notification thresholds, suppression and dismissal;
- manual correction, override and accepted-conflict semantics;
- household membership, role scope, shared/custody or multiple-household behavior if ever needed;
- exact school-meal provider implementation and school-lunch participation configuration;
- freshness/confidence UX and failure behavior;
- activity ownership and how Home summarizes Family context without duplication.

## Definition of ready for later epic planning

The product owner has approved priority after current Finance work; authoritative ownership and
privacy boundaries are agreed; existing items and dependencies remain linked without duplication;
source and member assumptions are explicit; open questions are narrowed enough to define a smallest
coherent slice; and a separate Definition of Done exists for that slice. This document itself starts
no sprint.
