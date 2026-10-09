using UnityEngine;
using UnityEngine.Events;
namespace Soulbound
{
    public sealed class KeyChest : MonoBehaviour
    {
        [SerializeField] private PuzzleLock puzzle;
        public bool IsOpen { get; private set; }
        public UnityEvent opened = new UnityEvent();
        public bool TryOpen(PlayerInventory inventory)
        {
            if (IsOpen || puzzle == null || !puzzle.IsSolved || inventory == null) return false;
            IsOpen = true; inventory.AddKey(); opened.Invoke(); return true;
        }
    }
}
