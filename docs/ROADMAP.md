# Project Soulbound — MVP Roadmap

Status: planned work. This documentation pass implements no gameplay. Milestones are ordered by dependency; dates are intentionally unset until scope and available development time are known.

## 0. Resolve prototype choices

Camera is first person; capture is a 3-second hold near a fleeing soul; 3 keys open the exit door. Resolve platform, controls, puzzle designs, descendant upgrade scaling, and attack patterns. Review the proposed defaults in [GDD.md](GDD.md) and [MECHANICS.md](MECHANICS.md).

Done when: the prototype has one agreed control scheme, objective, and soul rule set, with provisional tuning values recorded.

## 1. Build an explorable maze

Create a small greybox maze with player movement, first-person camera, collisions, start, and exit. Use simple placeholder visuals and multiple routes.

Done when: the player can navigate the full maze, cannot cross walls, and can restart from a consistent start position.

## 2. Establish combat

Add health, a finite-ammunition pistol, and one enemy that roams, chases, attacks, and dies. Show health and ammunition.

Done when: combat consumes resources correctly, empty ammunition is understandable, enemies cannot attack through walls, and death reliably ends the run.

## 3. Prove the soul loop

Add fleeing souls with a 10-second countdown, continuous 3-second close-range capture, progress reset on enemy damage, duplication, safe spawn positions, descendant speed/damage/attack-type upgrades, and lifecycle cleanup.

Done when: every soul either captures or produces one pair with slightly upgraded aggression, repeated generations work, and pause/restart/end-of-run cannot leave stale timers or spawns. Verify the scenarios in [MECHANICS.md](MECHANICS.md).

This is the key design checkpoint: confirm that capturing souls makes combat more interesting before expanding content.

## 4. Complete one run

Add one-use chests, ammunition pickups, puzzle-locked key chests, a 3-key exit-door objective, objective feedback, win/lose screens, and restart.

Done when: a new player can understand the goal and finish or lose a complete run without developer intervention; the mandatory objective is always reachable.

## 5. Add controlled replayability

Vary enemy, chest, and objective locations within prevalidated points. Preserve a viable objective route and a minimum resource supply. Record run seeds if seeded randomness is used.

Done when: multiple placement configurations change route choices while remaining solvable, and a reported bad configuration can be reproduced.

## 6. Playtest and tune

Run short sessions with players who have not read the design. Observe:

- Whether they understand that a kill leaves a timed threat.
- Capture successes, expiry count, and deaths caused by duplication.
- Ammunition shortages, avoidance choices, and chest usefulness.
- Time to locate the objective and finish the run.
- Confusing prompts, unfair spawn locations, and performance during sustained duplication.

Done when: players can explain the soul rule after playing, finish a run through understandable choices, and replay without recurring objective or spawning failures.

## After the MVP

Procedural mazes are now part of the MVP and have a prototype. Consider additional puzzles, enemy types, weapons, soul rewards, story, sound/art production, and persistent progression after the complete run is validated. Add one system at a time so its effect on difficulty and replayability can be assessed.

## Immediate next step

Playtest ProceduralMazePrototype navigation and layout variation, then wire the 3 puzzle-locked key chests and exit door. Movement, combat and the soul loop have already passed user playtesting in the earlier arena scene. See DEVELOPMENT.md for current implementation scope.

## Confirmed design update

- First-person camera.
- Souls keep trying to escape even during capture and deal no damage. Stay close and hold capture continuously for 3 seconds; damage from another enemy resets progress. An uncaptured soul duplicates 10 seconds after spawning.
- Expired souls create two upgraded, more aggressive enemies with increased speed, slightly increased damage, and a changed attack type. Repeated duplication escalates danger; no gameplay enemy cap is currently specified.
- Ammunition, health, and healing are limited. Unchecked duplication can eventually overwhelm and kill the player.
- Collect 3 keys from puzzle-locked chests and open the exit door to win. Solve the associated puzzle before opening each key chest.
- Still undecided: soul flee speed, capture range, player movement during capture, exact upgrade amounts and attack types, puzzle designs, resource quantities, and controls.

These confirmed choices override earlier prototype suggestions; remaining unconfirmed details are proposals. This update changes documentation only.

## Horror exploration milestone
Implemented in the procedural prototype: district palettes/props, varied halls and key rooms, cramped passages, journal and quest inventory, three multi-location objective chains, window silhouettes/taps, shrouded hanging bodies, limited flashlight and sound investigation. Next verification gate: complete all journeys in live Unity, check navigation and controller input, then tune horror pacing and replace procedural placeholder art/audio with authored assets. See HORROR_PLAYTEST.md.
