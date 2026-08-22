# Handoff: Parking Pass App (subscription + digital pass prototype)

## Overview
A mobile app prototype for parking users to subscribe to a garage's parking plan, hold their subscription as a digital "pass" (recognized by license plate, with a QR backup), manage their vehicles, and view/renew their plan. Built for the **Voltway design system** (dark-mode-first, techy/energetic/trustworthy visual language — see Design Tokens below).

## About the Design Files
The files in this bundle are **design references created in HTML** — an interactive click-through prototype showing intended look, layout, and behavior. They are not production code to copy directly. The task is to **recreate these HTML designs in the target codebase's existing environment** (React Native, Flutter, SwiftUI, native Android, etc.) using its established patterns, navigation stack, and component library — or, if no mobile environment exists yet, to choose the most appropriate framework and implement the designs there.

`Parking Pass App.dc.html` is a single self-contained prototype file (custom lightweight component runtime + inline styles) rendered inside an iPhone bezel (`ios-frame.jsx`, a cosmetic device frame only — do not port this file, it exists purely to preview the app on a phone-shaped canvas in the browser).

## Fidelity
**High-fidelity (hifi).** Colors, typography, spacing, and copy are intended to be final/near-final. Recreate the UI closely using the codebase's existing design system components where they already match Voltway tokens; introduce new components only where the codebase has no equivalent.

## Screens / Views

### 1. Plans (`screen.plans`)
**Purpose:** Choose a subscription plan for a specific garage before paying.
**Layout:** Full-height scrolling column, iOS status-bar-safe top padding (~58–60px).
- Header block: overline "HARBORVIEW GARAGE" (11px uppercase tracked, `--fg-3`), title "Choose your plan" (24px/32px Space Grotesk 600), subtitle address (13px `--fg-2`).
- Billing toggle: two-segment control, "Monthly" / "Annual · 2 months free", 40px tall pill buttons, active = `--volt` background with `#0F1117` text, inactive = `--bg-elevated` with border.
- Three plan cards (Basic / Plus / Unlimited), each: `--bg-elevated` fill, 16px radius, 18px padding, 1px `--border` (1.5px `--volt` + `--volt-soft` tint when selected), tap-to-select (radio dot top-right), plan name (18px Space Grotesk 600), optional "Most popular" pill on Plus (volt-soft bg, volt text), big price (32px/36px JetBrains Mono tabular nums) + period suffix, two feature lines (13px `--fg-2`).
  - Basic: €39/mo — "Nights & weekends only", "1 vehicle · standard spaces"
  - Plus: €69/mo — "Anytime access", "1 vehicle · reserved level" (marked Most popular)
  - Unlimited: €99/mo — "Anytime access", "2 vehicles · reserved level + EV bay"
  - Annual price = monthly base × 10 (2 months free), same feature copy.
- Start-date row: card showing "Start date" label + formatted date (e.g. "Jul 12"), with a native date picker input on the right. Defaults to today for a first-time purchase; when initiated via "Manage plan" from an existing pass, defaults to the day after the current subscription's end date.
- Sticky-feeling bottom CTA: full-width 56px pill button, `--volt` bg, "Subscribe · €NN.NN / period".

### 2. Payment (`screen.payment`)
**Purpose:** Pay for the selected plan. No payment methods are ever stored — every checkout is entered fresh.
**Layout:**
- Header: back-chevron icon button (40×40, `--surface-2`, 16px radius) + "Payment" title.
- "Amount due" card: `--bg-elevated`, 24px radius, big mono price (44px) on the left, plan name + period + "Starts <date>" on the right.
- "Select method" section: two selectable rows —
  - **Apple Pay** (black icon swatch, "Touch ID · ready" subcopy)
  - **Credit or debit card** (card icon, "Not saved after checkout" subcopy) — selecting this expands three inputs: Card number, MM/YY, CVC (mono font, 48px tall, `--surface-2` fill).
  - Both rows use the same selected/unselected card treatment as plan cards (radio + border + tint).
- Bottom CTA button cycles through three copy states on tap: "Confirm payment · €NN.NN" → "Processing…" (1.1s) → "Payment Confirmed!" (then auto-navigates to the Pass screen after ~2.2s total). Button color deepens slightly on success.
- Below CTA: small centered security note with lock icon — "Secured by 256-bit SSL encryption".

### 3. Pass (`screen.pass`, default/home screen)
**Purpose:** Show the active subscription as a digital pass; primary and only mandatory screen.
**Layout:**
- No screen title — content starts right below the status bar (~58px top padding) so the QR reader fits near the top.
- Single hero card: `--bg-elevated`, 1.5px `--volt` border, 24px radius, soft green ring shadow (`0 0 0 4px rgba(34,224,122,0.10)`).
  - **QR block** (top, centered): white rounded panel containing a bespoke QR-style SVG graphic (132×132) + caption "BACKUP ENTRY CODE" (11px uppercase tracked, muted gray). This is a decorative/placeholder QR — wire up a real QR encoder (e.g. of a pass/entry token) in production.
  - Status row: "Active" pill badge (pulsing 8px dot, 2s ease-in-out loop — the brand's signature "live pulse", see Motion tokens) + plan name (right-aligned, Space Grotesk 600).
  - License plate block: overline "LICENSE PLATE", plate number in large mono (40px/44px JetBrains Mono 700), helper line "Recognized automatically at entry — no scan needed." If the user has more than one vehicle on file, an additional muted line reads "+N more vehicle on file · manage in Profile". If no plate is on file, the plate value falls back to "No plate on file" and the recognition helper text is hidden.
  - Divider, then a two-column row: "Garage" (Harborview Garage) / "End date" (larger 20px mono, right-aligned — computed as start date + 1 billing period − 1 day, e.g. purchase Jul 12 monthly → ends Aug 11).
  - Final row: "Price" label / plain price value (no "/month" suffix — the product intentionally avoids "subscription" framing; it reads as a one-time-feeling parking pass, not a recurring subscription).
- "Manage plan" button (full-width, outline style, `--border-strong` border) below the card — routes back to the Plans screen, pre-filling the next plan's proposed start date as described above. **There is no cancel-subscription flow** — it was intentionally removed.
- If the user has additional **future-dated** (not-yet-active) subscriptions purchased via "Manage plan", they render as small cards below the Manage-plan button under an "Upcoming" label — each ~48px tall, showing plan name + "start – end" date range in mono.

### 4. Profile (`screen.profile`)
**Purpose:** Manage identity, vehicles, contact info, subscription shortcut, and settings.
**Layout:**
- Header: "Profile" title only (no theme toggle here — moved into Settings, see below).
- Identity card: circular gradient avatar with initials, name (18px), "Member since" caption.
- **Vehicles section** (the focus of recent iteration): horizontally swipeable card carousel (`overflow-x:auto; scroll-snap-type:x mandatory`), one full-width card per vehicle plus a trailing "+ Add car" dashed card (hidden once 3 vehicles exist — 3 is the max). Section label reads "Vehicles" with a "{n}/3 · swipe for more" hint on the right.
  - Each vehicle card: small × remove button top-right corner (28×28 circle, `--surface-2` bg, red glyph) — tapping removes that vehicle immediately (there is no minimum; a user may end up with zero vehicles).
  - Fields per card: **Make** and **Model** side-by-side (two half-width inputs, 44px tall), then **License plate** full-width below (56px tall, 23px JetBrains Mono, **center-aligned** text — this field is intentionally ~1.5× the size of the other inputs since it's the primary identifier). There is no Color field (removed).
  - Each card has its own **Save** button (full-width under the fields) — always visible, but dimmed/disabled (opacity 0.4, `not-allowed` cursor, non-interactive) until at least one field differs from its last-saved value, then becomes fully opaque `--volt`/`#0F1117` and clickable. Edits are local "draft" state per card until Save is pressed; nothing commits (and nothing shown elsewhere, e.g. on the Pass screen) until Save is tapped.
  - Helper line below the carousel: "Any plate on file is recognized automatically at Harborview Garage entry."
- Contact info card: Phone, Email rows (icon + label + value).
- "Subscription" shortcut card (volt-tinted, tappable): shows current plan name + garage, routes to the Pass screen.
- **Settings** list:
  - **Appearance** row — inline light/dark segmented toggle (two 36×32 circular buttons, sun / moon icons) that switches the entire app's theme live. This is the *only* theme control in the app (there is no separate theme toggle elsewhere).
  - Notifications row — standard on/off toggle switch.
  - Sign out row (red glyph + label, no confirmation dialog wired up).
- Footer: small centered version string "Parking pass v1.0".

## Bottom Navigation
Two tabs only: **Pass** and **Profile** (a "Plans" tab was considered and removed — Plans is reached only via "Manage plan" on the Pass screen). Nav bar is a floating pill (rounded 32px, blurred dark translucent background, `backdrop-filter: blur(24px) saturate(180%)`), fixed 12px from left/right and 22px from bottom, and is **always visible on every screen** (including Plans and Payment — those screens reserve ~100px bottom padding so content isn't hidden underneath it). Active tab icon/label is `--volt` green; inactive is muted gray (`#6A7187`).

## Interactions & Behavior
- **Plan selection**: tapping a plan card selects it (radio fills, border/tint updates); billing-cycle toggle recalculates all three prices live.
- **Start date picker**: native date input; changing it updates the displayed formatted date and (on Pass) the computed end date immediately.
- **Manage plan → next start date**: computed as (max end-date across all existing subscriptions) + 1 day, so a renewal/second plan never overlaps the current one.
- **Payment confirm**: three-state button (default → loading (~1.1s) → success (~1.1s more)) then auto-navigates to Pass and appends the new subscription to the list (sorted by start date). If its start date is in the future relative to the existing active one, it shows as an "Upcoming" mini-card on the Pass screen rather than replacing the active card.
- **Vehicle carousel**: swipe/scroll horizontally (CSS scroll-snap; no custom JS drag logic needed — a native scroll view with paging behaves the same). "Add car" appends a blank draft card (editable immediately, not yet saved). Removing a vehicle (× button) is immediate, no confirmation, no minimum-count restriction.
- **Per-field vehicle Save**: each vehicle card tracks its own "changed" state (draft vs. last-saved snapshot: Make, Model, Plate). Save only commits that one card.
- **Theme toggle**: instant switch, no transition animation currently implemented (spec suggests a 320ms crossfade per Voltway motion tokens — see below).
- **Live pulse**: the "Active" badge dot on the Pass card pulses continuously — 2s, ease `cubic-bezier(0.2, 0.8, 0.2, 1)`, opacity 1→0.55 + scale 1→0.85. This is the Voltway brand's signature motion loop; the same timing/easing should be reused for any other "live" indicator.

## State Management
Key state needed in the real app:
- `subscriptions`: list of `{ id, planId, billing, startDate }`, sorted ascending by start date. First = active/current; rest = upcoming/scheduled.
- `vehicles`: list of `{ id, make, model, plate }`, max length 3.
- Draft/edit-buffer copies of vehicle fields (per-card "unsaved changes" tracking) — do not write through to the committed list until Save.
- `theme`: 'dark' | 'light', user-togglable, persisted across sessions in production (not persisted in the prototype).
- Payment flow transient state: selected method (Apple Pay vs. card), card form fields (never persisted/stored), payment status (idle/processing/success).
- Currently no backend/date-of-purchase persistence, no real payment processing, no real QR encoding, no real license-plate-recognition integration — all of these need real implementations.

## Design Tokens (Voltway design system)
**Color** (dark mode, primary surface):
- `--bg: #0F1117` (app background) · `--bg-elevated: #171A22` (cards) · `--surface-2: #1F2330` (inputs)
- `--border: #252A38` · `--border-strong: #363C4E`
- `--fg-1: #F2F4F8` (primary text) · `--fg-2: #A4ABBD` (secondary) · `--fg-3: #6A7187` (tertiary/hints)
- `--volt: #22E07A` (brand green — CTAs, selected state, active pins) · `--volt-soft: rgba(34,224,122,0.14)` (tint)
- `--danger` / remove glyphs: `#FF5C5C` / `#FF6B6B`

**Color** (light mode — same accent system, flipped surfaces):
- `--bg: #F4F6FA` · `--bg-elevated: #FFFFFF` · `--surface-2: #FFFFFF`
- `--border: #E3E7EE` · `--border-strong: #CFD5E0`
- `--fg-1: #0F1117` · `--fg-2: #5B6478` · `--fg-3: #8A92A4`

**Typography:**
- Space Grotesk 600 — display/headings/plan names/big numbers (Google Fonts)
- Inter 400/500/600 — body, labels, buttons, captions
- JetBrains Mono 500/700 — prices, dates, plate numbers, all "readout" digits (tabular nums)

**Radii:** 12px (inputs/small buttons), 16px (cards), 24px (hero cards/primary CTA), 999px (pills/toggles/nav).

**Motion:** standard ease `cubic-bezier(0.2, 0.8, 0.2, 1)`; signature "live pulse" always 2s, same easing, same green color family — reuse for any real-time/live indicator, never vary the timing.

**Icons:** Lucide-style 1.5px stroke SVGs, 24px artboard, `currentColor` by default, `--volt` for active/selected states only. No emoji anywhere.

## Assets
No bitmap/photo assets are used — this is a purely typographic + iconographic UI per the Voltway design system ("no photography in-product, no illustrations"). All icons are inline SVG (Lucide-style paths, hand-written in the prototype markup). The QR graphic on the Pass screen is a bespoke placeholder SVG, not a functional QR code — swap in a real QR/barcode encoding library in production.

## Files
- `Parking Pass App.dc.html` — the full prototype (all four screens + bottom nav + all interaction logic). This is the primary reference; open it in a browser to click through the flow.
- `ios-frame.jsx` — cosmetic iPhone bezel used only to preview the app on a phone-shaped canvas. Not part of the product design itself; do not port.
