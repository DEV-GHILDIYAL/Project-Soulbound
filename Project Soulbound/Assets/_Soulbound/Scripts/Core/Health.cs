using System;
using UnityEngine;
namespace Soulbound
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private float maximum = 100f;
        public float Current { get; private set; }
        public float Maximum => maximum;
        public bool IsAlive => Current > 0f;
        public event Action<float> Damaged;
        public event Action Died;
        private void Awake() => Current = maximum;
        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount);
            if (!IsAlive) Died?.Invoke();
        }
        public void Heal(float amount)
        {
            if (IsAlive && amount > 0f) Current = Mathf.Min(maximum, Current + amount);
        }
    }
}
