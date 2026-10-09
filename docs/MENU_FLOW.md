# Menus and scene navigation

Start Play mode from `Assets/_Soulbound/Scenes/MainMenu.unity`. The scene creates its camera, ruined-corridor backdrop, floating soul and menu UI at runtime. Its edit-mode hierarchy is intentionally minimal. It is the first enabled scene in Build Settings, so a player build starts here. The previous sample and prototype scenes remain available.

## Flow

- Main menu → Enter the dungeon → `ProceduralMazePrototype` with a new run.
- Main menu → Settings or How to play → Back.
- Gameplay → Esc / controller Start → pause menu → Resume, Settings, How to play or Return to main menu.
- Win/loss → New dungeon, Retry same dungeon or Main menu.
- Quit exits a player build; in the Unity editor it stops Play mode.

Loading uses scene names and an asynchronous transition overlay with a progress strip. Repeated load requests are ignored while a transition is running. Cursor, time scale and audio pause state are restored across transitions. Returning to the menu discards the current run; no save-game/Continue flow is advertised.

Menus support mouse hover/click, arrows/D-pad selection and Enter/A confirmation. Esc/B goes back; Esc/Start also resumes from the pause home page. Journal/puzzle modals keep ownership of their close input; close them before opening pause. Resume delays gameplay input restoration by one frame to avoid treating the menu confirmation as a shot/interaction.

Pause sets time scale to zero and pauses audio. Enabled enemies are temporarily suspended, since their Update-based impact checks could otherwise still execute at a frozen timestamp; their enabled state is restored on resume. Enemy AI implementation files are unchanged by the menu work.

Settings persist volume and mouse/gamepad look multiplier in PlayerPrefs. This is a focused menu prototype, with no rebinding, graphics-quality or save system yet. The main menu uses a live 3D background, ivory/gold type and restrained dark buttons. Layout follows the existing 1280×720 reference CanvasScaler.

## Verification

Gameplay scripts compile against the installed Unity/package assemblies. Static checks validate the first build scene, existence of all registered scene files, the main-menu MonoBehaviour GUID and scene/build GUID agreement. Live Unity navigation has not been run by the agent.

Playtest mouse and controller selection; menu → game → pause → settings → resume; idle enemies and soul timers while paused; journal Escape ownership; win/loss → each result option; and repeated return-to-menu/play cycles. Check that cursor lock, sound, look settings and time scale recover, and inspect the Console for missing scene/script messages. Also check 16:9 and narrower Game view sizes.

## Peek control update
Hold Q/E to peek left/right. Gamepad uses L3/R3. Peek shifts the camera up to 0.35 metres and rolls up to 12 degrees, smoothly returning on release; opposing inputs cancel. A camera sphere sweep limits displacement against physical walls and props. Menus, focus loss and death reset peek. Interact/soul capture is now F (gamepad A unchanged); flashlight is G (LB unchanged). These bindings override the historical E/F control tables. The How to play page and gameplay prompts use the updated controls. Scripts compile; live corner-clearance and input verification remain pending.
