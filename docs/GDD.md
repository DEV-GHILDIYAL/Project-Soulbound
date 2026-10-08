# Project Soulbound — Initial Game Design Document

Status: concept and MVP planning. No gameplay implementation is claimed by this document.

## Vision

A maze should feel dangerous because the player must choose when to explore, spend ammunition, solve a puzzle, or secure a soul. Surviving a fight is only half the task: abandoning the soul can make the next encounter harder.

### Confirmed concept

- Replayable maze survival/puzzle gameplay.
- Chests and collectible items.
- A pistol with limited ammunition.
- Enemies roam the maze.
- A defeated enemy becomes a soul.
- An uncaptured soul becomes two upgraded enemies after 10 seconds; speed, damage, and attack type change.

### Design pillars

1. **Meaningful scarcity:** bullets should create choices between fighting and avoidance.
2. **Readable danger:** souls, timers, enemy attacks, and puzzle goals must be understandable.
3. **Risk and reward:** valuable chests and objectives draw the player into dangerous routes.
4. **Replayable decisions:** changing placements should alter routes and resource choices.

## Proposed core loop

1. Enter the maze with a pistol and a small ammunition supply.
2. Explore corridors and locate chests, puzzle clues, and exit objectives.
3. Avoid enemies or spend ammunition to defeat them.
4. Chase the fleeing soul and stay close while holding capture for 3 seconds before its timer expires.
5. Solve puzzles to open key chests and collect all 3 keys for the exit door.
6. Open the exit door to win, or die and restart with fresh run placements.

Example: an enemy guards a chest. The player can detour, lure it away, or shoot it. Shooting gives access to the chest but also leaves a timed soul. Looting first risks allowing that soul to produce two new enemies.

## MVP scope

Proposed first playable target: one small maze, one enemy type, one pistol, one capture interaction, and one complete escape objective.

| System | Minimum behavior |
| --- | --- |
| Player | Movement, first-person camera, limited health/healing, interaction |
| Maze | Handcrafted corridors with a start, exit, and multiple routes |
| Pistol | Aim/fire, finite ammunition, clear empty feedback |
| Enemy | Roam, detect/chase, attack, die; descendants gain aggression |
| Soul | Spawn on death, flee player, show countdown, 3-second capture hold, duplicate on expiry |
| Chests/items | One-use chests with ammunition and an objective item |
| Puzzle | Solve puzzles to open key chests; collect 3 keys to open the exit door |
| Run flow | Start, win, lose, restart |
| Feedback | Health, ammunition, objective state, soul warning, interaction prompts |
| Replayability | Vary enemy, chest, and objective placements within validated locations |

Target a short run of roughly 5–10 minutes as an initial playtest hypothesis. Tune from observations.

Defer procedural maze generation, multiple weapons, unrelated enemy variants beyond required descendant attack changes, crafting, permanent upgrades, multiplayer, and extensive story content until the core loop works.

## Puzzle ideas

| Idea | Player task | Scope |
| --- | --- | --- |
| Key and locked exit | Solve chest puzzles, collect 3 keys, and open the exit door | MVP default |
| Symbol sequence | Match corridor clues to a sequence of switches | Alternative MVP puzzle |
| Pressure plates | Place movable objects on plates to open a route | Later prototype |
| Soul-powered door | Spend captured souls to open a shortcut | Later; changes capture economy |
| Timed gate | Activate a switch and navigate to a gate before it closes | Later; test alongside combat pressure |

Use one MVP puzzle. Provide clear feedback when it is solved. Required items must be reachable before the lock they open; avoid objectives that require more ammunition than the run guarantees.

## Replayability

Begin with a fixed maze and vary placements. Keep start and exit safe enough to make runs understandable, and choose objective locations from prevalidated reachable points. Random loot should preserve a minimum useful resource supply.

Later, consider seeded layouts, alternate puzzle configurations, difficulty presets, and optional route challenges. Record a seed when randomness is introduced so problematic runs can be reproduced.

## Open design decisions

| Decision | Suggested prototype default | Why it matters |
| --- | --- | --- |
| Camera and dimension | First-person camera (confirmed) | Changes movement, aiming, visibility, and assets |
| Platform and controls | Desktop keyboard/mouse first | Sets control and UI scope |
| Win condition | Collect 3 keys to open the exit door | Gives the run a clear end |
| Capture method | Stay close to a fleeing soul and hold capture for 3 seconds (confirmed) | Creates exposure after a kill |
| Capture cost/reward | Free capture; no spendable reward initially | Isolates containment as the incentive |
| Soul behavior | Flees from the player through the maze (confirmed) | Keeps the timer readable |
| Duplication escalation | No gameplay cap currently specified; upgrades increase aggression | Limited ammo, health, and healing eventually make unchecked growth lethal |
| Ammunition model | One finite ammo pool; magazines/reloading deferred | Reduces prototype scope |
| Persistence | Reset health, items, ammunition, and enemies each run | Supports a simple replay loop |

These defaults need confirmation through design review and playtesting. Camera and capture duration are confirmed; remaining economy, numeric balance, and progression details are provisional.

## Main design risks

- Repeated uncaptured souls can produce an overwhelming enemy population: unchecked escalation is intentional; test pacing and performance early.
- Too little ammunition can make required combat impossible: retain viable avoidance routes and resource guarantees.
- Capture may feel like routine cleanup: tune exposure and positioning so it creates meaningful choices.
- Simultaneous puzzle and timer pressure may be confusing: introduce each rule clearly before combining them.
- Random placement can create unfair runs: validate reachability and resource access.

## Confirmed design update

- First-person camera.
- Souls keep trying to escape even during capture and deal no damage. Stay close and hold capture continuously for 3 seconds; damage from another enemy resets progress. An uncaptured soul duplicates 10 seconds after spawning.
- Expired souls create two upgraded, more aggressive enemies with increased speed, slightly increased damage, and a changed attack type. Repeated duplication escalates danger; no gameplay enemy cap is currently specified.
- Ammunition, health, and healing are limited. Unchecked duplication can eventually overwhelm and kill the player.
- Collect 3 keys from puzzle-locked chests and open the exit door to win. Solve the associated puzzle before opening each key chest.
- Still undecided: soul flee speed, capture range, player movement during capture, exact upgrade amounts and attack types, puzzle designs, resource quantities, and controls.

These confirmed choices override earlier prototype suggestions; remaining unconfirmed details are proposals. This update changes documentation only.
