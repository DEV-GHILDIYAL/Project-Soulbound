using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
namespace Soulbound
{
    public sealed class CombatPrototype : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Health playerHealth;
        [SerializeField] private RunState run;
        [SerializeField] private Material enemyMaterial;
        [SerializeField] private Material soulMaterial;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private SoulCapture capture;
        [SerializeField] private ProceduralMaze maze;
        private readonly List<EnemyAI> enemies = new List<EnemyAI>();
        private readonly List<Soul> souls = new List<Soul>();
        private readonly List<Soul> pending = new List<Soul>();
        private Pistol pistol;
        private NavMeshSurface surface;
        private string setupError;
        private float nextSpawnRetry;
        private int captures, duplications;
        private float startedAt, endedAt;
        private bool showDebug;
        private int resultSelection, endedFrame;
        private GameplayHUD canvasHud;
        public int ResultSelection => resultSelection;
        public bool DebugVisible => showDebug;
        public string DiagnosticText => $"F3 · DEBUG\nSeed {(maze != null ? maze.Seed : 0)} · Enemies {enemies.Count}\nSouls {souls.Count} · Captured {captures}\n{setupError ?? "Navigation ready"}";
        private void Start()
        {
            if (player == null || playerHealth == null || run == null || inventory == null || capture == null)
            { setupError = "Missing soul prototype references."; Debug.LogError(setupError, this); return; }
            playerHealth.Died += OnPlayerDeath;
            run.won.AddListener(OnRunEnded); run.lost.AddListener(OnRunEnded);
            startedAt = Time.time;
            SetLayer(player, 2);
            if (maze != null) maze.Generate(player);
            surface = gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = 1 << 0;
            surface.BuildNavMesh();
            if (!NavMesh.SamplePosition(maze != null ? maze.EnemyPosition : new Vector3(3, 0, 7), out NavMeshHit spawn, 3f, NavMesh.AllAreas))
            { setupError = "No walkable spawn. Check Navigation settings."; Debug.LogError(setupError, this); return; }
            SpawnEnemy(spawn.position, 0);
            if (maze != null)
            {
                var path = new NavMeshPath();
                var required = new List<Vector3> { maze.CellPosition(maze.Layout.Exit), spawn.position };
                required.AddRange(maze.RoomApproaches);
                var horror = maze.GetComponent<HorrorDungeon>();
                if (horror != null) required.AddRange(horror.RequiredApproaches);
                foreach (Vector3 point in required)
                    if (!NavMesh.CalculatePath(maze.StartPosition, point, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                    { setupError = "Navigation route validation failed for seed " + maze.Seed; Debug.LogError(setupError, this); }
            }
            pistol = player.GetComponent<Pistol>();
            canvasHud = gameObject.AddComponent<GameplayHUD>(); canvasHud.Configure(player, run, maze);
            gameObject.AddComponent<MenuController>().Configure(player,run);
            if (pistol != null) player.gameObject.AddComponent<PistolViewModel>().Configure(player.GetComponentInChildren<Camera>(), pistol, enemyMaterial);
        }
        private void SpawnEnemy(Vector3 position, int generation)
        {
            var root = new GameObject("Enemy G" + generation);
            root.transform.position = position;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Enemy Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up;
            body.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
            body.GetComponent<Renderer>().sharedMaterial = enemyMaterial;
            body.GetComponent<Renderer>().enabled = false;
            var agent = root.AddComponent<NavMeshAgent>();
            agent.height = 2f; agent.radius = 0.35f; agent.acceleration = 12f; agent.angularSpeed = 360f;
            root.AddComponent<Health>();
            var stats = root.AddComponent<EnemyGeneration>();
            stats.SetGeneration(generation);
            root.AddComponent<DungeonGuardVisual>().Build(enemyMaterial, generation);
            var enemy = root.AddComponent<EnemyAI>();
            enemy.Configure(player, playerHealth, run, maze != null ? maze.Patrol : new[] {
                new Vector3(3, 0, 7), new Vector3(3, 0, -5),
                new Vector3(-8, 0, -5), new Vector3(-8, 0, 7) });
            enemy.Defeated += SpawnSoul;
            enemies.Add(enemy);
        }
        private void SpawnSoul(Vector3 position, int generation)
        {
            if (run.HasEnded) return;
            var root = new GameObject("Soul G" + generation);
            root.transform.position = position;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Soul Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.up * 1.35f;
            visual.transform.localScale = Vector3.one * 0.65f;
            visual.GetComponent<Collider>().isTrigger = true;
            visual.GetComponent<Renderer>().sharedMaterial = soulMaterial;
            visual.GetComponent<Renderer>().enabled = false;
            var agent = root.AddComponent<NavMeshAgent>();
            agent.height = 1.8f; agent.radius = 0.3f; agent.speed = 1.65f;
            agent.acceleration = 14f; agent.angularSpeed = 720f;
            agent.stoppingDistance = 0.1f;
            var soul = root.AddComponent<Soul>();
            soul.Configure(player, run, generation);
            root.AddComponent<SoulVisual>().Build(soulMaterial);
            soul.captured.AddListener(() => { captures++; souls.Remove(soul); });
            soul.expired.AddListener(() => { if (!pending.Contains(soul)) pending.Add(soul); });
            souls.Add(soul);
        }
        private bool TryFindPair(Vector3 origin, out Vector3 first, out Vector3 second)
        {
            first = second = origin;
            bool foundFirst = false;
            var path = new NavMeshPath();
            // Validate the entire pair before creating either enemy; no silent cap or lost child.
            for (int ring = 1; ring <= 4; ring++)
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6f;
                Vector3 requested = origin + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * ring;
                if (!NavMesh.SamplePosition(requested, out NavMeshHit sample, 0.6f, NavMesh.AllAreas)) continue;
                Vector3 point = sample.position;
                Vector3 fromPlayer = point - player.position; fromPlayer.y = 0;
                if (fromPlayer.sqrMagnitude < 1.6f * 1.6f) continue;
                if (!NavMesh.CalculatePath(origin, point, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                if (Physics.CheckCapsule(point + Vector3.up * 0.45f, point + Vector3.up * 1.55f,
                    0.38f, ~(1 << 2), QueryTriggerInteraction.Ignore)) continue;
                if (!foundFirst) { first = point; foundFirst = true; }
                else if ((point - first).sqrMagnitude >= 1.2f * 1.2f) { second = point; return true; }
            }
            return false;
        }
        private static void SetLayer(Transform node, int layer)
        { node.gameObject.layer = layer; foreach (Transform child in node) SetLayer(child, layer); }
        private void OnPlayerDeath() => run.Lose();
        private void OnRunEnded()
        {
            endedAt = Time.time; endedFrame = Time.frameCount; resultSelection = 0;
            player.GetComponent<FirstPersonController>().SetInteractionMode(true);
        }
        private void OnDestroy()
        {
            if (playerHealth != null) playerHealth.Died -= OnPlayerDeath;
            if (run != null) { run.won.RemoveListener(OnRunEnded); run.lost.RemoveListener(OnRunEnded); }
            foreach (var enemy in enemies) if (enemy != null) enemy.Defeated -= SpawnSoul;
            if (surface != null) surface.RemoveData();
        }
        private void Update()
        {
            if (maze != null && Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            { maze.ReplayNextRun(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); return; }
            if (run != null && run.HasEnded && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); return; }
            enemies.RemoveAll(enemy => enemy == null);
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) showDebug = !showDebug;
            if (run != null && run.HasEnded)
            {
                if(SceneNavigation.IsLoading)return;
                resultSelection = (resultSelection + PlayerControls.MenuStep + 3) % 3;
                if (Time.frameCount > endedFrame && PlayerControls.ConfirmPressed)
                { if(resultSelection==2)SceneNavigation.Menu();else SceneNavigation.Retry(resultSelection==1); }
                return;
            }
            if (run != null && !run.HasEnded && player != null && !player.GetComponent<FirstPersonController>().InputBlocked
                && PlayerControls.HealPressed)
                inventory.UseHealing(playerHealth, 30f);
            if (run == null || run.HasEnded || Time.time < nextSpawnRetry) return;
            nextSpawnRetry = Time.time + 0.2f;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                Soul soul = pending[i];
                if (soul == null) { pending.RemoveAt(i); continue; }
                if (!TryFindPair(soul.transform.position, out Vector3 first, out Vector3 second)) continue;
                int generation = soul.Generation + 1;
                pending.RemoveAt(i); souls.Remove(soul);
                soul.gameObject.SetActive(false);
                SpawnEnemy(first, generation); SpawnEnemy(second, generation);
                duplications++;
                Destroy(soul.gameObject);
                Physics.SyncTransforms();
            }
        }
        private GUIStyle hudText, numberText, smallText, captionText;
        private void OnGUI()
        {
            if (canvasHud != null) return;
            if (hudText == null)
            {
                hudText = TextStyle(15, new Color(0.95f, 0.93f, 0.86f));
                numberText = TextStyle(26, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
                smallText = TextStyle(12, new Color(0.83f, 0.85f, 0.82f));
                captionText = TextStyle(12, new Color(0.90f, 0.78f, 0.52f), TextAnchor.MiddleLeft, FontStyle.Bold);
            }
            if (playerHealth == null || inventory == null || run == null) return;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousTint = GUI.color, previousContent = GUI.contentColor;
            GUI.color = Color.white; GUI.contentColor = Color.white;
            float uiScale = Mathf.Clamp(Screen.height / 900f, 0.9f, 1.4f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * uiScale);
            float w = Screen.width / uiScale, h = Screen.height / uiScale;
            bool modal = player.GetComponent<FirstPersonController>().InputBlocked;
            if (!run.HasEnded)
            {
                float left = 32, bottom = h - 32;
                Color gold = new Color(0.70f, 0.58f, 0.34f);
                Color crimson = playerHealth.Current <= 25 ? new Color(0.94f, 0.27f, 0.22f) : new Color(0.69f, 0.30f, 0.26f);
                // Soft compact plates, icon-led status and generous safe-area margins.
                Card(new Rect(left, bottom - 78, 214, 78));
                Fill(new Rect(left + 14, bottom - 65, 2, 18), crimson);
                GUI.Label(new Rect(left + 24, bottom - 67, 100, 20), "HEALTH", captionText);
                GUI.Label(new Rect(left + 143, bottom - 74, 56, 36), playerHealth.Current.ToString("0"), numberText);
                float fraction = Mathf.Clamp01(playerHealth.Current / playerHealth.Maximum);
                for (int i = 0; i < 10; i++)
                {
                    float segment = Mathf.Clamp01(fraction * 10 - i);
                    Fill(new Rect(left + 16 + i * 18.2f, bottom - 37, 16, 4), new Color(0.23f, 0.24f, 0.24f));
                    if (segment > 0) Fill(new Rect(left + 16 + i * 18.2f, bottom - 37, 16 * segment, 4), crimson);
                }
                string heal = PlayerControls.UsingGamepad ? "X" : "H";
                Badge(new Rect(left + 15, bottom - 25, 19, 16), heal);
                GUI.Label(new Rect(left + 42, bottom - 26, 152, 20), $"HEAL  ·  {inventory.HealingCharges} charges", smallText);

                float ammoX = w - 146;
                Card(new Rect(ammoX, bottom - 78, 114, 78));
                Fill(new Rect(ammoX + 16, bottom - 64, 5, 18), gold);
                Fill(new Rect(ammoX + 14, bottom - 43, 9, 2), gold);
                GUI.Label(new Rect(ammoX + 37, bottom - 74, 61, 38), inventory.Ammunition.ToString("00"), numberText);
                Fill(new Rect(ammoX + 16, bottom - 33, 82, 1), new Color(0.70f, 0.58f, 0.34f, 0.3f));
                GUI.Label(new Rect(ammoX + 16, bottom - 24, 90, 18), "PISTOL AMMO", captionText);

                float keysX = w - 207;
                Card(new Rect(keysX, 30, 175, 49));
                GUI.Label(new Rect(keysX + 14, 37, 88, 20), "KEYS", captionText);
                for (int i = 0; i < 3; i++) KeyIcon(keysX + 20 + i * 24, 59, i < inventory.Keys ? gold : new Color(0.34f, 0.37f, 0.36f));
                GUI.Label(new Rect(keysX + 111, 45, 49, 25), $"{inventory.Keys} / 3", hudText);
                if (!modal && Cursor.lockState == CursorLockMode.Locked)
                {
                    float x = w / 2, y = h / 2;
                    Color reticle = capture != null && capture.AimedSoul != null ? new Color(0.4f, 0.9f, 0.85f) : new Color(0.9f, 0.9f, 0.85f, 0.85f);
                    Fill(new Rect(x - 1, y - 1, 2, 2), reticle);
                    Fill(new Rect(x - 10, y, 5, 1), reticle); Fill(new Rect(x + 5, y, 5, 1), reticle);
                    Fill(new Rect(x, y - 10, 1, 5), reticle); Fill(new Rect(x, y + 5, 1, 5), reticle);
                    if (pistol != null && Time.time < pistol.FeedbackUntil && pistol.Feedback == "HIT")
                    {
                        Fill(new Rect(x - 12, y - 12, 4, 2), Color.white); Fill(new Rect(x + 8, y + 10, 4, 2), Color.white);
                    }
                }
                if (pistol != null && Time.time < pistol.FeedbackUntil && pistol.Feedback == "EMPTY")
                    GUI.Label(new Rect(w - 146, h - 137, 114, 25), "NO AMMUNITION", smallText);
                if (!modal && capture != null && capture.AimedSoul != null)
                {
                    GUI.Label(new Rect(w / 2 - 130, h - 155, 260, 25), $"{PlayerControls.InteractLabel}  CAPTURE  ·  {capture.AimedSoul.Remaining:0.0}s", hudText);
                    Fill(new Rect(w / 2 - 120, h - 127, 240, 5), new Color(0.15f, 0.2f, 0.2f));
                    Fill(new Rect(w / 2 - 120, h - 127, 240 * Mathf.Clamp01(capture.Progress / Soul.CaptureDuration), 5), new Color(0.35f, 0.8f, 0.75f));
                }
                else if (!modal && capture != null && Time.time < capture.InterruptedUntil)
                    GUI.Label(new Rect(w / 2 - 125, h - 135, 280, 25), "Capture interrupted", smallText);
                if (setupError != null) GUI.Label(new Rect(20, 70, 480, 25), setupError, smallText);
                if (showDebug)
                {
                    Panel(new Rect(20, 20, 370, 120));
                    GUI.Label(new Rect(30, 28, 350, 100), $"DEBUG · F3 to hide\nSeed: {(maze != null ? maze.Seed : 0)} | R new / F5 replay\nEnemies {enemies.Count} · Souls {souls.Count}\nCaptured {captures} · Duplications {duplications} · Pending {pending.Count}", smallText);
                }
            }
            else
            {
                float x = w / 2 - 210, y = h / 2 - 125;
                Panel(new Rect(x, y, 420, 250));
                GUI.Label(new Rect(x + 25, y + 20, 370, 30), run.HasWon ? "ESCAPED" : "YOU DIED", hudText);
                GUI.Label(new Rect(x + 25, y + 60, 370, 30), $"{endedAt - startedAt:0}s  ·  Keys {inventory.Keys}/3  ·  Captures {captures}", smallText);
                if (maze != null) GUI.Label(new Rect(x + 25, y + 87, 370, 25), $"Seed {maze.Seed}", smallText);
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = resultSelection == 0 ? new Color(0.35f, 0.8f, 0.7f) : previous;
                if (GUI.Button(new Rect(x + 25, y + 120, 370, 35), "New run")) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                GUI.backgroundColor = resultSelection == 1 ? new Color(0.35f, 0.8f, 0.7f) : previous;
                if (maze != null && GUI.Button(new Rect(x + 25, y + 165, 370, 35), "Retry this dungeon"))
                { maze.ReplayNextRun(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
                GUI.backgroundColor = previous;
                GUI.Label(new Rect(x + 25, y + 213, 370, 25), "D-pad / arrows select · A / Enter confirm", smallText);
            }
            GUI.matrix = previousMatrix; GUI.color = previousTint; GUI.contentColor = previousContent;
        }
        private static GUIStyle TextStyle(int size, Color colour, TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle weight = FontStyle.Normal)
        {
            var style = new GUIStyle { font = GUI.skin.font, fontSize = size, alignment = alignment,
                fontStyle = weight, clipping = TextClipping.Clip, wordWrap = false, padding = new RectOffset(0, 0, 0, 0) };
            style.normal.textColor = colour; style.hover.textColor = colour; style.active.textColor = colour; style.focused.textColor = colour;
            style.onNormal.textColor = colour; style.onHover.textColor = colour; style.onActive.textColor = colour; style.onFocused.textColor = colour;
            return style;
        }
        private static void Card(Rect rect)
        {
            GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 3, rect.width, rect.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0, 0, 0.18f), 0, 7);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.045f, 0.055f, 0.06f, 0.78f), 0, 7);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.57f, 0.51f, 0.36f, 0.22f), 1, 7);
        }
        private void Badge(Rect rect, string text)
        {
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.65f, 0.65f, 0.58f, 0.3f), 1, 3);
            var style = new GUIStyle(smallText) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(rect, text, style);
        }
        private static void KeyIcon(float x, float y, Color color)
        {
            GUI.DrawTexture(new Rect(x - 4, y - 3, 8, 8), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, color, 1.5f, 4);
            Fill(new Rect(x - 1, y + 4, 2, 10), color);
            Fill(new Rect(x + 1, y + 9, 4, 2), color);
            Fill(new Rect(x + 1, y + 12, 3, 2), color);
        }
        private static void Panel(Rect rect) => Fill(rect, new Color(0.025f, 0.035f, 0.04f, 0.78f));
        private static void Fill(Rect rect, Color color)
        { Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous; }
    }
}
