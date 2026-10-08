# Project Soulbound — Mechanics

Status: initial rule proposals. The enemy-to-soul-to-two-enemies lifecycle is confirmed; interaction details and numbers remain provisional.

## Player, pistol, and resources

- Proposed health model: enemy attacks reduce health; zero health ends the run.
- Firing consumes one bullet only when a shot is actually fired. An empty pistol gives clear feedback.
- Start with one ammunition pool. Reloading and magazine size are open decisions.
- Keep damage and ammunition values configurable. Required routes must remain possible through avoidance or guaranteed supplies.
- A chest opens once per run. Its contents can include ammunition or an objective item. Reopening must not grant duplicate loot.
- Objective items persist until used or the run ends. Healing is limited; healing amount, supply, use interaction, and inventory capacity remain open decisions.

## Enemy lifecycle

`Roaming → Chasing → Attacking → Defeated → Soul active → Captured OR Expired → Two enemies`

Roaming enemies use reachable patrol locations. Detection starts pursuit; losing the player eventually returns them to roaming. Attacks need readable feedback and a cooldown. Exact detection range, damage, and pursuit behavior are tuning decisions.

Each defeated enemy creates exactly one soul and removes the original enemy from the active enemy count. An expired soul is consumed and creates exactly two enemies, with slightly increased aggression compared with their parent. Descendant enemies follow the same lifecycle; repeated generations become slightly upgraded and more aggressive. Speed and damage increases and changed attack types are confirmed; exact values, scaling, and attack patterns remain open.

## Soul capture — proposed MVP rules

1. On enemy death, create a soul on valid reachable ground near the death location and start its countdown.
2. Show the soul and its remaining time clearly; add an urgent warning near expiry.
3. The player must be within capture range and have a clear interaction path.
4. The soul moves away from the player. Staying close and holding the capture button continuously for 3 seconds completes capture; show progress throughout the hold. Leaving range or releasing interact resets progress.
5. Successful capture removes the soul permanently and cancels its countdown.
6. Capture costs no ammunition and grants no spendable currency in the first prototype.

The soul continues trying to escape throughout capture and never damages the player. Damage from another enemy resets capture progress to zero; the soul countdown continues. Player movement during capture and capture through obstacles remain open decisions. Tune flee speed so a continuous 3-second close-range capture is possible. Proposed default: block capture through walls and allow only one soul to be targeted at a time.

## Timer expiry and duplication

- At expiry, stop capture and resolve the soul exactly once.
- Remove the soul and spawn two slightly upgraded, more aggressive enemies at validated nearby positions.
- Use a visible warning and a short spawn grace period so the player cannot take unavoidable instant damage.
- Avoid walls, locked areas, other occupied spawn points, and the player's immediate space.
- New enemies can also die and leave souls; ignoring successive generations produces escalating pressure.

Example with no capture and no population cap: one enemy dies, its soul expires into two enemies; both die and expire into four. Each expiry replaces one soul with two enemies, rather than keeping the soul or reviving the original enemy alongside them.

### Escalation and technical constraints

Repeated duplication increases enemy count and aggression. Ammunition, health, and healing are limited, so unchecked growth can eventually kill the player. No gameplay population cap is currently specified; the earlier cap proposal is withdrawn.

Confirmed upgrade directions: descendants gain speed, slightly more damage, and a changed attack type. Proposed tracking: retain a generation index to select upgrades. Exact increments, scaling, and attack patterns remain undecided; health increases and attack cooldown changes are not confirmed.

Validate both spawn positions and resolve each pair exactly once. Handling temporarily unavailable positions is still an implementation decision. Profile sustained growth before selecting any technical fallback that changes gameplay.

### Resolution edge cases

- Capture succeeds only if its completion occurs strictly before the expiry deadline; expiry wins ties.
- One soul resolves once even if death, capture, and timer events arrive together.
- Restart clears enemies, souls, spawn requests, timers, and capture progress.
- Win/lose pauses gameplay timers and prevents later spawning.
- Pausing the game pauses soul countdowns.
- If the death position is invalid, relocate the soul to the nearest validated reachable position.

## Initial tuning hypotheses

These are starting points for experiments, not balance commitments.

| Parameter | First trial | What to observe |
| --- | --- | --- |
| Soul lifetime | 10 seconds (confirmed) | Can the player reach it after a normal fight? |
| Capture duration | 3 seconds (confirmed) | Does exposure create a choice without feeling tedious? |
| Capture range | 2 Unity units, adjusted to world scale | Is the interaction intuitive? |
| Starting ammunition | Enough for roughly 3 basic enemy kills | Can players explore without shooting everything? |
| Initial enemies | 3 in the small test maze | Are there safe routes and meaningful encounters? |
| Descendant aggression | Small upgrade per generation; values undecided | Does each generation increase pressure? |
| Spawn grace period | 1 second | Can nearby players react fairly? |

Tune ammunition together with bullets required per kill. Keep all values editable in future implementation rather than spread across gameplay logic.

## Puzzle and run rules

Confirmed MVP objective: collect 3 keys from puzzle-locked chests and open the exit door to win. Show key progress from 0/3 to 3/3 and indicate missing keys at the door. Health reaching zero loses the run. Restart resets the entire run and chooses new placements if variation is enabled.

All 3 keys must be in puzzle-locked chests reachable without passing the final door. Puzzle clues and required puzzle items must be accessible before their own chest opens. Solving a chest puzzle permits opening it; opening grants its key only once. Enemy spawning must not permanently block the only route. Optional chests may create risk, but the mandatory objective must remain reachable.

## Verification scenarios for future implementation

- Kill one enemy: one soul appears and the original enemy disappears.
- Capture before expiry: no child enemies appear, even after the old deadline.
- Ignore a soul: it disappears and exactly two enemies appear.
- Kill both descendants and ignore their souls: four enemies appear, upgraded by another generation.
- Verify successive generations increase aggression and each expired soul creates exactly one pair; profile sustained growth.
- Capture while the soul flees: a full 3-second close-range hold succeeds; the soul deals no damage.
- Take damage from another enemy during capture: progress resets to zero and the 10-second countdown continues.
- Pause, restart, win, or lose: timers follow the rules above and no stale spawn occurs.
- Generate varied placements: all 3 keys and the exit remain reachable, and loot cannot be collected twice.

## Confirmed design update

- First-person camera.
- Souls keep trying to escape even during capture and deal no damage. Stay close and hold capture continuously for 3 seconds; damage from another enemy resets progress. An uncaptured soul duplicates 10 seconds after spawning.
- Expired souls create two upgraded, more aggressive enemies with increased speed, slightly increased damage, and a changed attack type. Repeated duplication escalates danger; no gameplay enemy cap is currently specified.
- Ammunition, health, and healing are limited. Unchecked duplication can eventually overwhelm and kill the player.
- Collect 3 keys from puzzle-locked chests and open the exit door to win. Solve the associated puzzle before opening each key chest.
- Still undecided: soul flee speed, capture range, player movement during capture, exact upgrade amounts and attack types, puzzle designs, resource quantities, and controls.

These confirmed choices override earlier prototype suggestions; remaining unconfirmed details are proposals. This update changes documentation only.
