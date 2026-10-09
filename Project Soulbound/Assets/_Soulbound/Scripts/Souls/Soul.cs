using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
namespace Soulbound
{
    public sealed class Soul : MonoBehaviour
    {
        public const float Lifetime = 10f;
        public const float CaptureDuration = 3f;
        [SerializeField] private RunState run;
        public UnityEvent captured = new UnityEvent();
        public UnityEvent expired = new UnityEvent();
        public float Remaining => frozen ? frozenRemaining : Mathf.Max(0f, deadline - Time.time);
        public bool Resolved { get; private set; }
        public int Generation { get; private set; }
        private NavMeshAgent agent;
        private Transform player;
        private float deadline, nextRepath;
        private float frozenRemaining;
        private bool frozen;
        private NavMeshPath path;
        private void Awake() => deadline = Time.time + Lifetime;
        public void Configure(Transform target, RunState state, int generation)
        {
            player = target; run = state; Generation = generation;
            agent = GetComponent<NavMeshAgent>(); path = new NavMeshPath();
            deadline = Time.time + Lifetime;
        }
        private void Update()
        {
            if (run != null && run.HasEnded)
            {
                if (!frozen) { frozenRemaining = Remaining; frozen = true; }
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                return;
            }
            if (Resolved) return;
            if (Remaining <= 0f)
            {
                Resolved = true;
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                expired.Invoke(); return;
            }
            // Capture never stops fleeing; souls have no damage component.
            if (player == null || agent == null || !agent.isOnNavMesh || Time.time < nextRepath) return;
            nextRepath = Time.time + 0.3f;
            Vector3 away = transform.position - player.position;
            away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = transform.forward;
            away.Normalize();
            float bestScore = float.NegativeInfinity;
            Vector3 best = transform.position;
            for (int i = 0; i < 8; i++)
            {
                Vector3 direction = Quaternion.Euler(0, i * 45f, 0) * away;
                if (!NavMesh.SamplePosition(transform.position + direction * 3f, out NavMeshHit candidate, 1f, agent.areaMask)) continue;
                if (!NavMesh.CalculatePath(transform.position, candidate.position, agent.areaMask, path)
                    || path.status != NavMeshPathStatus.PathComplete) continue;
                float score = (candidate.position - player.position).sqrMagnitude;
                if (score > bestScore) { bestScore = score; best = candidate.position; }
            }
            if (bestScore > float.NegativeInfinity) agent.SetDestination(best);
        }
        public bool Capture()
        {
            if (Resolved || Remaining <= 0f || (run != null && run.HasEnded)) return false;
            Resolved = true; captured.Invoke(); gameObject.SetActive(false); Destroy(gameObject); return true;
        }
    }
}
