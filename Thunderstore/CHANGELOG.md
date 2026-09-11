# Changelog

## 1.2.0

- Add hover tooltips to the join button, the withdraw button, the AuraPay toggle and the
  pouch itself.
- Show the joining message inside the store window, which draws in front of it. The heads
  up display message was hidden behind the shop interface.
- The AuraPay toggle now reads AuraPay rather than Aura.

## 1.1.2

- Move the join button clear of the store panel and stop its label wrapping.

## 1.1.1

- Fix the join button being invisible. Its width came out negative, because the Buy button
  it is cloned from is stretch anchored and its sizeDelta is an inset, not a width.

## 1.1.0

- Fix the Merchant Bank join button. It was cloned from the Sell button, which carries no
  text label, so it appeared as a second unlabelled coin icon. It now reads "Join Merchant
  Bank" and logs its position when it is created.

## 1.0.0

- First release built on CurrencyPocket 1.0.13 by Azumatt, for Valheim 1.0.7.
