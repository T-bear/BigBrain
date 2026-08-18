# Product, UX, authentication and school-meals backlog capture

> Historical planning record. On 2026-08-18 the Family-specific requirements were consolidated,
> without duplication or implementation, into the canonical
> [Family View & Family Coordination epic](../../architecture/family-view-family-coordination.md).

- Date: 2026-08-17
- Scope: repository-wide duplicate assessment and future product-intent capture
- Status: planning only; not approved for implementation

## Guardrails and priority

This record introduces no code, API, schema, dependency, deployment, external research or provider
access. It starts neither UX redesign, security consolidation, autonomous research nor Finance work.
BB-091 remains complete, and the current Finance direction remains the smallest deterministic
foundation toward `FINANCE AUTONOMOUS RESEARCH v1`. The repository's existing security and
penetration-testing ordering is unchanged. New items have no BB IDs until normal prioritization.

## Repository-wide classification

| Product idea | Classification before this capture | Existing evidence and capture action |
| --- | --- | --- |
| BigBrain-wide Obsidian Gold design system and less-is-more hierarchy | PARTIALLY_CAPTURED | Obsidian Gold theme/tokens and interaction/accessibility states already existed in the theme contract, STATUS, ADR 0012 and code. The cross-module hierarchy, copy reduction and candidate-not-final semantics were missing and are now in the backlog. |
| Progressive disclosure, including human-first Finance information | PARTIALLY_CAPTURED | BB-088 and the Finance roadmap already record a summary-to-evidence hierarchy. The BigBrain-wide product principle was missing and is now captured without redesigning Finance. |
| Passwordless/passkey/trusted-device/cross-device and step-up authentication | PARTIALLY_CAPTURED | `ARCHITECTURE.md` and authentication knowledge already define OIDC/OAuth, authorization and the rule that a network boundary is insufficient. WebAuthn/passkeys, device lifecycle, phone-assisted login, biometrics semantics and step-up were missing and are now captured. |
| Optional network trust and differentiated device use cases | PARTIALLY_CAPTURED | The network boundary and future role/layout concepts existed separately. Device identity versus user identity, complementary private-network scope and shared/kiosk restrictions are now explicit. |
| School meals in the weekly Meal Planner | MISSING | Added as a future generic external meal-source enhancement with Rosenfeldtskolan and Musikugglan explicitly named and separate-provider assumptions prohibited. |
| School-aware household menu generation | MISSING | Added as future Meal Planner intelligence using semantic and temporal soft constraints across all relevant sources, with manual choice winning. |
| Calendar past-day presentation in Home | MISSING | Added as accessible presentation intent. Calendar is explicitly a module in Home, not a standalone view. |
| Mobile swipe navigation | MISSING | Added as an investigation with gesture-conflict, alternative navigation, accessibility and destructive-action safeguards. |

No searched idea was fully `ALREADY_CAPTURED` at the supplied level of product intent. Existing
material was preserved and only complemented; no duplicate BB item was created.

## Obsidian Gold and less-is-more direction

The implemented Obsidian Gold theme is evidence of an existing visual option, not approval of a
BigBrain-wide redesign or a final visual specification. The candidate direction is a dark Obsidian
foundation with restrained gold used primarily for focus, action and emphasis rather than decoration.
Future consolidation should standardize typography and text hierarchy, buttons, inputs, cards and
surfaces, spacing, radii, icons, status presentation, interaction states, responsive behavior,
accessibility/focus states and a coherent color/token system.

Product-owner principle: **the interface should not explain what is already self-evident.** Avoid a
title, subtitle, explanatory paragraph and card title that restate one obvious idea. Prefer:

1. data or content first;
2. a short contextual label;
3. detail on demand.

Use one heading per real information level. Do not explain obvious controls or modules. An icon may
replace text only while its meaning remains clear. Whitespace is intentional. Technical information
belongs deeper in the hierarchy, and summaries should not repeat what is already visually obvious.

The desired disclosure path is `Home → summary → detail → technical evidence/settings`. Finance
should lead with understandable human-level information while Market Memory, datasets, backtests,
providers, revisions and risk evidence remain available below the primary experience. This is a
future cross-BigBrain principle and does not authorize a Finance redesign.

## Passwordless, trusted-device and step-up investigation

The goal is strong authentication with little everyday friction on a trusted authenticated device.
Future design should evaluate established WebAuthn/passkey standards, passwordless authentication,
platform authenticators, enrollment, human-readable device naming/management, revocation,
cross-device authentication, recovery/bootstrap and lost-device handling.

A desired standards-based cross-device experience is:

`Computer → BigBrain login → Logga in med telefon → QR/proximity flow → local Face ID or equivalent → cryptographic approval → BigBrain session on computer`.

BigBrain must never receive or store Face ID, Touch ID or equivalent biometric data. Local biometric
verification only unlocks or authorizes use of the cryptographic credential on the device. MAC
addresses are not trusted authentication identities, and no homemade biometric/authentication
protocol is acceptable.

Risk-based step-up may later require a fresh passkey or locally biometric-backed credential for
server shutdown/restart, security configuration, enrollment or removal of trusted devices,
privileged administration and any separately approved future high-risk Finance operation. Low-risk
reads should have minimal friction. Very high-risk Finance execution, if ever approved, cannot
inherit authority merely from a dashboard session; this plan creates no execution authority.

Future authorization should account for owner/adult personal devices, shared family tablets,
kitchen displays, kiosks/displays and service devices without encoding actual family members in
generic architecture. Shared/kiosk use may be capability-restricted and must not become an owner
session. Tailscale device identity or an equivalent zero-trust/private-network layer may be
investigated as a complement for remote access, never as a replacement for application-level user
identity and authorization; device identity is not user identity.

Place this work according to the established sequence: active Finance work first, then the relevant
BigBrain consolidation/security and authentication/authorization hardening, followed by penetration
testing as the repository roadmap requires. This record does not independently move any gate.

## External school meals in the weekly Meal Planner

The initial use case is Rosenfeldtskolan and Musikugglan in Karlskrona. Do not assume a shared
provider. On weekdays the existing Meal Planner Lunch area should eventually show each configured
external school's meal; household Dinner remains the household plan. On weekends Lunch remains the
normal household planned meal. This belongs in Meal Planner, not a separate Home module.

Do not hardcode the domain around two schools. A future generic `ExternalMealSource`-like concept
may represent a school, preschool, workplace or other institution and carry appropriate identity,
provider and provenance. The name is illustrative rather than an approved contract.

Before implementation, separately investigate:

- Rosenfeldtskolan: Karlskrona municipality's current official school-menu source, official/public
  API or structured endpoint, automated-access terms and retention/caching terms.
- Musikugglan: the official school/Tant Grön source, whether it publishes directly or through
  another meal service, structured/API access, automated-access terms and retention/caching terms.

Prefer an official structured API/feed to HTML scraping. If rights or automated access are unclear,
classify the source `MANUAL_REVIEW` and do not automate it. Acquisition should later follow provider
behavior at approximately weekly rather than constant cadence. Provider failure must remain isolated:
household planning works, no menu is fabricated, an honest missing/unavailable state is shown and
retry follows a bounded policy.

## School-aware household menu intelligence

School meals are contextual input to household weekly generation, not display-only data. Future
generation should avoid serving substantially similar food immediately around school meals while
considering every relevant external source. If one school serves fish and another pasta, a household
dinner should preferably differ reasonably from both rather than optimize for only one source.

Similarity is semantic, not dish-name string matching. Candidate attributes include main protein,
carbohydrate/base, dish type, preparation, sauce/style and broad flavor/cuisine family. Thus meatballs
with potatoes and cream sauce may be relatively similar to a hamburger steak with the same base and
sauce, while chicken noodle stir-fry is substantially different. No classifier is selected here.

Temporal weighting remains a future design: strongest avoidance on the same day, strong around the
day before/after, softer two or three days away and normally acceptable later. These are qualitative
intent statements, not hardcoded weights. Similarity is normally a soft preference/optimization
constraint, never an irreversible ban; manual household choice wins.

The broader direction combines school meals, household preferences, meal history, variation,
leftovers, budget, available ingredients where supported and shopping-list integration into a weekly
household menu. Generated household meals may produce household shopping requirements. School meals
themselves must not add their ingredients to the household shopping list.

## Home Calendar and mobile navigation investigations

Calendar is a module in the Home view, not a standalone Calendar view. Past days should visually
recede so the current and upcoming portion of the week is immediately understandable. Reduced
emphasis, appropriate strike-through, opacity or another treatment may be evaluated, but the state
must remain accessible and must not rely only on color or ambiguous opacity.

Touch swipe between appropriate BigBrain views or contexts is an investigation, not a decided
interaction. Evaluation is mobile-first and must avoid conflicts with browser/system back gestures
and horizontal controls or carousels. Keyboard/mouse and visible navigation remain available,
accessibility is preserved and no gesture may cause an accidental destructive action.
