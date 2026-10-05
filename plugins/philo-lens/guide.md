# Philo Lens

A quick readiness check for raid and party leaders: open a player's profile card and see their
spec, Ability Score, season strength, equipped Battle Imagines, season talent and active food and
serum buffs, plus everyone in their party, without asking anyone to screenshot anything.

## Getting started

1. Install Philo Lens from the launcher's plugin list and launch the game modded.
2. Open a player's profile card.
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
- **Party**, to the right when the player is in one: how full it is (for example *Party (4/5)*; a
  raid counts out of 20), how many tanks, healers and DPS it has, and a table of every member with
  their name, class (and spec when known), Ability Score and season strength (headed by the
  stat's initials, *IBS* in season 3).

Season-specific names come from the game itself, so they follow the current season and your game
language.

## What it can and can't see

- The spec, Battle Imagines, season talent and food & potions are only known for players **near
  you**: the game only sends those details for players in range. For someone far away, the window
  shows their class, Ability Score and season strength from their profile.
- The spec is usually known as soon as a player comes into range, even in town. If it isn't, it
  appears once they use a spec skill nearby.
- While a player is transformed by a Battle Imagine, their real class stays on screen.
- The **party** works at any distance, since it comes with the player's profile. A member's spec
  shows only when that member is near you (or was seen using a spec skill); otherwise just their
  class.
- The party's copy of a member's stats can lag a little behind that member's own profile. The
  player whose card you opened always shows their current values.
- A raid is shown as one list in party order, not split into its groups: the profile's copy of who
  is in which group can be out of date.

## Languages

The window's labels come in English, Indonesian, Japanese, Thai and Filipino, following your game
language. The non-English labels are machine-generated and haven't been checked by native
speakers yet, so some wording may be rough. Corrections are welcome on the
[project page](https://github.com/philomelch/bpsr-philo-lens).

## Privacy

Philo Lens only reads what the game already sends to your client: details about players near you,
and the profile the game loads when you open someone's card. It never asks the server for anything,
stores nothing, sends nothing anywhere, and performs no game actions.
