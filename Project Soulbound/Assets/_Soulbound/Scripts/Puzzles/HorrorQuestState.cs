using System.Collections.Generic;
namespace Soulbound
{
    // Run-local quest inventory; world interactions are gated by explicit dependencies.
    public sealed class HorrorQuestState
    {
        private readonly HashSet<string> found = new HashSet<string>();
        public bool Has(string id) => found.Contains(id);
        public bool Add(string id) => found.Add(id);
        public bool HasAll(params string[] ids)
        { foreach (string id in ids) if (!Has(id)) return false; return true; }
        public bool Ready(int key)
        {
            if (key == 0) return HasAll("guard-note", "epitaph", "crest", "statue");
            if (key == 1) return HasAll("maintenance", "valve", "crank", "valve-fit", "crank-fit", "pressure");
            return HasAll("astral-0", "astral-1", "astral-2", "medallion", "medallion-fit");
        }
        public int PressureStep { get; private set; }
        public bool TurnPressure(int handle, int[] order)
        {
            if (!HasAll("valve-fit", "crank-fit") || Has("pressure")) return false;
            PressureStep = handle == order[PressureStep] ? PressureStep + 1 : 0;
            if (PressureStep == order.Length) { Add("pressure"); PressureStep = 0; return true; }
            return false;
        }
    }
}
