using UnityEngine;
using UnityEngine.InputSystem;
namespace Soulbound
{
    public sealed class Pistol : MonoBehaviour
    {
        [SerializeField] private Camera view;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Health owner;
        [SerializeField] private RunState run;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField, Min(0)] private float damage = 25f;
        [SerializeField, Min(0)] private float range = 40f;
        [SerializeField, Min(0.01f)] private float shotInterval = 0.3f;
        private float nextShot;
        public int Loaded { get; private set; }
        public const int MagazineSize = 8;
        public bool Reloading { get; private set; }
        private float reloadEnds;
        private void Start() { if (inventory != null) Loaded = inventory.TakeAmmo(MagazineSize); }
        public string Feedback { get; private set; }
        public float FeedbackUntil { get; private set; }
        public event System.Action Fired;
        private void ShowFeedback(string message)
        { Feedback = message; FeedbackUntil = Time.time + 0.25f; }
        private void Update()
        {
            var controller = owner != null ? owner.GetComponent<FirstPersonController>() : null;
            if (view == null || inventory == null || (owner != null && !owner.IsAlive) || (run != null && run.HasEnded)) { Reloading = false; return; }
            if (Reloading)
            {
                if (Time.time >= reloadEnds) { Loaded += inventory.TakeAmmo(MagazineSize - Loaded); Reloading = false; }
                return;
            }
            if ((controller != null && controller.InputBlocked) || Cursor.lockState != CursorLockMode.Locked) return;
            if (PlayerControls.ReloadPressed && Loaded < MagazineSize && inventory.Ammunition > 0)
            { Reloading = true; reloadEnds = Time.time + 1.4f; return; }
            if (!PlayerControls.ShootPressed || Time.time < nextShot) return;
            if (Loaded <= 0) { ShowFeedback("EMPTY"); return; }
            Loaded--;
            nextShot = Time.time + shotInterval;
            Fired?.Invoke();
            ShowFeedback("SHOT");
            if (Physics.Raycast(view.transform.position, view.transform.forward, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                var target = hit.collider.GetComponentInParent<Health>();
                if (target != null && target != owner && target.IsAlive)
                { target.TakeDamage(damage); ShowFeedback("HIT"); }
            }
        }
    }
}
