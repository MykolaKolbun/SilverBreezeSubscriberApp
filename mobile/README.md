# Parking Pass — Mobile App (Expo / React Native)

React Native client for the parking subscription platform, implementing the
**Voltway** hifi design from [`../design_handoff_parking_pass_app/`](../design_handoff_parking_pass_app/README.md)
(four screens: Plans, Payment, Pass, Profile + floating two-tab nav).

## Run

```bash
cd mobile
npm install
npm start        # Expo dev server (scan QR with Expo Go)
npm run web      # or run in the browser
```

## Structure

Deliberately flat — no navigation library yet (the app is four screens with a
custom floating tab bar, driven by a single `screen` state value, exactly like
the design prototype):

```
App.tsx                      # font loading, providers, screen switch
src/
  theme.ts                   # Voltway design tokens (dark + light palettes)
  plans.ts                   # plan catalog, pricing and date rules
  state.tsx                  # one context: subscriptions, vehicles, payment, theme
  components/
    ui.tsx                   # Overline, Radio, selectable-card style, PulseDot
    icons.tsx                # hand-written Lucide-style SVG icons
    BottomNav.tsx            # floating blurred pill nav (Pass / Profile)
    DateField.tsx            # native date picker (DOM <input type=date> on web)
    QrPlaceholder.tsx        # decorative QR graphic from the design
  screens/
    PlansScreen.tsx          # plan choice, billing toggle, start date
    PaymentScreen.tsx        # amount, Apple Pay / card, 3-state confirm button
    PassScreen.tsx           # hero pass card, upcoming passes, Manage plan
    ProfileScreen.tsx        # vehicles carousel, contact, settings, theme
```

## Behavior rules (from the handoff)

- Annual price = monthly × 10 (2 months free).
- End date = start + 1 billing period − 1 day (Jul 12 monthly → Aug 11).
- "Manage plan" pre-fills the start date as (latest end date across all
  subscriptions) + 1 day, so periods never overlap; a future-dated purchase
  shows under **Upcoming** on the Pass screen.
- Max 3 vehicles; per-card draft state — nothing (including the Pass screen
  plate) updates until that card's **Save** is tapped.
- Card payment details are never persisted; cleared after every checkout.
- Theme (dark default) is the only persisted state (AsyncStorage).
- No cancel-subscription flow — intentionally removed in the design.

## Still stubbed / next steps

- **Backend wiring**: plans, auth, payments, and the pass are local state.
  The .NET backend (`../backend`) already exposes `/plans`, `/payments`,
  `/parking-cards/{id}/qr` etc. — replace `src/plans.ts` data and the
  payment simulation in `src/state.tsx` with API calls.
- **QR code**: `QrPlaceholder` is decorative; render the real QR
  (`GET /parking-cards/{id}/qr`) or encode the pass token client-side.
- **Payments**: the confirm button simulates processing (1.1 s) → success →
  auto-navigate; hook up the real payment provider + webhook flow.
- **License plate recognition**: copy only; no integration.
- Android blur: the nav bar falls back to a translucent background
  (expo-blur on Android needs `BlurTargetView` wiring).
