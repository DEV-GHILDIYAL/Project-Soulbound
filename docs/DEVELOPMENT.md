# Development foundation

## Procedural maze prototype

Open `Assets/_Soulbound/Scenes/ProceduralMazePrototype.unity` and press Play. This separate scene preserves the movement and combat arenas. It generates a 15x15 maze with 4-metre cells and 3-metre-high walls at runtime. The player starts in one corner facing an open corridor; one enemy starts deep in the maze and uses generated patrol points. Existing shooting, soul capture, fleeing, and duplication remain active.

Controls: R reloads with a new random layout, F5 reloads the same layout seed. Seed appears on the HUD and in the console. To reproduce a saved seed later, stop Play, select Combat Prototype Setup, disable Randomize Seed on ProceduralMaze, and enter that seed. Replay guarantees maze geometry and objective placements, not a recording of actor movement or input. Reproducibility assumes the same generator/runtime version.

MazeLayout uses iterative randomized depth-first carving, then opens some extra passages for alternate routes. A breadth-first traversal selects a distant exit and three distinct chest positions distributed across shallow, middle, and deep path distances. Start, exit, and chest cells never overlap. There is no closed gate splitting the maze during this generator milestone.

Dungeon chest models now live inside three locked key chambers. Each chamber has a working external puzzle and a one-use key reward inside. The glowing green exit now has a physical seal: collect all 3 keys and press E nearby to open it and win.

### Dungeon appearance

ProceduralMaze now enables Dungeon Look by default. DungeonDressing adds staggered weathered stone courses, grain-textured stone slabs with mortar seams, a roof with cross beams, corner pillars, warm wall torches, timber/iron/brass chest details, and an arched glowing exit seal. Fog, mild bloom/vignette, and a small player lantern give the dungeon a darker atmosphere while retaining close-range visibility. Torch intensity flickers subtly. These are procedural prototype visuals, not imported final art.

Details are combined into meshes per material and 8-metre spatial region to keep draw calls and local URP light selection manageable. A maximum of 24 torch point lights is used; extra torches remain visual. Point lights do not cast shadows, so some light leakage across walls is possible. Runtime materials, textures, volume assets, and meshes are cleaned up when the scene unloads. Core wall/floor colliders and maze topology are unchanged; decorative meshes have no colliders. Turn off Dungeon Look before Play to return to the simple greybox environment.

Reopen ProceduralMazePrototype after stopping Play to load the serialized Dungeon Look setting. Test corridor visibility, torch lighting, wall collisions, and frame rate at the default 15x15 size. Code compiles against the installed URP assemblies; the rendered appearance has not been visually inspected in this pass.

The generated geometry is created before NavMesh baking. After baking, the coordinator checks complete navigation paths from the start to the exit, all three chest locations, and enemy spawn. A failed check reports the seed on the HUD and in the console. Startup may pause briefly while navigation builds.

Validation: all gameplay scripts compile against Unity references; serialized scene links pass. Independent layout checks pass for 1,003 layouts (3x3, 9x9, 15x15, 31x31 plus rectangular cases), covering connectivity, reciprocal passages, closed outer boundaries, distinct reachable objectives, and deterministic replay. The test source is `tests/MazeLayoutChecks.cs`, outside Unity Assets. Generated test binaries remain under the ignored Unity Temp folder. This does not verify live NavMesh baking, graphics, or player comfort.

Playtest: compare a few R-generated layouts, confirm F5 retains the visible seed/geometry, explore every chest marker and exit, and observe enemy/soul navigation in corners. Puzzle-clue reachability and a full three-key win remain future checks once real puzzles are placed.

## Folder structure

New game assets live under `Project Soulbound/Assets/_Soulbound/`. Existing template assets and scenes are preserved.

- `Art/Materials`, `Models`, `Textures`, `Animations`
- `Audio/Music`, `SFX`
- `Prefabs/Player`, `Enemies`, `Souls`, `Environment`, `Items`, `Puzzles`
- `Scenes`, `Data`, `UI`, `Editor`
- `Scripts/Core`, `Player`, `Combat`, `Enemies`, `Souls`, `Items`, `Puzzles`, `UI`

Empty folders are intentional reserves, with Unity metadata committed alongside assets. A movement test scene and reusable player prefab are now available.

## Movement test scene

Open `Assets/_Soulbound/Scenes/MovementPrototype.unity` and press Play. The scene includes a wired first-person player, camera, health/inventory, run state, lighting, 24-metre test floor, boundary walls, a narrow cornering lane, slalom blocks, 0.2-metre steps, and a gentle ramp. The player starts at the south side facing the test area. WASD moves, mouse looks, Escape releases the cursor, and click resumes control. No combat or soul targets are placed in this movement-only scene.

The reusable player is saved at `Assets/_Soulbound/Prefabs/Player/FirstPersonPlayer.prefab`. Its camera and health references are internal; assign a scene RunState after placing it. The scene player is a standalone copy, so later prefab edits will not automatically propagate to it.

The scene was authored directly using the existing sample scene's lighting/camera format. Object identifiers and local references were checked. Unity's batch startup exited before import, so visual import and play-mode behavior remain unverified. The existing sample scene and build scene list were preserved.

## Placeholder materials

Eight URP Lit materials: floor (dark grey), wall (grey), enemy (red), soul (cyan), chest (brown), key (gold), exit (green), puzzle (purple). These are opaque flat-color placeholders, including the soul; glowing/translucent effects come later. Their shader identifier was taken from the project's installed URP package. Visual import has not been checked in Unity yet.

## Combat test scene

Open `Assets/_Soulbound/Scenes/CombatPrototype.unity` and press Play. This is a separate copy of the movement arena; the movement scene and player prefab remain intact. The scene is included after the sample scene in the build scene list so the restart control can reload it.

The scene builds a NavMesh from environment colliders at startup, then creates one red capsule enemy. Player objects are put on Ignore Raycast layer and excluded from the navigation bake and pistol hit mask. Enemy AI patrols reachable points, detects the player within 10 metres with line of sight, chases, remembers the last seen position for 3 seconds, and attacks within 1.6 metres. Its capsule flashes orange during the 0.35-second attack windup; range and wall visibility are checked again at impact. Initial damage is 10 with a 1.2-second cooldown after windup. Death removes the enemy and creates one fleeing soul at its position.

Left click fires a hitscan pistol (25 damage, 0.3-second shot interval). HUD shows player health, ammunition, enemy health/state, crosshair, and SHOT/HIT/EMPTY feedback. Start with 100 health and 12 bullets; the enemy has 100 health and needs 4 hits. R reloads the test scene; player death ends combat and unlocks the cursor. Pistol has a procedural first-person view model with slide recoil and a muzzle flash. Authored hands/arms, final weapon art, and firing sound remain deferred.

### Combat checks

- Observe patrol before approaching, then approach into detection range: enemy transitions to chase and paths around blocks.
- Put a wall between player and enemy: it cannot detect or damage through the wall; it searches its last seen position before returning to patrol.
- Stand in attack range: orange windup precedes each hit. Back away during windup: the strike misses if out of range at impact.
- Hit the enemy 4 times: health reaches zero and it becomes a fleeing cyan soul. Shooting walls consumes ammo without enemy damage; an empty pistol causes no damage.
- Let player health reach zero: run stops movement/shooting/attacks. Press R: health, ammo, enemy, and navigation reset.

Compilation against installed Unity, Input System, and Navigation assemblies passes; scene local references are validated. Navigation baking, visual import, and play-mode behavior still need checking in Unity. The scene now includes the soul capture and duplication loop described below.

## Starter scripts

| Component | Available foundation |
| --- | --- |
| FirstPersonController | Smooth acceleration/braking, adjustable mouse smoothing, gravity, distance-based head bob, subtle strafe roll, cursor/focus handling |
| Health | Damage, healing, damage/death events |
| PlayerInventory | Finite bullets, finite healing charges, key count |
| Pistol | Mouse-click hitscan, ammunition consumption, cooldown, health damage |
| RunState | One-time win/lose events |
| EnemyGeneration | Generation index and provisional speed/damage values |
| Soul | NavMesh fleeing, 10-second deadline, one-time capture/expiry, run-end freeze |
| SoulCapture | E hold for 3 seconds, close-range aim check, reset on enemy damage |
| PuzzleLock | One-time solved state for a future concrete puzzle |
| KeyChest | Requires solved puzzle, grants one key once |
| ExitDoor | Requires 3 keys, opening event and win |

Numbers other than the confirmed soul lifetime, capture duration, and key count are temporary prototype defaults. Keyboard/mouse controls are provisional. Starter movement uses the installed Input System directly; action-map rebinding is deferred.

## Wiring the first test scene

1. Create a new scene under `_Soulbound/Scenes` when beginning the maze milestone.
2. Add a player root with CharacterController, Health, PlayerInventory, FirstPersonController, Pistol, and SoulCapture. Add a child camera and assign it to the three components that require a view.
3. Add a RunState object and assign it to the player controller, pistol, capture component, soul objects, and exit. Assign the player Health to controller, pistol owner, and SoulCapture. Assign PlayerInventory to Pistol.
4. Connect the player Health death event to RunState.Lose in the future scene bootstrap; it is a C# event, so this requires subscription in code. Win/lose currently emits events rather than showing screens.
5. Soul targets need colliders and must be included in the capture interaction mask. Capture currently requires aim and clear line of sight. The player must move to track the fleeing soul. Exclude the player layer from pistol/capture masks to avoid self-hits.
6. Connect concrete puzzle completion to PuzzleLock.Solve. Assign that lock to its KeyChest. A future interaction controller calls KeyChest.TryOpen and ExitDoor.TryOpen with the player inventory; those components do not yet poll input themselves.

WASD moves; mouse looks; left click fires; E holds capture; Escape releases the cursor and left click locks it again. Cursor release does not pause timers. Jump, sprint, reload, and sound remain deferred. The combat/dungeon scene now includes HUD, puzzle/results panels, and H for healing.

### Player movement and camera feel

The controller now accelerates and brakes smoothly instead of changing speed instantly. Diagonal movement is normalized; reduced air control preserves some momentum. Mouse sensitivity applies to mouse displacement, independent of frame time, with adjustable smoothing. The camera adds small head bob based on actual grounded travel and subtle strafe roll, then settles when movement stops. Walking into a wall does not drive head bob. Cursor release stops new movement input while braking and gravity continue; losing application focus releases the cursor.

Inspector defaults are walk speed 2.8, acceleration 28, braking 45, look smoothing 0.025 seconds, bob amount 0.045 metres, and strafe roll 0.6 degrees. Set bob amount and strafe roll to zero to disable camera motion; set look smoothing to zero for immediate aiming. Existing serialized speed and sensitivity values are preserved. The camera should be a dedicated child camera with a neutral local rotation; its starting local position is preserved. No stamina or new combat mechanics were added.

This controller change compiles against Unity references but still needs play-mode checks in a wired scene: turns at different frame rates, braking, collisions/slopes, capture while moving, cursor focus, and camera comfort.

## Remaining work

- Build the actual maze beyond the movement arena, and wire gameplay components.
- Playtest combat and the complete death-to-soul capture/duplication lifecycle.
- Tune soul fleeing routes and capture comfort with actual playtesting.
- Refine descendant attack progression beyond the first-generation lunge prototype.
- Profile repeated generations and refine spawn fallback presentation.
- Add concrete chest puzzles and interaction input, healing pickups/use, door animation, HUD and run restart.
- Extend run-end handling from the combat prototype to future souls, puzzles, and effects; RunState itself records the result.

## Validation

The 11 starter scripts compiled together against the installed Unity 6000.6.4f1 engine references and the project's Input System assembly. Compilation had no errors; serialized Inspector references produced expected unassigned-field warnings. This verifies C# compatibility, not Unity asset import, scene wiring, or play-mode behavior. Those checks belong to the first playable scene milestone.

Movement tuning update: grounded changes of direction follow input immediately while speed ramps briefly. Lower walking speed and stronger braking reduce sliding. Bob uses a 0.36-cycle-per-metre gait (about 2 steps per second at full speed); smoothing applies to its intensity instead of attenuating the footstep waveform. Scene and prefab values are updated alongside script defaults. These are proposed tuning values pending player feedback.

## Soul-loop prototype

The existing CombatPrototype scene now wires SoulCapture to the player camera, health, and run state. Killing an enemy creates a cyan sphere soul at eye height, navigating away from the player at 1.65 metres/second. It chooses reachable fleeing destinations every 0.3 seconds and keeps escaping during capture. Souls have trigger colliders and no health/damage component; shooting them wastes ammunition.

Aim directly at a soul within the 2-metre raycast range and continuously hold E for 3 seconds. Releasing E, losing aim/range, or taking enemy damage resets progress. The countdown is an absolute 10-second scaled-time deadline; capture at or after expiry fails. Each soul resolves once. The HUD shows its countdown, capture percentage, interruption feedback, live enemy/soul counts, captured count, and duplication count.

An expired soul produces exactly two enemies one generation above its defeated parent. Prototype tuning increases speed by 10% of base speed per generation and damage by 5% of base damage per generation; health stays at 100. Generation zero uses close melee; generations one and above add a short lunge from up to 2.7 metres, with 0.45 seconds of orange windup and 0.25 seconds of movement. The lunge checks NavMesh boundaries and damage still requires range and line of sight. Spawned descendants wait 1 second before acting. Further generations retain the lunge while speed/damage keep increasing; more attack patterns remain future design work.

Both spawn points must be walkable, reachable, separated, unoccupied, and at least 1.6 metres horizontally from the player. If two valid locations cannot be found within 4 metres of the expired soul, it remains visibly unstable and resolved while the coordinator retries every 0.2 seconds. It cannot be captured after the deadline. This is a technical fallback, not an enemy-count cap. The HUD reports pending pairs. There is no periodic enemy cap or extra refill of limited health/ammunition.

Player death stops enemy/soul movement and pending spawns; R reloads the scene and clears all runtime actors and counters. The movement-only scene/prefab are unchanged by this milestone. Compilation and serialized scene-reference checks pass; play-mode behavior and sustained-growth performance require testing in Unity.

### Soul-loop playtest

1. Kill the original enemy: exactly one cyan soul appears with roughly 10 seconds remaining.
2. Chase and aim at it while holding E: it keeps fleeing, capture completes after 3 uninterrupted seconds, and no offspring appear after the old deadline.
3. Restart; kill and ignore the soul: it disappears and exactly two generation-one enemies appear when valid spawn space is available.
4. Kill one descendant while the other is alive: capture its soul while under pressure; a hit resets progress without resetting the soul countdown.
5. Ignore descendant souls: each expiry creates a pair at the next generation with increased speed/damage and lunge attacks.
6. Test near walls/corners: souls follow navigable routes; lunge attacks do not damage through walls. A blocked pair waits and then resolves once space becomes available.
7. Die or restart with active souls and pending pairs: old timers/spawns must not continue into the new run.

## Larger dungeon and puzzle rooms

### Dungeon props

ProceduralMaze has a Prop Density setting (default 0.65). Eligible cells receive one small wall-side cluster: iron-banded barrels, timber crates, loose rubble, skull/bone remains, broken pillar bases, hanging iron racks, or tattered banners. Props use a separate seeded random stream, so they replay with F5 without changing layout, puzzles, or the stone-course pattern.

The start, exit, chest cells, and both sides of room entrances are excluded. Props are anchored against closed walls; open passages and cell centres stay clear. Barrels, crates, and broken pillars have static box colliders generated before NavMesh baking, so they block movement/shots and navigation can route around them. Low rubble/bones and wall decorations have no colliders. They are scenery, not loot containers or destructible objects.

Prop meshes share the existing material/spatial batches. Cylinder/sphere meshes provide rounded barrels and bone details; 32-bit indices support mixed meshes safely. No extra prop lights are added. Set Prop Density to zero before Play to remove this dressing. Compilation and saved density settings are checked; rendered appearance, corridor navigation with props, and frame rate require Play-mode testing.

The prototype now defaults to 15x15 cells (60x60 metres), about 2.8 times the old floor area. Three separated 2x2-cell chambers are reserved inside the grid before corridors are carved. Each room has exactly one entrance; the surrounding corridor graph stays connected even with all three doors shut. Room placement prefers distance from the start and separation from other rooms. Chest positions are fixed inside their own chamber at the far corner relative to the entrance, never loose in corridors. No room-location arrows or minimap are provided.

Base enemy speed is now 1.25 metres/second versus player walking speed 2.8. Existing generation upgrades remain: +10% of base speed and +5% of base damage per generation, plus descendant lunge attacks. Initial enemy count remains one for focused prototype testing.

Approach a sealed chamber and press E to inspect the puzzle. Mouse buttons operate the panel; Escape or Leave closes it. Puzzle progress persists when leaving, and enemies/soul timers continue running. Taking damage closes the panel so the player can react. Mouse clicks on the puzzle do not steer or fire the player. Reset returns that mechanism to its original scrambled state.

- Rune Chamber: order five runes using four relative-position clues. The clue template combines before/after, adjacency, and a two-position gap; it guarantees one order. Enter runes and Confirm to unlock.
- Flame Chamber: six lights with cyclic linked switches; each switch toggles itself and both neighbours. Match the displayed target, then Confirm. The scramble is produced by legal moves from the target.
- Astral Chamber: three six-symbol rings; turning one also advances the next, and the inner ring wraps to the outer. Interpret relative-symbol inscriptions to find target positions and align all rings. The scramble is produced by legal moves.

A solved chamber door raises and removes its collider/NavMesh obstacle. Go inside and press E near its chest to receive one key, once. The HUD shows Keys: 0/3 through 3/3. Puzzles are on the room entrance, replacing the earlier separate chest-lock proposal. Puzzle controls currently use a prototype UI rather than animated world mechanisms. Chest geometry is static; key collection has HUD feedback but no lid animation yet.

Locked door colliders use the built-in TransparentFX layer so the static NavMesh bake excludes them. Box NavMeshObstacles carve their closed entrances; opening removes the obstacle. Corridor and exit validation targets the outside approaches rather than inaccessible chest interiors. Enemies do not patrol inside key rooms, though unlocked rooms can be entered during pursuit. Spawn pair validation still treats closed doors as blocking colliders.

Validation: 900 seeded room layouts at sizes 9, 15 and 21 pass connectivity with all doors closed, single-entrance rooms, interior chests, and replay checks. 300 seeded puzzle configurations pass unique rune-order and exhaustive reachable-state checks for torches/rings. Scripts compile against installed Unity assemblies. Runtime carving, door transitions, puzzle input, and larger-dungeon frame rate still need Unity playtesting.

Playtest: use R to explore varied room placements, F5 to replay; confirm every door blocks entry before solving, mechanisms can be resumed/reset, enemies can interrupt puzzles, each chest grants one key, and main corridors remain navigable with all doors locked. Then reach the exit with all 3 keys and verify the win flow described below.

## Complete run flow and finite supplies

ProceduralMazePrototype now supports a complete run: explore, solve three room puzzles, collect each chest key, find the exit, and press E nearby. The exit remains sealed with fewer than 3 keys. With all keys, its collider disables, the seal raises, and RunState records a win once. Opening is the win trigger; crossing a separate threshold is not required. RunState distinguishes wins from deaths.

Win and lose result panels show elapsed run time, key count, captures, and seed. Both offer New Run (R) and Retry This Dungeon (F5). R chooses a fresh maze when Randomize Seed is enabled; F5 replays geometry, puzzles, props, and supply locations with reset health, ammunition, keys, enemies, souls, and supplies. The sample and arena scenes remain available. Mouse cursor unlocks at run end; movement, shooting, healing, pickups, enemy attacks, soul countdowns, and pending duplication stop. The exit opening animation may finish behind the results panel.

Finite prototype supplies: 12 starting bullets plus six one-use ammo packs worth 6 bullets each; one starting healing charge plus two one-use healing pickups. H spends one charge to heal up to 30 HP. Full health, dead player, or no charges causes no spend. Healing is disabled during puzzle interactions and after a run ends. Amounts are provisional balance values, not unlimited refills.

Supplies use a separate seed stream and distinct unlocked corridor centres, outside the key rooms and exit. Pickup visuals have no collider, so they do not obstruct navigation. Approach within 1.3 metres with unobstructed sight to collect automatically; nearby labels identify ammo or healing. Collection works once and is cleared on reload. The player HUD displays healing charges and the H control.

Validation: gameplay scripts compile and existing serialized scene references remain valid. The new systems are created/wired at generation time. Live checks still required: exit rejects 0–2 keys, accepts 3 exactly once, seal animation, one-use pickups, healing at full/partial/zero health, win/lose actor freeze, mouse-driven results buttons, and complete R/F5 reset with active/pending souls. Supplies and room/puzzle difficulty still need balance playtesting across seeds.

## Keyboard/mouse, gamepad, clean HUD and weapon view

PlayerControls centralizes keyboard/mouse and Unity Input System Gamepad inputs, with radial stick deadzones and repeat-limited menu navigation. Mouse look uses displacement; stick look uses an editable 150 degrees/second rate scaled by frame time. Plugging/unplugging a supported gamepad is handled through Gamepad.current. Prompts switch according to recent keyboard/mouse or gamepad activity. Existing action-map rebinding is still deferred.

| Action | Keyboard/mouse | Standard gamepad |
| --- | --- | --- |
| Move | WASD | Left stick |
| Look | Mouse | Right stick |
| Fire pistol | Left click | Right trigger / R2 |
| Interact / hold soul capture | E | South face button: A / Cross |
| Heal | H | West face button: X / Square |
| Leave puzzle | Escape | East face button: B / Circle |
| Select puzzle/result buttons | Arrow keys or mouse | D-pad or left stick |
| Activate selected button | Enter or mouse click | A / Cross |
| Release/resume cursor | Escape / mouse click | Start / Options toggles |
| New/replayed dungeon | R / F5 | Select respective result-screen button |
| Debug counters | F3 | Optional keyboard shortcut |

Cursor release is not a simulation pause. Puzzle/result panels block gameplay input, preventing controller selection from moving/turning/shooting the player. Puzzle progress survives leaving; incoming damage closes its panel. The button that opens a puzzle or wins a run is not reused to activate its menu in the same frame.

Normal HUD now keeps health/bar and healing at bottom-left, ammunition at bottom-right, and key progress at top-right. A small dot/line crosshair sits at centre; it changes colour when a capturable soul is aimed at. Capture deadline and progress appear in a narrow lower-centre strip for the aimed nearby soul. Unrelated overhead soul labels, permanent controls lists, enemy counts and seed panels are removed from normal play. F3 enables optional debug counters. Nearby room/exit/supply prompts appear only when relevant. A hit marker replaces SHOT/HIT text; EMPTY feedback stays near the ammo display. Results show run stats and seed after the run ends.

PistolViewModel creates a gunmetal low-poly pistol with slide, grip, sights, trigger guard, muzzle, and serrations. Actual shots animate recoil, slide movement and a brief flash; empty fire does not. Collider-free weapon parts stay on Ignore Raycast layer. A URP overlay camera renders the model over world depth so it does not disappear into nearby walls, while the main camera excludes that layer. This is a procedural placeholder model without hands/arms, not final realistic weapon art. View-model materials, camera and objects clean up on scene unload.

Validation: scripts compile against installed Input System and URP versions, and runtime wiring preserves existing scene references. Hardware controller testing and visual HUD/weapon review have not been performed. Playtest mouse/pad switching, unplugging, stick drift/frame-rate sensitivity, fire/capture/heal, all three puzzle menus, win/lose buttons, no shots on UI click, wall clipping, and readability at the intended display resolution.

HUD visual refresh: compact rounded translucent charcoal cards, subtle brass borders, warmer typography, segmented crimson health meter, healing keycap, larger unclipped ammo/health numbers, and three key icons that light up as collected. Screen-height scaling keeps corner margins and proportions consistent; centre crosshair remains unchanged. Gameplay values and control mappings are preserved. Compilation passes; a fresh in-game screenshot is still needed for rendered layout review.

Chamber gate visual update: plain gate renderers are replaced by oak plank panels, iron borders/cross-straps, bronze rivets and lock plates, plus cyan/amber/violet seals for the three chambers. All visual parts follow the existing raising gate; colliders and NavMesh obstacle dimensions are unchanged. Chamber lettering is smaller, brass-coloured, and attached to the gate so it rises with the door.

HUD contrast/layout correction: independent GUI text styles explicitly set light text in every state, zero inherited padding, and restore GUI tint/content colour after drawing. Health caption and number have separate columns; the key header is shortened to KEYS to avoid colliding with its count. Minimum scaling is increased for readable small-window text, and all number bounds fit the font. Scripts compile; final colour and door appearance still need fresh Play-mode review.

Wall seam correction: wall collider/mortar spans now equal cell size; visible stone courses stop 0.01 metres short of each endpoint instead of overlapping neighbouring segments by 0.25 metres. Wall coping stops short of corner posts, and intersecting roof beams use separate heights to avoid coplanar faces. This addresses the reported seam z-fighting; fresh visual checks at straight seams and corners are still required.

Prop variety expanded to 12 cluster types with default density 0.8: urn pairs, benches, wall shields, candle shrines and hanging chains join earlier props. Existing objective exclusions and clear centre lanes remain. Static prop colliders are included in the navigation bake.

Enemy appearance: DungeonGuardVisual replaces the rendered capsule with a procedural armoured humanoid: helmet/crest, ember eyes, breastplate, tabard, limbs/boots and mace. Walking poses follow actual agent velocity; the weapon arm raises for attack/lunge poses. Descendants use a different tabard tint, and attack warnings tint all visible body pieces. The original capsule collider, health and AI remain; this is placeholder art with simple procedural animation, not a rigged production character. Runtime materials clean up on enemy removal. Scripts compile; model hit readability, seam appearance, navigation and FPS require Play-mode testing.

Soul visual update: a floating emissive face, translucent aura, orbiting motes and a short trail replace the rendered sphere. The original trigger hitbox remains for capture. The core pulses orange in the final 3 seconds. There are no extra lights or damage; the 10-second deadline, fleeing and 3-second capture rules remain. Materials clean up with the soul.

Enemy combat movement: spotting/remembering the player enables a lower, forward-leaning torso and raised guard arms. When visible at 2.8–8 metres, the enemy may try a short 0.8-metre side step about every 2.8 seconds, provided the straight NavMesh segment is clear. It faces the player during the dodge and returns to chase afterwards. Normal patrol speed stays slow; attacks/lunges take priority. This is evasive footwork, not automatic bullet immunity.

Door interaction correction: room prompts and interaction now share a closest-point check against the actual gate collider (2.6-metre range), plus a visibility test including doors/props and excluding the player. Modal interaction uses 3.2 metres of tolerance. The old invisible offset-point requirement is removed, so standing close to the visible door is sufficient when unobstructed. Compile passes; fresh checks required for all door orientations, E/A opening and B/Esc closing, crouch/dodge in tight corridors, and soul appearance/capture.

## Native UI, interaction reliability and reload update

GameplayHUD and chamber puzzle panels now use native screen-overlay Canvas/Text/Button components with explicit white/gold colours, scaling and a dedicated Input System UI EventSystem. Legacy IMGUI HUD is suppressed when the Canvas HUD exists. F3 debug remains available on a native panel. Mouse button events operate puzzles/results; existing explicit D-pad/arrow/A/Enter navigation remains, without duplicate native submit handling.

Room interaction uses a dedicated InputAction bound to E/A in addition to held-button edge detection. Opening logs `Opened chamber puzzle` in the console and immediately shows the native mechanism panel. Its former continuous distance recheck no longer auto-closes it; only leave/cancel, enemy damage, or run end closes it. The visible gate range/visibility check remains necessary to open it.

R reloads during gameplay (Y/Triangle on gamepad). New-run R is restricted to result screens, removing its conflict with reload. The pistol uses an 8-round magazine, finite reserve ammunition, and a 1.4-second reload. Initial 12 total bullets become 8 loaded / 4 reserve; reload transfers only what is missing. Reload blocks firing, stops on death/end, and does not create ammo. HUD shows loaded/reserve and reload status. The view model has an exposed round barrel/bore, ejection port, rail and a magazine movement/tilt during reload.

Enemy patrol stays at the slow base speed. Detection switches to faster 1.8x pursuit and 3x-speed side dodges with a 1.7-second attempt cooldown and quicker acceleration. Existing short lead prediction is constrained by NavMesh walls; path refresh is throttled to 0.15 seconds. Incoming nonfatal hits encourage a dodge attempt, without immunity or teleporting.

Soul tails are segmented and wave, with an extra orbiting crown. Capture/deadline rules remain unchanged. Pillars now cover all closed-wall joints and outside edges, once per grid vertex; completely open intersections are skipped. Pillar colliders are present before navigation baking.

Validation: native UI/Input System/URP scripts compile. Live testing remains required for E/A opening, pointer/controller menu input, readable Canvas text, reload conservation/full/empty cases, reload versus restart, accelerated combat navigation, and pillar clearance. If interaction fails again, the console opening message distinguishes an input/range failure from a panel-rendering problem.

## Horror exploration update
The procedural scene now creates HorrorDungeon, HorrorQuestState, HorrorWindow and HorrorAmbience at runtime. The two first chamber panels are replaced by multi-location world tasks; the final rings remain behind a scattered-note/medallion prerequisite. See HORROR_PLAYTEST.md for controls, exact chains, section geometry and current validation limits. This update supersedes the earlier single-panel puzzle-room flow.

## Main menu and scene flow
MainMenu.unity is now the first build scene. MenuController creates main/pause/settings/help interfaces at runtime; SceneNavigation handles asynchronous loading and result-screen retry/menu actions. MenuBackdrop creates the main-menu corridor and floating soul. Settings persist audio volume and mouse/gamepad look multiplier. See MENU_FLOW.md for controls and the live navigation test sequence.
