# Philo Lens

A quick readiness check for raid and party leaders: open any nearby player's profile card and see
their spec, Ability Score, season strength, equipped Battle Imagines, season talent and active
food and serum buffs, without asking them to screenshot anything.

## Getting started

1. Install Philo Lens from the launcher's plugin list and launch the game modded.
2. Open a nearby player's profile card.
3. Press the **Lens** button on the card to open the Lens window.
4. Close the card and the Lens window closes with it.

## Reading the window

- **Class and spec**, for example *Verdant Oracle · Lifebind*. While only the class is known, it
  says *Spec not seen yet*.
- **Ability Score** and the **season strength** stat (in season 3, *Illusion-Breaking Strength*).
- **Battle Imagines**: each equipped Imagine with its tier, for example
  *Phantom Arachnocrab · Tier 5*.
- **Season talent**: the board the player runs this season (in season 3, *Deep Slumber*).
- **Food & Potions**: active food, serum and *Foodie's Grace* buffs with their effect and the time
  left, counting down live.

Season-specific names come from the game itself, so they follow the current season and your game
language.

## What it can and can't see

- Everything except the class and Ability Score is only known for players **near you**: the game
  only sends those details for players in range. For someone far away, the window shows their class
  and the Ability Score from their profile.
- The spec is usually known as soon as a player comes into range, even in town. If it isn't, it
  appears once they use a spec skill nearby.
- While a player is transformed by a Battle Imagine, their real class stays on screen.

## Languages

The window's labels come in English, Indonesian, Japanese, Thai and Filipino, following your game
language. The non-English labels are machine-generated and haven't been checked by native
speakers yet, so some wording may be rough. Corrections are welcome on the
[project page](https://github.com/philomelch/bpsr-philo-lens).

## Privacy

Philo Lens only reads what the game already sends to your client about players near you. It
stores nothing, sends nothing anywhere, and performs no game actions.
