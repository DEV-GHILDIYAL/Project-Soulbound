using UnityEngine;
using UnityEngine.Events;
namespace Soulbound
{
    public sealed class RunState : MonoBehaviour
    {
        public bool HasEnded { get; private set; }
        public bool HasWon { get; private set; }
        public UnityEvent won = new UnityEvent();
        public UnityEvent lost = new UnityEvent();
        public void Win() { if (HasEnded) return; HasEnded = true; HasWon = true; won.Invoke(); }
        public void Lose() { if (HasEnded) return; HasEnded = true; lost.Invoke(); }
    }
}
