# OttoPay

![OttoPay. Your gold, always with you.](https://raw.githubusercontent.com/potto007/OttoPay/master/docs/images/ottopay-title.png)

A bank balance for your coins, run by the merchants of Valheim.

Talk to any merchant and press the "Join Merchant Bank" button in the store window. From then on, every coin you pick up goes to your Merchant Bank balance instead of your inventory, where it takes no slot and weighs nothing, and any merchant draws on that balance when you buy. Until you join, coins behave exactly as they do in vanilla.

Think of it as a mystical tap-to-pay. The coins are not on your belt - the merchants hold them for you, and the AuraPay network settles your payments on your behalf.

## What you get

- The inventory window shows your coin balance next to your armor and weight.
- Coins you pick up go straight to the balance, and you still see the normal pickup message.
- Merchants read your balance when you buy, and RapidLoadouts reads it too.
- A withdraw button opens the split dialog so you can choose how many coins to take, or you can hold Ctrl when you click it to take them all.
- Drop coins on the coin icon to deposit them again. Items with a coin value, like rubies and amber, can be dropped there as well and are deposited for their value, which the config controls.
- An AuraPay toggle lets an OttoAura ward charge your balance to repair the gear you wear. It is off until you turn it on, and the choice is saved with the character.

## Client and server

OttoPay is a client mod, so it works on a server that does not have it.

If you also install it on the server, the server config wins and the clients follow it. Clients without the mod can still join.

## Configuration

The config file is `potto007.OttoPay.cfg` in the BepInEx config folder, and the mod reloads it when the file changes.

| Setting | Default | Meaning |
| --- | --- | --- |
| Lock Configuration | On | Only server admins can change the config, and the setting is synced with the server. |
| AllowValuableItems | true | Items with a coin value can be dropped on the coin icon and deposited for that value. |
| AllowedValuablePrefabs | empty | Comma separated prefab names, for example `Ruby,Amber`. When set, only those items and coins can be deposited. When empty, every valuable item can be. |

## Other mods

- OttoAura draws on the balance through AuraPay when it charges for aura repairs.
- CurrencyPocket by Azumatt cannot run next to OttoPay. Both mods keep the coins under the same player data key, so BepInEx refuses to load OttoPay beside it. A balance you built up with CurrencyPocket carries over, and a character with coins in it already counts as a bank member.
- Jewelcrafting and QuickStackStore move the inventory panels too. OttoPay moves them to make room for the balance, and with one of these installed the coin balance keeps its place while the other panels shift instead.
- ExtraSlots keeps lists of panels. OttoPay removes its own panel from those lists when they hold more panels than the row count allows.

## Credits

OttoPay started from CurrencyPocket 1.0.13 by Azumatt, under the MIT No Attribution license. The Merchant Bank Network, the join button, the withdraw dialog, AuraPay and the tooltips are new, so the bugs in them are mine. Report them at https://github.com/potto007/OttoPay.
