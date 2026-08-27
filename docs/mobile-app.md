# Mobile App

React Native / Expo (SDK 57) customer app. State lives in `mobile/src/state.tsx`
(`AppProvider`); the REST client is `mobile/src/api/client.ts`.

> Expo v57 changed APIs — read `https://docs.expo.dev/versions/v57.0.0/` before editing.

## Screens

- **Auth** (`AuthScreen`) — passwordless email OTP (email → 6-digit code). No name fields
  here by design; the profile is filled after sign-in.
- **Pass** (`PassScreen`) — the active pass with an on-device QR (react-native-qrcode-svg),
  works offline.
- **Plans / Payment** — pick a tariff, pay via the iPay hosted page
  (`WebBrowser.openAuthSessionAsync`, deep link `silverbreeze://payment`), poll the
  server-confirmed status, refresh cards.
- **Profile** (`ProfileScreen`) — identity, editable contact details (name + phone),
  vehicles (up to 3), settings (theme, language), history, sign-out. Footer shows
  `version · build · commit · date` from `buildInfo.ts`.
- **History** — transactions + receipt (PNG inline + PDF via share sheet).

## Offline-first bidirectional sync

On launch (after auth), `loadData` runs `Promise.all([syncProfile(true), syncVehicles(true)])`
plus plans + cards. Each sync **pushes local changes first, then pulls** server state.
State is cached in AsyncStorage so the app works offline and replays changes on the next
launch.

- **Cards** — pull-only; cached under `silverbreeze.cards` (QR renders on-device).
- **Vehicles** — bidirectional, cached under `silverbreeze.vehicles`. A `Vehicle` keeps a
  local numeric `id`, an optional `serverId` (backend Guid) and a `dirty` flag; deletes of
  synced vehicles queue their `serverId`. `pushVehicles` replays queued deletes then dirty
  create/updates (`POST`/`PUT`/`DELETE /vehicles`); `pullVehicles` makes the server list
  authoritative while keeping not-yet-pushed offline creations. Best-effort push also runs
  after each edit when online.
- **Profile** — bidirectional, cached under `silverbreeze.profile`. `pushProfile` `PUT`s
  `/users/{id}` (firstName/surname/mobile); `pullProfile` `GET`s it, keeping unpushed edits.

Endpoints used: `/users/{id}` (get/update), `/users/{id}/vehicles`, `/vehicles` (CRUD),
`/plans`, `/users/{id}/parking-cards`, `/payments`, `/users/{id}/payments`.

## Profile completion gate

Name (first name + surname) is required before buying a pass; **phone is optional**.

- After registration the user is prompted to complete their profile and sent to the Profile
  screen.
- `openPlans` and `confirmPayment` are gated: an incomplete profile shows a prompt and
  returns instead of proceeding.
- `profileComplete` in state drives a hint on the Profile screen while incomplete.

## Build

`npm run build:apk` in `mobile/` stamps `buildInfo.ts` (version, commit count as build
number, short SHA + dirty flag) then runs the Android release build. If the `android/`
folder was removed, regenerate it first with `npx expo prebuild -p android`. Verify the
installed build via the Profile footer (build number = commit count).
