# Project Soulbound

A replayable maze survival/puzzle game about exploring, managing scarce ammunition, and containing enemies before their souls multiply.

## Game concept

Explore a maze, collect chests and useful items, solve puzzles, and survive roaming enemies. The player carries a pistol with limited ammunition. Killing an enemy leaves a soul behind. Capture that fleeing soul with a **3-second hold** before its **10-second timer** expires, or it turns into **two upgraded enemies** with increased speed, slightly increased damage, and a changed attack type.

Combat creates a second problem: every kill needs a safe opportunity to capture the soul.

## Documentation

- [Game design document](docs/GDD.md): vision, core loop, MVP, puzzle ideas, and replayability.
- [Mechanics](docs/MECHANICS.md): proposed rules, soul lifecycle, duplication, and tuning.
- [Roadmap](docs/ROADMAP.md): implementation milestones and playtest criteria.

The core concept above is confirmed. Additional rules in these documents are **proposals for the first prototype**, not final decisions or implemented features.

## Current project

- Unity project folder: `Project Soulbound/`.
- Editor version recorded in the project: **6000.6.4f1**.
- Universal Render Pipeline, Input System, and AI Navigation are listed in the package manifest.
- Inspection found `Assets/Scenes/SampleScene.unity` and Unity tutorial/readme scripts; no custom gameplay scripts were found.
- This documentation pass does not change scenes, scripts, assets, or project settings.

Open `Project Soulbound/` through Unity Hub using the recorded editor version. Start implementation with the first milestone in the roadmap after resolving the blocking design choices.

## Confirmed design update

- First-person camera.
- Souls keep trying to escape even during capture and deal no damage. Stay close and hold capture continuously for 3 seconds; damage from another enemy resets progress. An uncaptured soul duplicates 10 seconds after spawning.
- Expired souls create two upgraded, more aggressive enemies with increased speed, slightly increased damage, and a changed attack type. Repeated duplication escalates danger; no gameplay enemy cap is currently specified.
- Ammunition, health, and healing are limited. Unchecked duplication can eventually overwhelm and kill the player.
- Collect 3 keys from puzzle-locked chests and open the exit door to win. Solve the associated puzzle before opening each key chest.
- Still undecided: soul flee speed, capture range, player movement during capture, exact upgrade amounts and attack types, puzzle designs, resource quantities, and controls.

These confirmed choices override earlier prototype suggestions; remaining unconfirmed details are proposals. This update changes documentation only.
