# Horror dungeon prototype

Open `Assets/_Soulbound/Scenes/ProceduralMazePrototype.unity`, stop any old play session, and enter Play mode. The generator creates the new systems automatically; no inspector wiring is needed. F5 replays the current seed, while a new run generates a new layout. Journal, quest items, and scare events reset on scene reload.

## Exploration and controls

- F / gamepad A (Cross): aim at a note, item or mechanism and interact within 2.7 metres. Key gates retain their existing proximity interaction.
- J / gamepad View (Select): journal containing only discovered notes and collected quest items.
- Arrows / D-pad: journal pages. Mouse buttons also work. B / Esc closes the journal; enemy damage interrupts reading. The world continues running while reading.
- G / left shoulder: flashlight. It has ten minutes of active use; switching it off conserves charge. Nearby quest objects have small lights, and the old player lantern is reduced to a very faint light.
- Existing shooting, reload, healing, soul capture and three-key exit controls remain.

## Key journeys

1. **Crypt:** prison guard diary → flooded caretaker epitaph → caretaker crest → correct chapel saint, identified by epitaph → interact with sealed crypt gate → chest key. Incorrect saints retain the crest and cannot unlock the gate.
2. **Furnace:** maintenance log → prison valve + catacomb crank → fit both at separate workshop assemblies → turn the three scattered pressure handles in the seeded order given by the log → furnace vault gate → chest key. Incorrect order resets pressure progress, not inventory. The started boiler attracts nearby enemies.
3. **Astral:** chapel journal page + catacomb journal page + astral inscription → prison medallion → astral socket → final coupled-ring alignment → chest key. Ring clues are discovered on the scattered pages before the final panel becomes usable. Ring 1 also moves 2; 2 also moves 3; 3 also moves 1.

Required nodes are placed once per run outside the sealed key rooms, with a distinct cell for each interaction. Destination sections grow around their key rooms; the prison and chapel fill the remaining regions. Placement prefers the named region, with a reachable free-cell fallback on unusually small layouts. Quest items cannot be farmed; completed stations remain completed.

## Environments

Five districts use different stone palettes, wall details and ambient pitch. Prison has bars, shrouded upside-down remains and isolated knocks; catacombs have reflective-looking shallow water and pipes; furnace has boilers and ember vents; chapel has saints and crosses; astral has ritual markings and cold lighting. Public halls remove internal walls in different rectangular footprints. At normal map size, key rooms are 8×8, 12×8 and 8×12 metres. Selected straight corridors narrow to approximately two metres and have lower ceilings, without closing their exits.

Fogged windows use generated grime textures and a flattened silhouette to simulate a figure pressed against obscured glass. Selected windows reveal it once after approach, tap, and then slide it out of view. Walls behind the panes stay sealed. These are procedural prototype visuals, not imported character models, real transparent exterior views or finished animation. Audio clips are generated placeholders for later sound design.

## Verification

Automated checks cover 900 seeded layouts, connected outside routes with sealed rooms, unique room entrances, interior chests, deterministic replay, 300 final-puzzle configurations, missing-task unlock prevention, duplicate rewards, and all six pressure-order permutations. Gameplay scripts compile against the installed Unity/package assemblies. Runtime NavMesh validation includes all quest approach cells as well as exit/gates/enemy spawn.

Live Unity playtesting is still required for lighting, low-ceiling movement, actual baked navigation, controller interactions, silhouette visibility, sound volume and pacing. Check the Console for a route-validation error and record the seed if one occurs. Complete all three journeys on a fresh run and verify that the final exit requires all three chest keys.

## Body bag storage revision
Hanging humanoid placeholders are replaced by six zipped canvas body bags in one seeded mortuary storage area in the prison district. Two rows preserve a centre passage; this cell is reserved from narrow-wall and random-prop placement. Each bag has a tapered closed mesh, zipper, restraints and tag, a solid capsule collider and a wider contact sensor. Player movement through the sensor excites a bounded, damped suspension, then the bag settles. Kinematic bodies live on layer 1 so they are not baked into static navigation. Live playtesting is required for contact feel; walls and water are unchanged in this revision.

## Water and enemy approach revision
The blue glass floor slabs are replaced by transparent dark-green shallow-water planes in selected catacomb cells. Stone stays visible below the surface; irregular alpha edges remain stationary while precomputed linear normal frames animate small ripples. Surfaces have no collider and do not emit light. Splash sounds and hearing events now depend on actual water-patch occupancy rather than the entire district.

Guards independently randomize reaction pauses, slow stalking, short circling steps and faster closing bursts. Dodges have varied intervals, lengths and durations; aiming or recent hits can influence the chance, and some dodges move diagonally towards the player. Candidate manoeuvres require a clear NavMesh segment and avoid layer-one physical obstacles. Patrol/chase acceleration is softened. Existing range/line-of-sight attack checks and descendant windups/lunges remain.

Guard legs now have separate thigh/shin/boot segments. The gait advances with actual distance travelled; stance feet retain world positions, swing feet lift and plant, and knees solve between hip and ankle. Weight shift and spatial footfall audio follow the stepping cycle. These are procedural poses rather than authored skeletal animation. Live playtesting remains necessary for foot planting during turns, narrow-corridor choices, attack timing and water visibility.

## Creature model and leg-flip repair
The armoured guard is replaced by a gaunt hunched creature with rounded body segments, long forearms, clawed fingers, hollow eye sockets and an elongated face. Helmet, mace and metal greaves are removed. The existing enemy collision capsule and gameplay AI remain. The windup colour tint is subdued to preserve the creature's palette.

Ground sampling now starts near the agent's floor height and accepts only upward-facing surfaces close to that height. Foot endpoints stay below the pelvis and within limb reach; the knee solver uses a stable forward bend direction. Teleports reset planted feet. CreatureLegChecks validates 10,000 extreme/stale foot positions and turn directions for finite poses, bounded limb lengths and endpoints below the hip. Gameplay compilation passes; live Unity verification of the creature appearance and collision-adjacent stepping remains required.

## Vision, idle, dodge and attack animation revision
Enemy sight uses a 110-degree horizontal cone with a 10-metre range plus a blocking linecast. Hearing and being hit create a last-known position, not automatic visual contact. VisionChecks covers front/rear/side/range and 72 facing directions. Idle starts with a 4–9-second wait, followed by occasional 1.5–3-second local patrols and further waits rather than a continuous whole-map route.

Dodge uses a NavMesh-validated eased root displacement of about 0.85–1.25 metres, with crouched/braced body posing and stepping. Claw attacks have separate windup, strike and recovery phases; base damage is checked once during the swing, and descendant lunge impact retains range/sight validation. Nonfatal hits can interrupt attack/dodge with a short, rate-limited stagger and visible torso/head/arm recoil. Test cone detection from behind without making water noise, an actual body sidestep in an open hall, wall blocking, damage interruption, and recovery after a missed attack. Live Unity validation remains pending.

## Atmosphere and floating-label cleanup
World-space text labels for quest nodes, chamber names, district signs and mortuary markers are removed. Aim/proximity interaction prompts and readable journal/puzzle UI remain. Dungeon lighting now uses darker cool ambient light, reduced directional light and dimmer shorter-range torch pools. Exponential-squared fog density is 0.055 with a dark blue-green fog colour.

The named runtime global horror volume has priority 50: ACES tonemapping, -0.35 exposure, 18 contrast, -24 saturation, a cool filter, 0.34 vignette, subtle bloom, 0.12 fine film grain and 0.025 chromatic aberration. The gameplay camera explicitly enables HDR, post-processing, FXAA and the volume layer mask. The faint player fill light is reduced; flashlight and quest-local lights remain. This uses URP distance fog, not a volumetric-fog renderer. Compilation passes; verify readability and fog/grade in live Unity before further darkening.

## Readability retune
After a playtest screenshot showed near-black corridors, ambient light and a 4.5-metre player fill were increased. Exposure is now +0.3, contrast 8, vignette 0.22, and fog density 0.038. Torch pools are slightly brighter/wider. This supersedes the previous darker values; verify the same corridor with flashlight on/off in live Unity.

## Narrow passage masonry repair
Plain iron narrowing cubes are replaced by section-matched rough/damp stone with visible masonry courses. Side masses now extend to the original roof, and a solid textured soffit fills from 2.3 to 3.04 metres, closing the exposed upper void and end faces. Static colliders preserve two metres of lane width and 2.3 metres of head clearance. Geometry uses the dungeon batches and existing wall normal maps. Compilation passes; inspect the same seed in live Unity for roof seams and player/enemy clearance.
