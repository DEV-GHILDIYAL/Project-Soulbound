using UnityEngine;
namespace Soulbound
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(0)] private int ammunition = 12;
        [SerializeField, Min(0)] private int healingCharges = 1;
        public int Ammunition => ammunition;
        public int HealingCharges => healingCharges;
        public int Keys { get; private set; }
        public bool SpendBullet() { if (ammunition <= 0) return false; ammunition--; return true; }
        public int TakeAmmo(int count) { int taken = Mathf.Min(Mathf.Max(0, count), ammunition); ammunition -= taken; return taken; }
        public void AddAmmunition(int amount) => ammunition += Mathf.Max(0, amount);
        public void AddKey() => Keys = Mathf.Min(3, Keys + 1);
        public void AddHealing(int amount) => healingCharges += Mathf.Max(0, amount);
        public bool UseHealing(Health health, float amount)
        {
            if (healingCharges <= 0 || health == null || !health.IsAlive || health.Current >= health.Maximum || amount <= 0) return false;
            healingCharges--; health.Heal(amount); return true;
        }
    }
}
