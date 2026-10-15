# I Have Too Much Item Bloat

I mean enough said, right? Starstorm 2, Aetherium, Sandswept, the mod that lets you turn on all DLC at the same time...

I love these mods, and I love or like most of the items. I disable a few with the rulebook, but the fact of the matter
is; with a big enough modlist, the item pool gets so large that finding the one item you really need or want for a
specific build wants turns into a pipe dream. I could disable more items of course, but that just didn't feel right.
You NEED bad items in your pool for balance (and for a good scrap target), but being on loop 3 without a single
god damn hoppoo feather on a melee survivor is enough to make you want to blow your brains out.

So I made this mod. Right now it is pretty simple, but I intend to tweak and expand upon it a bit over time as I get
a feel for what I actually want.

## What it does

### Shrine of Wishes

![A Shrine of Wishes on a stage](https://raw.githubusercontent.com/Delfofthebla/IHaveTooMuchItemBloat/master/docs/screenshots/WishShrine.png)

A new shrine that can spawn, with up to 2 per stage. One is guaranteed to spawn on Gilded Coast. Despite having shrine
in the name, it has its own category in the stage director (like printers and scrappers do). This was done so that it
doesn't push out vanilla shrines and other mods' shrines don't push ours out either. This was a deliberate choice so
that it could spawn reliably and consume the appropriate amount of director credits for the value that it brings.

Opening the shrine shows you a selection of 10 random items to wish for, mostly commons and uncommons, with the odd
legendary. Every player gets their own selection at each shrine. A wish costs 40 gold before difficulty scaling (between
a small and a large chest), and each player gets 2 wishes per shrine. Like a Shrine of Chance, the price goes up with
each use, but only your own uses raise your price.

![The Make a Wish panel](https://raw.githubusercontent.com/Delfofthebla/IHaveTooMuchItemBloat/master/docs/screenshots/WishPanel.png)

You can hold 3 wishes at once, of any rarity. Each use of the shrine does one of these:

- **Wish:** put an item from the selection into an empty slot.
- **Strengthen:** pick one of your existing wishes to make it come true sooner.
- **Replace:** put an item from the selection into a filled slot. The new wish starts from scratch.

### Wishes

A wish doesn't hand you the item. It is a steadily ramping chance to _swap_ with one of your random drops and become the
desired item. Whenever a chest, a Shrine of Chance, or a "?" shop drops an item of the same rarity as one of your wishes,
that wish _can_ take its place. Rarity never changes: a common wish only ever replaces a common drop. Only drops that
**could** have been the wished item count, so a Healing chest is never going to give you a pair of Lens-Maker's Glasses.
Sorry bud. Choice containers like a Void Potential can technically grant wishes too, but that's currently off by default.

A fresh wish's chance is 120% of that item's odds from the same source in an unmodded game, so no matter how bloated the
pool is, your odds will be just a little bit better than they would in vanilla. As you continue to open chests and fail
to find your desired item, the attempts add to the wishes **desperation** counter. With each bit of added desperation, 
the chance to have it granted climbs higher and higher until it is guaranteed. A desperation of 16 for commons, 10 for
uncommons, and 8 for legendaries. Category chests count for more, since they narrow the pool. Holding several wishes of
the same rarity won't make all of them come true any faster either; desperation is evenly split between them.

When a wish comes true, everyone sees it in chat and the wish is used up.

![The wish row on the scoreboard](https://raw.githubusercontent.com/Delfofthebla/IHaveTooMuchItemBloat/master/docs/screenshots/WishHud.png)

Your wishes show under your items while the scoreboard is open. Hover over one to see its desperation.

If an item drop was random, but just so happened to be your desired item (a "natural" drop, if you will) it is still
counted as your wish becoming true, but 40% of the desperation on the wish will be split among your other wishes of
that rarity as a sort of "pity bonus". The _Natural Drops_ config setting can instead keep the wish and give your other
wishes the desperation, grant it without passing anything on, or grant it and start it over. I'm still playing around
with what feels best but these are the current ideas.

Printers, shop terminals showing their item, Artifact of Command, and Artifact of Delusion's re-drops are never touched.

## Don't like my numbers?

Yeah me neither. I'm still working on it. Luckily everything above is a setting, and you can change it in game with
[Risk Of Options](https://thunderstore.io/package/Rune580/Risk_Of_Options/) or in the config file:

- **Wish Shrine:** how often it spawns, how many per stage, its cost and price increase, uses, and selection size.
- **Wish Shrine Offer Weights:** how often each rarity shows up in the selection. Lunar, void and equipment items
  aren't offered by default, but wishes for them work just the same.
- **Wishes:** how many you can hold, the starting chance, how much strengthening adds, and what natural drops do.
- **Guaranteed At:** how much desperation each rarity needs to be guaranteed.
- **Drop Sources:** which kinds of drops can grant wishes.

## Multiplayer

Every player needs the same version of this mod. The host's settings apply to everyone.

Each player has their own wishes, their own selection at every shrine, and their own uses. A drop grants the wishes
of the player who opened it. There is a config option to make the shrine uses apply to the whole lobby, like most other
interactables, if you do not like how the shrine breaks vanilla conventions.

## Compatibility & Disclaimers

### This is an BETA at best. InDev/Alpha at worst.

I'm mostly just making this for myself and my friend. I don't know if anyone would even want a mod like this. I don't
even know that ***I*** want a mod like this. But well, here it is. I can vouch for it working in singleplayer and in a
2-player lobby with the exact same mod profile, but it's still very new.

### Game version

**This mod was built and tested on game version 1.4.1, from before the new DLC. It is completely untested on 1.5.0.**

The Hallowed Concepts update broke my entire modlist, so I am not currently playing on it at all and likely won't be
until the dust settles. I would be genuinely surprised if this mod works at all on the latest version. Don't expect
a 1.5.0 update for a while. Besides, the entire point of this mod is to address item bloat caused by too many mods,
and all of the item mods are broken. If you aren't playing on a downgraded version of the game, do not download this
mod. And be careful about updating any of your other mods to anything released after 10/7/26, but you should know that
already.

### DLC

Owning DLC _shouldn't_ be required. The wish menu is built from chef's recipe menu from Alloyed Collective, but the
game installs every DLC's files for everyone, owned or not, so it _should_ work even if you or a player in your
lobby does not have it.

Items from a DLC that's off for the run (or that you don't own) aren't in the item pool, so they're never offered and
never drop. Their associated settings (like void tiers without owning Survivors of the Void), simply do nothing.

If a game update removes or changes something this mod relies on, the broken part _should_ turn itself off and log an
error, while everything else keeps working. If the Shrine of Wishes can't be built, it never spawns. Please report the
error from `LogOutput.log` on the GitHub if you see one.

### Assets

Listen, I'm not a 2D or 3D artist. I'm a programmer and AI deleted my entire career. The shrine & UI are just
re-used/patched vanilla & DLC assets. The shrine is the shrine of combat with a runtime-recolored shrine of the mountain
icon, and the Wish Panel is just Chef's recipe panel. I might try and polish both up at some point, but my skillset is
limited here. At least it's not some AI Slop prefab! If you like the mod and want to make me something I wouldn't say
no, but for now all I've got is this lazy vanilla asset swap. It's the best I can do.

### Other mods

Works with modded items: anything in the run's item pool can be offered and wished for.

Wishes are saved with the run, so [ProperSave](https://thunderstore.io/package/KingEnderBrine/ProperSave/) keeps them.
If you remove a mod between saving and loading, wishes for its items are dropped.

## Your Shrine Looks Like Crap

