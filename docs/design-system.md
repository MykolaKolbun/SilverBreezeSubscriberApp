# Design System

The web frontend (`ParkingSubscription.Web`) uses a layered design-token system shared with the EVCharging app family: same palette, same 4-point spacing grid, same typography and radius scales. This document covers the tokens, the color palette, and the component classes.

Unlike the React Native projects (where tokens are TypeScript exports), here tokens are **CSS custom properties** declared once in [`wwwroot/site.css`](../backend/ParkingSubscription.Web/wwwroot/site.css). There is no build step — the `:root` block *is* the theme.

---

## Token location

Everything lives in one file:

| Section of `site.css` | Purpose |
|---|---|
| `:root { … }` | Full dark-mode palette + spacing + radius tokens (the base, like `Colors`) |
| `@media (prefers-color-scheme: light)` | Light-mode overrides — only values that differ (like `LightColors`) |
| Typography presets | `h1`–`h3` element styles + utility classes (`.body`, `.label`, `.tag`, …) |
| Component classes | `.card`, `.panel`, button variants, `.alert`, `.status-*`, forms |

Pages consume tokens via `var(--token-name)` in CSS, or by using the component classes in markup. **Light/dark switching is automatic** — it follows the OS setting; no toggle, no JavaScript.

---

## Colors

### Dark mode (base palette)

```
Backgrounds
  --bg-primary  #0F1117   — outermost page background
  --bg-card     #161B27   — cards, panels, top bar
  --bg-input    #1A1F2E   — text inputs, secondary buttons
  --bg-chip     #1E2436   — neutral chips/pills

Brand
  --primary       #2EE89E — action color: buttons, links, prices
  --primary-dark  #1BC47D — pressed / hover variant

Text
  --text-primary    #FFFFFF — headings, main labels
  --text-secondary  #9CA3AF — secondary labels, descriptions
  --text-muted      #6B7280 — hints, placeholders, footer
  --text-on-primary #0F1117 — text on top of the green primary

Borders
  --border        #2A2D3E — card borders, input borders
  --border-focus  #2EE89E — focused input border

Status
  --ok       #2EE89E   --ok-text    (same in dark)
  --warn     #F59E0B
  --error    #EF4444   --error-text #FCA5A5
  --offline  #6B7280

Status background tints
  --tint-green   #162318
  --tint-orange  #231A14
  --tint-red     #1F1215

Danger buttons (decline / delete / stop)
  --danger-bg      #3D1515
  --danger-border  #7F1D1D
  --danger-text    #FCA5A5
```

### Light mode (overrides only)

Values not listed here inherit from the dark palette.

```
--bg-primary  #F4F6FA      --text-primary   #111827
--bg-card     #FFFFFF      --text-secondary #4B5563
--bg-input    #F3F4F6      --text-muted     #9CA3AF
--bg-chip     #E9ECF2      --border         #E5E7EB

--ok-text     #1BC47D  (darker green for contrast on light tints)
--error-text  #DC2626

--tint-green  #DCFCE7      --danger-bg     #FEE2E2
--tint-orange #FFF7ED      --danger-border #EF4444
--tint-red    #FEF2F2      --danger-text   #DC2626
```

### Usage

```css
/* ✅ Correct — adapts to light/dark automatically */
.my-block { background: var(--bg-card); color: var(--text-secondary); }

/* ❌ Wrong — locked to one mode, invisible to future palette changes */
.my-block { background: #161B27; color: #9CA3AF; }
```

The one hardcoded exception: `.qr` keeps a white background in both themes, because a QR code needs its white quiet zone to scan reliably.

---

## Spacing

4-point grid. All values are multiples of 4.

```
--space-xs   4px   — tiny gaps, QR padding
--space-sm   8px   — gap between inline elements, form field gap
--space-md  16px   — standard padding, grid gap (default)
--space-lg  24px   — card padding, page padding, section spacing
--space-xl  32px   — generous vertical space (footer)
--space-xxl 48px   — hero spacing
```

```css
.card { padding: var(--space-lg); }
.cards { gap: var(--space-md); }
```

---

## Typography

Same named scale as the EVCharging app. `h1`/`h2`/`h3` are styled at the element level (plain `<h2>` in a `.cshtml` just works); the rest are utility classes to put on any element.

| Name | Size | Weight | Line height | Use case |
|---|---|---|---|---|
| `.display` | 36 | 800 | 44 | Hero numbers |
| `h1` / `.h1` | 28 | 800 | 34 | Page headings |
| `h2` / `.h2` | 22 | 800 | 28 | Section headings, plan names |
| `h3` / `.h3` | 18 | 700 | 24 | Card headings |
| `.stat-value` | 20 | 800 | 26 | Prices, stat values (`.price` uses this size) |
| `.body-lg` | 16 | 400 | 24 | Standard body copy (`.lead`) |
| `.body` | 15 | 400 | 22 | Secondary body copy, inputs, buttons |
| `.body-md` | 14 | 400 | 20 | Smaller descriptions, nav links |
| `.small` | 13 | 600 | 18 | Form labels |
| `.label` | 12 | 600 | 16 | Status pills, badges |
| `.caption` | 11 | 400 | 15 | Timestamps, fine print |
| `.tag` | 11 | 600 | 15 | Uppercase section tags (letter-spacing 1.5) |

`.muted` sets `color: var(--text-secondary)` and combines with any preset.

---

## Radius

```
--radius-sm    8px  — small badges, tight chips
--radius-md   12px  — inputs, alerts, QR images
--radius-btn  14px  — all buttons
--radius-lg   16px  — standard cards and panels
--radius-xl   20px  — large overlays
--radius-full 999px — pills (status badges)
```

---

## Component classes

The Razor markup equivalent of the `ui/` atoms. All of these are plain CSS classes — no partials or tag helpers required.

### Buttons

Variants mirror the EVCharging `Button` atom:

| Class | EVCharging equivalent | Look | Use for |
|---|---|---|---|
| `class="primary"` | `variant="primary"` | Green fill, dark text | The main action on a page (Buy, Log in, Confirm) |
| *(no class)* | `variant="secondary"` | Input-colored fill, border | Neutral actions |
| `class="danger"` | `variant="destructive"` | Dark-red fill, red border/text | Decline, delete, stop |
| `class="link-button"` | `variant="ghost"` | No fill, text only | Log out, minor actions inside text |

```html
<button type="submit" class="primary">Buy</button>
<button type="submit" name="outcome" value="declined" class="danger">✖ Simulate declined</button>
<a class="button primary" href="/Cards">Go to my card →</a>   <!-- link styled as button -->
```

### Card / Panel

```html
<div class="cards">          <!-- responsive flex grid, 16px gap -->
  <div class="card">…</div>  <!-- 250px card: bg-card, border, radius-lg, padding 24 -->
</div>

<div class="panel">…</div>   <!-- full-width variant of card -->
```

### Alerts

```html
<div class="alert">Something failed</div>     <!-- red tint -->
<div class="success">It worked</div>          <!-- green tint -->
```

### Status pills

`class="status status-<value>"` where the value is the lowercased API status. Colors are pre-mapped:

| Statuses | Color |
|---|---|
| `active`, `succeeded` | green |
| `pending` | amber |
| `blocked`, `declined`, `deleted`, `timedout` | red |
| `suspended`, `refunded` | neutral gray |

```html
<span class="status status-@card.Status.ToLower()">@card.Status</span>
```

### Forms

```html
<form method="post" class="form">                <!-- vertical, 360px max width -->
  <label>Email <input type="email" name="email" /></label>
  <button type="submit" class="primary">Log in</button>
</form>
```

Inputs get `--bg-input`, `--border`, and a green `--border-focus` on focus automatically.

---

## Rules

1. **Tokens over magic numbers.** Colors, spacing, and radii in any new CSS must come from `var(--…)` tokens, not hardcoded values.
2. **Never hardcode a hex color in markup or new CSS** — if a needed color doesn't exist, add a token to `:root` (and its light override if it differs), then use it.
3. **Both modes, one edit.** When adding a token, ask whether it needs a light-mode override; text and tints usually do, brand colors usually don't.
4. **Reuse the component classes** (`.card`, `.status`, `.alert`, button variants) before inventing new ones. If a new pattern repeats on two pages, promote it to a class in `site.css` and document it here.
5. **Exception:** the QR image background stays white in both themes (scannability).
