using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
namespace Soulbound
{
    public sealed class GameplayHUD : MonoBehaviour
    {
        private Health health; private PlayerInventory inventory; private Pistol pistol; private RunState run;
        private SoulCapture capture; private ProceduralMaze maze; private FirstPersonController controller;
        private GameObject root, results; private Text hp, ammo, heal, keys, prompt, reticle, resultText, weaponLabel, debugText;
        private Image healthBar, progress; private RectTransform capturePanel; private Button[] buttons;
        public void Configure(Transform player, RunState state, ProceduralMaze generator)
        {
            health = player.GetComponent<Health>(); inventory = player.GetComponent<PlayerInventory>(); pistol = player.GetComponent<Pistol>();
            capture = player.GetComponent<SoulCapture>(); controller = player.GetComponent<FirstPersonController>(); run = state; maze = generator;
            root = RuntimeUI.Canvas("Gameplay HUD", 50);
            var left = RuntimeUI.Panel(root.transform,"Health",Vector2.zero,new Vector2(24,24),new Vector2(240,86));
            hp = RuntimeUI.Label(left,"",16,8,210,28,22); hp.color = Color.white;
            var bar = RuntimeUI.Rect(left,"Health bar",new Vector2(0,1),new Vector2(0,1),new Vector2(16,-44),new Vector2(208,5));
            healthBar = bar.gameObject.AddComponent<Image>(); healthBar.color = new Color(0.8f,0.3f,0.25f);
            heal = RuntimeUI.Label(left,"",16,55,210,23,14);
            var right = RuntimeUI.Panel(root.transform,"Ammo",new Vector2(1,0),new Vector2(-24,24),new Vector2(170,86));
            ammo = RuntimeUI.Label(right,"",14,7,145,36,26); ammo.alignment = TextAnchor.MiddleRight;
            weaponLabel = RuntimeUI.Label(right,"PISTOL",14,50,145,25,13); weaponLabel.color = RuntimeUI.Gold;
            var top = RuntimeUI.Panel(root.transform,"Keys",Vector2.one,new Vector2(-24,-24),new Vector2(150,42));
            keys = RuntimeUI.Label(top,"",14,4,130,34,18); keys.color = RuntimeUI.Gold;
            var debug = RuntimeUI.Panel(root.transform,"Debug",new Vector2(0,1),new Vector2(24,-24),new Vector2(330,100));
            debugText = RuntimeUI.Label(debug,"",12,8,306,85,14);
            var cross = RuntimeUI.Rect(root.transform,"Crosshair",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(28,28));
            reticle = RuntimeUI.Label(cross,"+",0,0,28,28,20); reticle.alignment = TextAnchor.MiddleCenter;
            capturePanel = RuntimeUI.Panel(root.transform,"Capture",new Vector2(.5f,0),new Vector2(0,125),new Vector2(300,44));
            prompt = RuntimeUI.Label(capturePanel,"",12,0,276,32,15);
            var strip = RuntimeUI.Rect(capturePanel,"Progress",Vector2.zero,Vector2.zero,new Vector2(12,4),new Vector2(276,4));
            progress = strip.gameObject.AddComponent<Image>(); progress.color = Color.cyan;
            var result = RuntimeUI.Panel(root.transform,"Results",new Vector2(.5f,.5f),Vector2.zero,new Vector2(440,310)); results = result.gameObject;
            resultText = RuntimeUI.Label(result,"",24,20,392,75,22);
            buttons = new[] {
                RuntimeUI.Button(result,"New dungeon",24,112,392,40,()=>SceneNavigation.Retry(false)),
                RuntimeUI.Button(result,"Retry same dungeon",24,164,392,40,()=>SceneNavigation.Retry(true)),
                RuntimeUI.Button(result,"Main menu",24,216,392,40,SceneNavigation.Menu) };
            RuntimeUI.Label(result,"D-pad / arrows select · A / Enter confirm",24,272,392,25,13);
        }
        private void LateUpdate()
        {
            if (root == null) return;
            hp.text = $"HEALTH   {health.Current:0}"; healthBar.rectTransform.sizeDelta = new Vector2(208 * health.Current / health.Maximum,5);
            heal.text = $"{(PlayerControls.UsingGamepad ? "X / Square" : "H")}  HEAL · {inventory.HealingCharges}";
            ammo.text = pistol != null ? pistol.Reloading ? "RELOADING" : $"{pistol.Loaded:00} / {inventory.Ammunition:00}" : inventory.Ammunition.ToString();
            keys.text = $"KEYS   {inventory.Keys} / 3";
            weaponLabel.text = pistol != null && pistol.Loaded == 0 && !pistol.Reloading ? (PlayerControls.UsingGamepad ? "Y / Triangle · RELOAD" : "R · RELOAD") : "PISTOL";
            var coordinator = GetComponent<CombatPrototype>();
            debugText.transform.parent.gameObject.SetActive(coordinator != null && coordinator.DebugVisible);
            if (coordinator != null && coordinator.DebugVisible) debugText.text = coordinator.DiagnosticText;
            reticle.gameObject.SetActive(!run.HasEnded && !controller.InputBlocked);
            reticle.color = capture != null && capture.AimedSoul != null ? Color.cyan : Color.white;
            bool active = !run.HasEnded && !controller.InputBlocked && capture != null && capture.AimedSoul != null;
            capturePanel.gameObject.SetActive(active);
            if (active) { prompt.text = $"{PlayerControls.InteractLabel} · CAPTURE · {capture.AimedSoul.Remaining:0.0}s"; progress.rectTransform.sizeDelta = new Vector2(276 * Mathf.Clamp01(capture.Progress / 3f),4); }
            results.SetActive(run.HasEnded);
            if (run.HasEnded)
            {
                resultText.text = (run.HasWon ? "ESCAPED" : "YOU DIED") + $"\nKeys {inventory.Keys}/3 · Seed {(maze != null ? maze.Seed : 0)}";
                var prototype = GetComponent<CombatPrototype>();
                for (int i = 0; i < buttons.Length; i++) buttons[i].GetComponent<Image>().color = prototype != null && prototype.ResultSelection == i ? new Color(.35f,.29f,.15f) : new Color(.14f,.18f,.21f);
            }
        }
        private void OnDestroy() { if (root != null) Destroy(root); }
    }
}
