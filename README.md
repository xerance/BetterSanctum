# BetterSanctum

A Sanctum overlay for [ExileApi](https://github.com/exApiTools/ExileApi-Compiled).

Reads the floor map, rates every room from your own tier values, and frames the best
route from where you stand to the boss. Also marks guard spawners and hazard telegraphs
in the room you are fighting in.

## Credit

This is a fork of [exApiTools/BetterSanctum](https://github.com/exApiTools/BetterSanctum),
which is the origin of the floor map overlay, the tier and profile system, and the
duplicate-run reward marking.

The in-room spawner and hazard overlay is ported from
[deafwave/PathfindSanctum](https://github.com/deafwave/PathfindSanctum), itself a fork of
the above, which is also where the idea of scoring whole routes rather than colouring
individual connections comes from.

Original donation addresses, carried over from both:

BTC: bc1qke67907s6d5k3cm7lx7m020chyjp9e8ysfwtuz

ETH: 0x3A37B3f57453555C2ceabb1a2A4f55E0eB969105

## How routing works

You give every currency, room type and affliction a value from 0 to 8:

| 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|---|---|---|---|---|---|---|---|---|
| always route through | +100 | +10 | +5 | neutral | -5 | -10 | -120 | never route through |

You enter exactly one room per layer, so every route holds the same number of rooms and
their totals compare directly. Each route is scored by how many rooms of each tier it
holds; the highest total wins. A route reaching a 0 beats every route that does not,
including through anything marked 8.

Currency is rated per reward slot, only a room's best slot counts, and the third slot
counts twice because it pays double.

## Building

Put the source in `Plugins/Source/BetterSanctum` and launch the HUD, which compiles it.
