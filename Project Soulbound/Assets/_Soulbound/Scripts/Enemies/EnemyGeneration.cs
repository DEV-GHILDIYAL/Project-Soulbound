using UnityEngine;
namespace Soulbound
{
    // Basic AI consumes these stats; offspring spawning and attack-type upgrades come later.
    public sealed class EnemyGeneration : MonoBehaviour
    {
        [SerializeField, Min(0)] private int generation;
        [SerializeField, Min(0)] private float baseSpeed = 1.25f;
        [SerializeField, Min(0)] private float baseDamage = 10f;
        [SerializeField, Min(0)] private float speedIncrease = 0.1f;
        [SerializeField, Min(0)] private float damageIncrease = 0.05f;
        public int Generation => generation;
        public float Speed => baseSpeed * (1f + generation * speedIncrease);
        public float Damage => baseDamage * (1f + generation * damageIncrease);
        public void SetGeneration(int value) => generation = Mathf.Max(0, value);
        // Descendants keep their speed/damage upgrades and the prototype lunge attack.
    }
}
