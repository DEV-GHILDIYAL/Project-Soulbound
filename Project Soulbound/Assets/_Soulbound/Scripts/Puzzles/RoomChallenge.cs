using System;
namespace Soulbound
{
    public sealed class RoomChallenge
    {
        public static readonly string[] Symbols = { "Sun", "Moon", "Eye", "Serpent", "Crown", "Star" };
        public int Kind { get; }
        public int[] Target { get; }
        public int[] State { get; private set; }
        public int[] RuneOrder { get; }
        public string[] Clues { get; }
        private readonly int[] initial;
        public RoomChallenge(int seed, int kind)
        {
            Kind = kind; var random = new Random(seed);
            if (kind == 0)
            {
                RuneOrder = new[] { 0, 1, 2, 3, 4 };
                for (int i = 4; i > 0; i--) { int j = random.Next(i + 1); int temp = RuneOrder[i]; RuneOrder[i] = RuneOrder[j]; RuneOrder[j] = temp; }
                string a = Symbols[RuneOrder[0]], b = Symbols[RuneOrder[1]], c = Symbols[RuneOrder[2]], d = Symbols[RuneOrder[3]], e = Symbols[RuneOrder[4]];
                Clues = new[] { $"{d} immediately follows {c}.", $"{a} comes before {b}.", $"{e} comes after {d}.", $"{b} stands exactly two positions before {d}." };
                Target = (int[])RuneOrder.Clone(); State = new int[0]; initial = new int[0];
            }
            else
            {
                Target = new int[kind == 1 ? 6 : 3];
                for (int i = 0; i < Target.Length; i++) Target[i] = random.Next(kind == 1 ? 2 : 6);
                State = (int[])Target.Clone();
                for (int i = 0; i < 15; i++) Move(random.Next(State.Length));
                if (IsSolved) Move(0);
                initial = (int[])State.Clone();
                Clues = kind == 1 ? new[] { "Each switch toggles itself and both neighbours.", "Match the target lights; the row wraps around." } : new[] {
                    "Symbol cycle: Sun > Moon > Eye > Serpent > Crown > Star > Sun",
                    $"Outer ring: one step AFTER {Symbols[(Target[0] + 5) % 6]}.",
                    $"Middle ring: two steps BEFORE {Symbols[(Target[1] + 2) % 6]}.",
                    $"Inner ring: opposite {Symbols[(Target[2] + 3) % 6]}.",
                    "Turning a ring also advances the next ring. Inner wraps to outer." };
            }
        }
        public bool IsSolved
        {
            get { if (State.Length != Target.Length) return false; for (int i = 0; i < State.Length; i++) if (State[i] != Target[i]) return false; return true; }
        }
        public void Move(int index)
        {
            if (Kind == 1)
            { State[index] ^= 1; State[(index + 5) % 6] ^= 1; State[(index + 1) % 6] ^= 1; }
            else if (Kind == 2)
            { State[index] = (State[index] + 1) % 6; int next = (index + 1) % 3; State[next] = (State[next] + 1) % 6; }
        }
        public void EnterRune(int index)
        {
            if (Kind != 0 || State.Length >= 5) return;
            foreach (int rune in State) if (rune == index) return;
            var entered = new int[State.Length + 1]; Array.Copy(State, entered, State.Length); entered[entered.Length - 1] = index; State = entered;
        }
        public void Reset() => State = (int[])initial.Clone();
    }
}
