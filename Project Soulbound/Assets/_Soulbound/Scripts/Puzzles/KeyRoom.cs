using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Soulbound
{
    public sealed class KeyRoom : MonoBehaviour
    {
        private Transform player, door, chest;
        private FirstPersonController controller;
        private PlayerInventory inventory;
        private Health health;
        private RunState run;
        private RoomChallenge challenge;
        private Collider doorCollider;
        private NavMeshObstacle obstacle;
        private Vector3 panelPosition, doorClosed;
        private bool modal, unlocked, collected;
        private string message;
        private float raised;
        private int selection, openedFrame;
        private GameObject uiRoot, menuPanel;
        private Text cluesText, stateText, hintText, contextText;
        private Button[] menuButtons;
        private bool interactionWasHeld;
        private InputAction interactionAction;
        private bool queuedInteraction;
        private HorrorDungeon journey;
        private int journeyKey;
        public RoomChallenge Challenge => challenge;
        public void SetJourney(HorrorDungeon value, int key) { journey = value; journeyKey = key; }
        public void Configure(Transform target, RunState state, Transform gate, Transform reward, Vector3 panel, int seed, int kind)
        {
            player = target; run = state; door = gate; chest = reward; panelPosition = panel;
            controller = player.GetComponent<FirstPersonController>(); inventory = player.GetComponent<PlayerInventory>();
            health = player.GetComponent<Health>(); health.Damaged += CloseOnDamage;
            doorClosed = door.position; doorCollider = door.GetComponent<Collider>();
            obstacle = door.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Vector3.one; obstacle.carving = true; obstacle.carveOnlyStationary = false;
            challenge = new RoomChallenge(seed, kind); BuildUI();
            interactionAction = new InputAction("Room interact", InputActionType.Button);
            interactionAction.AddBinding("<Keyboard>/f"); interactionAction.AddBinding("<Gamepad>/buttonSouth");
            interactionAction.performed += context => queuedInteraction = true; interactionAction.Enable();
        }
        private void CloseOnDamage(float damage) { message = "Interrupted by enemy!"; Close(); }
        private void Close() { if (!modal) return; modal = false; if (menuPanel != null) menuPanel.SetActive(false); if (controller != null) controller.SetInteractionMode(false); }
        private void OnDestroy() { if (health != null) health.Damaged -= CloseOnDamage; interactionAction?.Dispose(); Close(); if (uiRoot != null) Destroy(uiRoot); }
        private bool Nearby(Vector3 point, float range)
        {
            if (player == null) return false;
            Vector3 eye = player.position + Vector3.up * 1.4f;
            if (Vector3.Distance(player.position, point) > range) return false;
            return !Physics.Linecast(eye, point + Vector3.up * 1.2f, 1 << 0, QueryTriggerInteraction.Ignore);
        }
        private bool CanUseDoor(float range = 2.6f)
        {
            if (player == null || doorCollider == null) return false;
            var view = player.GetComponentInChildren<Camera>();
            Vector3 eye = view != null ? view.transform.position : player.position + Vector3.up * 1.4f;
            Vector3 point = doorCollider.ClosestPoint(eye);
            if (Vector3.Distance(eye, point) > range) return false;
            // Test the actual gate surface, not an invisible offset interaction point.
            if (!Physics.Linecast(eye, point, out RaycastHit hit, ~(1 << 2), QueryTriggerInteraction.Ignore)) return true;
            return hit.collider == doorCollider;
        }
        private void Update()
        {
            bool held = (Keyboard.current != null && Keyboard.current.fKey.isPressed) || (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed);
            bool pressed = queuedInteraction || (held && !interactionWasHeld); queuedInteraction = false;
            interactionWasHeld = held;
            if (run.HasEnded || !health.IsAlive) { Close(); if (contextText != null) contextText.gameObject.SetActive(false); return; } UpdateUI();
            if (unlocked)
            {
                raised = Mathf.MoveTowards(raised, 3.2f, Time.deltaTime * 2f);
                door.position = doorClosed + Vector3.up * raised;
                if (!collected && !controller.InputBlocked && Nearby(chest.position, 2f) && pressed)
                { collected = true; inventory.AddKey(); message = "Key collected."; }
                return;
            }
            if (modal && PlayerControls.CancelPressed) { Close(); return; }
            if (!modal && !controller.InputBlocked && CanUseDoor() && pressed)
            {
                if (journey != null && !journey.State.Ready(journeyKey))
                { journey.Toast(journeyKey == 0 ? "The crypt seal needs the caretaker's crest placed at the correct chapel saint." : journeyKey == 1 ? "The furnace is cold. Find the maintenance log and restore the boiler." : "The ritual is incomplete. Find all three journal pages and fit the medallion."); return; }
                if (journey != null && journeyKey < 2) { Unlock(); return; }
                modal = true; selection = 0; openedFrame = Time.frameCount; controller.SetInteractionMode(true); menuPanel.SetActive(true); UpdateUI();
            }
            if (modal && Time.frameCount > openedFrame + 1)
            {
                int count = challenge.Kind == 0 ? 5 : challenge.State.Length;
                selection = (selection + PlayerControls.MenuStep + count + 3) % (count + 3);
                if (PlayerControls.ConfirmPressed)
                {
                    if (selection < count) { if (challenge.Kind == 0) challenge.EnterRune(selection); else challenge.Move(selection); }
                    else if (selection == count) Solve();
                    else if (selection == count + 1) { challenge.Reset(); message = ""; }
                    else Close();
                }
            }
        }
        private void Solve()
        {
            if (!challenge.IsSolved) { message = "The mechanism is not aligned."; return; }
            Unlock();
        }
        private void Unlock() { unlocked = true; doorCollider.enabled = false; obstacle.enabled = false; Close(); }
        private void BuildUI()
        {
            uiRoot = RuntimeUI.Canvas("Chamber Puzzle UI", 60);
            var panel = RuntimeUI.Panel(uiRoot.transform,"Mechanism",new Vector2(.5f,.5f),Vector2.zero,new Vector2(660,420)); menuPanel = panel.gameObject;
            RuntimeUI.Label(panel,challenge.Kind == 0 ? "RUNE CHAMBER" : challenge.Kind == 1 ? "FLAME CHAMBER" : "ASTRAL CHAMBER",24,14,612,35,25).color = RuntimeUI.Gold;
            cluesText = RuntimeUI.Label(panel,"",24,55,612,130,17);
            stateText = RuntimeUI.Label(panel,"",24,191,612,45,16);
            int count = challenge.Kind == 0 ? 5 : challenge.State.Length;
            menuButtons = new Button[count + 3];
            for (int i = 0; i < count; i++)
            {
                int choice = i; float width = 612f / count;
                menuButtons[i] = RuntimeUI.Button(panel,"",24 + i * width,245,width - 8,52,()=> { Activate(choice); UpdateUI(); });
            }
            menuButtons[count] = RuntimeUI.Button(panel,"Unlock",24,318,196,40,Solve);
            menuButtons[count+1] = RuntimeUI.Button(panel,"Reset",232,318,196,40,()=> { challenge.Reset(); message = ""; UpdateUI(); });
            menuButtons[count+2] = RuntimeUI.Button(panel,"Leave",440,318,196,40,Close);
            hintText = RuntimeUI.Label(panel,"",24,375,612,28,14);
            var context = RuntimeUI.Rect(uiRoot.transform,"Door prompt",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,85),new Vector2(420,30));
            contextText = RuntimeUI.Label(context,"",0,0,420,30,17); contextText.alignment = TextAnchor.MiddleCenter;
            menuPanel.SetActive(false);
        }
        private void Activate(int choice)
        { if (challenge.Kind == 0) challenge.EnterRune(choice); else challenge.Move(choice); }
        private void UpdateUI()
        {
            if (uiRoot == null) return;
            menuPanel.SetActive(modal);
            string prompt = !unlocked && CanUseDoor() ? PlayerControls.InteractLabel + ": inspect room mechanism" : unlocked && !collected && Nearby(chest.position,2f) ? PlayerControls.InteractLabel + ": collect key" : null;
            contextText.gameObject.SetActive(!modal && !controller.InputBlocked && prompt != null); contextText.text = prompt;
            if (!modal) return;
            cluesText.text = string.Join("\n",challenge.Clues);
            if (challenge.Kind == 0)
            { string entry = "Order: "; foreach (int symbol in challenge.State) entry += RoomChallenge.Symbols[symbol] + "   "; stateText.text = entry; }
            else if (challenge.Kind == 1)
            { string target = "Target: "; foreach (int bit in challenge.Target) target += bit == 1 ? "ON   " : "off   "; stateText.text = target; }
            else stateText.text = "Turning one ring also advances its neighbour.";
            int count = menuButtons.Length - 3;
            for (int i = 0; i < menuButtons.Length; i++)
            {
                menuButtons[i].GetComponent<Image>().color = i == selection ? new Color(.35f,.29f,.15f) : new Color(.14f,.18f,.21f);
                if (i < count) menuButtons[i].GetComponentInChildren<Text>().text = challenge.Kind == 0 ? RoomChallenge.Symbols[i] : challenge.Kind == 1 ? $"Torch {i+1}\n{(challenge.State[i] == 1 ? "ON" : "off")}" : $"Ring {i+1}\n{RoomChallenge.Symbols[challenge.State[i]]}";
            }
            hintText.text = string.IsNullOrEmpty(message) ? "D-pad / arrows select · A / Enter confirm · B / Esc leave" : message;
        }
    }
}
