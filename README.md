# OttoPay

![OttoPay. Your gold, always with you.](https://raw.githubusercontent.com/potto007/OttoPay/master/docs/images/ottopay-title.png)

**Version 1.6.3**, built and Harmony-checked against Valheim 1.0.16.

A bank balance for your coins, run by the merchants of Valheim.

Talk to any merchant and press the "Join Merchant Bank" button in the store window. From then on, every coin you pick up goes to your Merchant Bank balance instead of your inventory, where it takes no slot and weighs nothing, and any merchant draws on that balance when you buy. Until you join, coins behave exactly as they do in vanilla.

Think of it as a mystical tap-to-pay. The coins are not on your belt - the merchants hold them for you, and the AuraPay network settles your payments on your behalf. AuraPay is how other mods, starting with OttoAura, charge your balance for their services.

## Better with OttoAura

OttoPay holds your coins. [OttoAura](https://thunderstore.io/c/valheim/p/potto007/OttoAura/) gives you things to spend them on. Turn AuraPay on in the inventory window and you get:

- **Ward repairs.** Stand inside a ward you own or are permitted on and it heals you and repairs the gear you wear, one coin per item per tick by default.
- **AuraBoost.** Roads and trails drain less stamina: half on dirt paths, wood and metal, none on paved roads and stone. It costs nothing.
- **AuraMove.** For a small fee the Merchant Guild moves a chest, a crafting station or a piece of furniture a short distance, contents intact, so you don't have to smash it and build it again.
- **AuraTrade.** Inside your ward, drop rubies, amber and other valuables on your balance to sell them, minus a small AuraPay fee.

OttoAura lists OttoPay as a dependency, so a mod manager installs both.

## Coming from CurrencyPocket

OttoPay continues Azumatt's [CurrencyPocket](https://thunderstore.io/c/valheim/p/Azumatt/CurrencyPocket/), which is now deprecated. Your pocket balance carries over, and a character with coins in it already counts as a Merchant Bank member. Remove CurrencyPocket first: the two mods cannot load side by side.

## What you get

- The inventory window shows your coin balance next to your armor and weight.
- Coins you pick up go straight to the balance, and you still see the normal pickup message.
- Merchants read your balance when you buy, and RapidLoadouts reads it too.
- A withdraw button opens the split dialog so you can choose how many coins to take, or you can hold Ctrl when you click it to take them all.
- Drop coins on your balance to deposit them again.
- An AuraPay toggle lets OttoAura charge your balance for its services. It is off until you turn it on, and the choice is saved with the character.

## Client and server

OttoPay is a client mod, so it works on a server that does not have it.

If you also install it on the server, the server config wins and the clients follow it. Clients without the mod can still join.

## Configuration

The config file is `potto007.OttoPay.cfg` in the BepInEx config folder, and the mod reloads it when the file changes.

| Setting | Default | Meaning |
| --- | --- | --- |
| Lock Configuration | On | Only server admins can change the config, and the setting is synced with the server. |

## Other mods

- OttoAura uses AuraPay for everything listed under [Better with OttoAura](#better-with-ottoaura). Other mods can register their own AuraPay services with OttoPayApi.RegisterAuraService so they appear in the AuraPay tooltip.
- CurrencyPocket by Azumatt cannot run next to OttoPay. Both mods keep the coins under the same player data key, so BepInEx refuses to load OttoPay beside it. A balance you built up with CurrencyPocket carries over, and a character with coins in it already counts as a bank member.
- Jewelcrafting and QuickStackStore move the inventory panels too. OttoPay moves them to make room for the balance, and with one of these installed the coin balance keeps its place while the other panels shift instead.
- ExtraSlots keeps lists of panels. OttoPay removes its own panel from those lists when they hold more panels than the row count allows.

## Credits

OttoPay started from CurrencyPocket 1.0.13 by Azumatt, under the MIT No Attribution license. The Merchant Bank Network, the join button, the withdraw dialog, AuraPay and the tooltips are new, so the bugs in them are mine. Report them at https://github.com/potto007/OttoPay.
