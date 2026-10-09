using System;
using System.Collections.Generic;
using Soulbound;
public static class RoomAndPuzzleChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Main()
    {
        for (int key = 0; key < 3; key++)
        {
            string[][] required = {
                new[] { "guard-note", "epitaph", "crest", "statue" },
                new[] { "maintenance", "valve", "crank", "valve-fit", "crank-fit", "pressure" },
                new[] { "astral-0", "astral-1", "astral-2", "medallion", "medallion-fit" }
            };
            foreach (string missing in required[key])
            {
                var state = new HorrorQuestState();
                foreach (string id in required[key]) if (id != missing) state.Add(id);
                Check(!state.Ready(key), "Key unlocks with a missing exploration task");
                state.Add(missing); Check(state.Ready(key), "Complete key journey remains locked");
                Check(!state.Add(missing), "Quest reward can be collected twice");
            }
        }
        Permute(new[] { 0, 1, 2 }, 0, order => {
            var state = new HorrorQuestState();
            Check(!state.TurnPressure(order[0], order) && state.PressureStep == 0, "Pressure accepts missing parts");
            state.Add("valve-fit"); state.Add("crank-fit");
            state.TurnPressure(order[0], order);
            state.TurnPressure(order[0], order); Check(state.PressureStep == 0, "Incorrect pressure sequence does not reset");
            state.TurnPressure(order[0], order); state.TurnPressure(order[1], order);
            Check(state.TurnPressure(order[2], order) && state.Has("pressure"), "Correct pressure sequence rejected");
            Check(!state.TurnPressure(order[0], order), "Completed boiler restarts sequence");
        });
        int layouts = 0, puzzles = 0;
        foreach (int size in new[] { 9, 15, 21 }) for (int seed = 0; seed < 300; seed++)
        {
            var maze = new MazeLayout(size, size, seed, true);
            var replay = new MazeLayout(size, size, seed, true);
            var reserved = new HashSet<int>();
            foreach (var room in maze.Rooms) foreach (int cell in room.Cells) Check(reserved.Add(cell), "Overlapping rooms");
            Check(reserved.Count == (size >= 15 ? 16 : 12) && !reserved.Contains(0) && !reserved.Contains(maze.Exit), "Invalid room cells");
            var visited = new HashSet<int> { 0 }; var queue = new Queue<int>(); queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                for (int d = 0; d < 4; d++) if (maze.IsOpen(cell, d))
                {
                    int next = maze.Neighbor(cell, d); Check(next >= 0, "Broken outer boundary");
                    if (!reserved.Contains(next) && visited.Add(next)) queue.Enqueue(next);
                }
            }
            Check(visited.Count == maze.Count - reserved.Count, "Locked rooms disconnect main corridors");
            for (int r = 0; r < 3; r++)
            {
                var room = maze.Rooms[r]; int entrances = 0;
                foreach (int cell in room.Cells) for (int d = 0; d < 4; d++)
                    if (maze.IsOpen(cell, d) && !reserved.Contains(maze.Neighbor(cell, d))) entrances++;
                Check(entrances == 1 && visited.Contains(room.DoorCell), "Room needs exactly one reachable external entrance");
                Check(Array.IndexOf(room.Cells, room.ChestCell) >= 0 && maze.Keys[r] == room.ChestCell, "Chest outside room");
                Check(room.DoorCell == replay.Rooms[r].DoorCell && room.ChestCell == replay.Rooms[r].ChestCell, "Room replay differs");
            }
            foreach (int distance in maze.Distances) Check(distance >= 0, "Disconnected unlocked maze");
            layouts++;
        }
        for (int seed = 0; seed < 100; seed++) for (int kind = 0; kind < 3; kind++)
        {
            var puzzle = new RoomChallenge(seed, kind);
            Check(!puzzle.IsSolved, "Puzzle starts solved");
            if (kind == 0)
            {
                int valid = 0;
                Permute(new int[] { 0, 1, 2, 3, 4 }, 0, order => {
                    int[] p = puzzle.RuneOrder;
                    int a = Array.IndexOf(order,p[0]), b = Array.IndexOf(order,p[1]), c = Array.IndexOf(order,p[2]), d = Array.IndexOf(order,p[3]), e = Array.IndexOf(order,p[4]);
                    if (d == c + 1 && a < b && e > d && b == d - 2) valid++;
                });
                Check(valid == 1, "Rune clues have ambiguous solution");
                foreach (int symbol in puzzle.RuneOrder) puzzle.EnterRune(symbol);
                Check(puzzle.IsSolved, "Correct rune order rejected");
            }
            else
            {
                var queue = new Queue<int[]>(); var seen = new HashSet<string>();
                queue.Enqueue((int[])puzzle.State.Clone()); bool solved = false;
                while (queue.Count > 0)
                {
                    int[] state = queue.Dequeue(); string key = string.Join(",", state);
                    if (!seen.Add(key)) continue;
                    bool match = true; for (int i = 0; i < state.Length; i++) if (state[i] != puzzle.Target[i]) match = false;
                    if (match) { solved = true; break; }
                    for (int i = 0; i < state.Length; i++)
                    {
                        int[] next = (int[])state.Clone();
                        if (kind == 1) { next[i] ^= 1; next[(i+5)%6] ^= 1; next[(i+1)%6] ^= 1; }
                        else { next[i] = (next[i]+1)%6; next[(i+1)%3] = (next[(i+1)%3]+1)%6; }
                        queue.Enqueue(next);
                    }
                }
                Check(solved, "Scrambled puzzle cannot be solved");
            }
            puzzles++;
        }
        Console.WriteLine($"PASS: {layouts} room layouts (locked-door reachability, single entrances, interior chests, deterministic replay) and {puzzles} puzzles (unique rune solution, reachable torch/ring targets).");
    }
    static void Permute(int[] values, int index, Action<int[]> visit)
    {
        if (index == values.Length) { visit(values); return; }
        for (int i = index; i < values.Length; i++)
        { int temp = values[index]; values[index] = values[i]; values[i] = temp; Permute(values,index+1,visit); temp = values[index]; values[index] = values[i]; values[i] = temp; }
    }
}
