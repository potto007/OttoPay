# Changelog

## 1.4.0

- AuraPay Pathwalk. The magic of AuraPay makes you feel invigorated and lighter on your feet!
  As a result, you experience less stamina drain when you don't stray off the roads and
  trails. It works once you have joined the Merchant Bank and while AuraPay is on, and shows a
  status icon while it is working.
- New server synced settings under `2 - AuraPay Pathwalk`: `Enabled`, `StaminaUsageTrail`
  (default 0.5), `StaminaUsageRoad` (default 0) and `ShowStatusIcon`.
- The AuraPay toggle shows without OttoAura while Pathwalk is enabled, and its tooltip lists
  what AuraPay does for you.
- Pathwalk does not stack with run stamina discounts from other mods' own status effects. The
  bigger discount wins, while food, meads and Moder's power still stack as usual.

## 1.3.0

- Every tooltip now uses the inventory slot box. Some controls got their tooltip before the
  inventory finished loading and before any slot existed, so they ended up with an unboxed style.
- The coin icon no longer answers hovers meant for the buttons on top of it, and it no
  longer rewrites the first inventory slot's tooltip.
- The withdraw tooltip was wrong, because the button opens a dialog to take some coins, not all.
- All player facing text now calls your coins a Merchant Bank balance instead of a pouch.
- New README, with the config table and the compatibility notes.
- New icon and title art.

## 1.2.0

- Hover tooltips on the join button, the withdraw button, the AuraPay toggle and the coin
  icon.
- The joining message now shows inside the store window. The heads up display drew it
  behind the shop, so you only saw it if you closed the shop in time.
- The toggle reads AuraPay, not Aura.

## 1.1.2

- The join button sits clear of the store panel, and its label no longer wraps.

## 1.1.1

- The join button was invisible. It is cloned from the Buy button, which is stretch
  anchored, so the copied sizeDelta was an inset and the width came out negative.

## 1.1.0

- The join button was a second, unlabelled coin icon, because it was cloned from the Sell
  button, which has no text child. It is cloned from Buy now and reads "Join Merchant Bank".

## 1.0.0

- First release, from CurrencyPocket 1.0.13 by Azumatt, for Valheim 1.0.7.
- Coins go to the bank only after you join the Merchant Bank Network at a merchant, and until
  then they behave as in vanilla.
- AuraPay toggle, so OttoAura can charge your balance for repairs.
