using UnityEngine;
namespace Soulbound
{
    public sealed class SupplyPickup : MonoBehaviour
    {
        private Transform player;
        private RunState run;
        private bool healing, collected;
        private Vector3 origin;
        public void Configure(Transform target, RunState state, bool heal)
        { player = target; run = state; healing = heal; origin = transform.position; }
        private void Update()
        {
            if (collected || player == null || run.HasEnded) return;
            transform.position = origin + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.06f);
            if (Vector3.Distance(player.position, origin) > 1.3f) return;
            if (Physics.Linecast(player.position + Vector3.up * 1.4f, origin, 1 << 0, QueryTriggerInteraction.Ignore)) return;
            collected = true;
            var inventory = player.GetComponent<PlayerInventory>();
            if (healing) inventory.AddHealing(1); else inventory.AddAmmunition(6);
            gameObject.SetActive(false); Destroy(gameObject);
        }
        private void OnGUI()
        {
            if (player == null || collected || run.HasEnded || Vector3.Distance(player.position, origin) > 2.5f) return;
            if (Physics.Linecast(player.position + Vector3.up * 1.4f, origin, 1 << 0, QueryTriggerInteraction.Ignore)) return;
            GUI.Label(new Rect(Screen.width / 2f - 110, Screen.height - 170, 240, 25), healing ? "Healing supply · collect nearby" : "Ammo +6 · collect nearby");
        }
    }
}
