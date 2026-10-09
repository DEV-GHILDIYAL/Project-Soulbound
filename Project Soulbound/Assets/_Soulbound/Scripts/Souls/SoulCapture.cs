using UnityEngine;
using UnityEngine.InputSystem;
namespace Soulbound
{
    public sealed class SoulCapture : MonoBehaviour
    {
        [SerializeField] private Camera view;
        [SerializeField] private Health health;
        [SerializeField] private RunState run;
        [SerializeField, Min(0)] private float range = 2f;
        [SerializeField] private LayerMask interactionMask = ~0;
        public float Progress { get; private set; }
        private Soul target;
        public Soul Target => target;
        public Soul AimedSoul { get; private set; }
        public float InterruptedUntil { get; private set; }
        private bool interrupted;
        private void OnEnable() { if (health != null) health.Damaged += OnDamage; }
        private void OnDisable() { if (health != null) health.Damaged -= OnDamage; ResetProgress(); }
        private void OnDamage(float amount)
        { interrupted = true; if (Progress > 0) InterruptedUntil = Time.time + 0.8f; ResetProgress(); }
        private void ResetProgress() { target = null; Progress = 0f; }
        private void LateUpdate()
        {
            AimedSoul = null;
            if (interrupted) { interrupted = false; return; }
            if (view == null
                || Cursor.lockState != CursorLockMode.Locked || (health != null && !health.IsAlive)
                || (run != null && run.HasEnded)) { ResetProgress(); return; }
            if (!Physics.Raycast(view.transform.position, view.transform.forward, out RaycastHit hit, range, interactionMask, QueryTriggerInteraction.Collide))
            { ResetProgress(); return; }
            var soul = hit.collider.GetComponentInParent<Soul>();
            if (soul == null || soul.Resolved) { ResetProgress(); return; }
            AimedSoul = soul;
            if (!PlayerControls.InteractHeld) { ResetProgress(); return; }
            if (target != soul) { ResetProgress(); target = soul; }
            Progress += Time.deltaTime;
            if (Progress >= Soul.CaptureDuration) { soul.Capture(); ResetProgress(); }
        }
    }
}
