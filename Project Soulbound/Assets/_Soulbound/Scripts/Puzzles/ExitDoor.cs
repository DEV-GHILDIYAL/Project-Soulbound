using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
namespace Soulbound
{
    public sealed class ExitDoor : MonoBehaviour
    {
        [SerializeField] private RunState run;
        public bool IsOpen { get; private set; }
        public UnityEvent opened = new UnityEvent();
        private Transform player;
        private PlayerInventory inventory;
        private Vector3 closedPosition;
        private float lift;
        public void Configure(Transform target, RunState state)
        { player = target; inventory = target.GetComponent<PlayerInventory>(); run = state; closedPosition = transform.position; }
        private bool Nearby()
        {
            if (player == null || Vector3.Distance(player.position + Vector3.up, closedPosition) > 2.2f) return false;
            return !Physics.Linecast(player.position + Vector3.up * 1.4f, closedPosition, 1 << 0, QueryTriggerInteraction.Ignore);
        }
        private void Update()
        {
            if (IsOpen)
            { lift = Mathf.MoveTowards(lift, 3f, Time.deltaTime * 2f); transform.position = closedPosition + Vector3.up * lift; return; }
            if (run == null || run.HasEnded || player == null || player.GetComponent<FirstPersonController>().InputBlocked) return;
            if (Nearby() && PlayerControls.InteractPressed) TryOpen(inventory);
        }
        private void OnGUI()
        {
            if (run == null || run.HasEnded || !Nearby()) return;
            GUI.Box(new Rect(Screen.width / 2f - 180, Screen.height - 100, 360, 32), inventory.Keys < 3 ? $"Exit sealed — keys {inventory.Keys}/3" : PlayerControls.InteractLabel + ": open exit and escape");
        }
        public bool TryOpen(PlayerInventory inventory)
        {
            if (IsOpen || run == null || run.HasEnded || inventory == null || inventory.Keys < 3) return false;
            IsOpen = true;
            var blocker = GetComponent<Collider>(); if (blocker != null) blocker.enabled = false;
            opened.Invoke(); run.Win(); return true;
        }
    }
}
