using UnityEngine;
using UnityEngine.Events;
namespace Soulbound
{
    // Concrete puzzle controllers call Solve only after their solution is validated.
    public sealed class PuzzleLock : MonoBehaviour
    {
        public bool IsSolved { get; private set; }
        public UnityEvent solved = new UnityEvent();
        public void Solve() { if (IsSolved) return; IsSolved = true; solved.Invoke(); }
    }
}
