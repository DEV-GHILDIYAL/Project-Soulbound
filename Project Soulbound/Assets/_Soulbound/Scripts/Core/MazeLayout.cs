using System;
using System.Collections.Generic;

namespace Soulbound
{
    // Engine-independent layout, also used by seeded connectivity checks.
    public sealed class MazeLayout
    {
        public int Width { get; }
        public int Height { get; }
        public int Count => Width * Height;
        public int Exit { get; }
        public int[] Keys { get; }
        public int[] Distances { get; }
        public sealed class Room
        {
            public int[] Cells;
            public int DoorCell, InsideCell, Direction, ChestCell;
        }
        public Room[] Rooms { get; }
        private readonly byte[] openings;
        public MazeLayout(int width, int height, int seed, bool keyRooms = false)
        {
            if (width < 3 || height < 3 || width > 31 || height > 31)
                throw new ArgumentOutOfRangeException("Maze dimensions must be between 3 and 31.");
            Width = width; Height = height;
            openings = new byte[Count];
            var random = new Random(seed);
            var reserved = new bool[Count];
            Rooms = keyRooms ? ReserveRooms(random, reserved) : new Room[0];
            var visited = new bool[Count];
            var stack = new Stack<int>();
            visited[0] = true; stack.Push(0);
            var choices = new List<int>(4);
            while (stack.Count > 0)
            {
                int cell = stack.Peek(); choices.Clear();
                for (int d = 0; d < 4; d++)
                { int next = Neighbor(cell, d); if (next >= 0 && !visited[next] && !reserved[next]) choices.Add(d); }
                if (choices.Count == 0) { stack.Pop(); continue; }
                int direction = choices[random.Next(choices.Count)];
                int neighbor = Neighbor(cell, direction);
                Connect(cell, direction, neighbor);
                visited[neighbor] = true; stack.Push(neighbor);
            }
            // A few loops provide escape routes without sacrificing connectedness.
            for (int cell = 0; cell < Count; cell++)
                for (int d = 0; d < 2; d++)
                { int neighbor = Neighbor(cell, d); if (neighbor >= 0 && !reserved[cell] && !reserved[neighbor] && random.NextDouble() < 0.08) Connect(cell, d, neighbor); }
            // Unequal public halls remain outside sealed key rooms. Their interior walls
            // are removed before distances are measured, preserving seeded routes.
            if (keyRooms) for (int hall = 0; hall < 5; hall++)
            {
                int sx = 2 + hall % 3, sz = 2 + hall % 2;
                var sites = new List<int>();
                for (int z = 1; z < Height - sz; z++) for (int x = 1; x < Width - sx; x++)
                {
                    bool clear = true;
                    for (int dz = 0; dz < sz; dz++) for (int dx = 0; dx < sx; dx++)
                        if (reserved[(z + dz) * Width + x + dx]) clear = false;
                    if (clear) sites.Add(z * Width + x);
                }
                if (sites.Count == 0) continue;
                int origin = sites[random.Next(sites.Count)];
                for (int dz = 0; dz < sz; dz++) for (int dx = 0; dx < sx; dx++)
                {
                    int cell = origin + dz * Width + dx;
                    if (dz + 1 < sz) Connect(cell, 0, cell + Width);
                    if (dx + 1 < sx) Connect(cell, 1, cell + 1);
                }
            }
            foreach (Room room in Rooms)
            {
                foreach (int cell in room.Cells) for (int d = 0; d < 2; d++)
                {
                    int neighbor = Neighbor(cell, d);
                    if (Array.IndexOf(room.Cells, neighbor) >= 0) Connect(cell, d, neighbor);
                }
                var entries = new List<(int cell, int direction, int outside)>();
                foreach (int cell in room.Cells) for (int d = 0; d < 4; d++)
                {
                    int outside = Neighbor(cell, d);
                    if (outside >= 0 && !reserved[outside]) entries.Add((cell, d, outside));
                }
                var entry = entries[random.Next(entries.Count)];
                room.InsideCell = entry.cell; room.DoorCell = entry.outside;
                room.Direction = (entry.direction + 2) % 4;
                Connect(entry.cell, entry.direction, entry.outside);
                room.ChestCell = room.Cells[0];
                foreach (int cell in room.Cells)
                    if (CellDistance(cell, entry.cell) > CellDistance(room.ChestCell, entry.cell)) room.ChestCell = cell;
            }
            Distances = new int[Count];
            for (int i = 0; i < Count; i++) Distances[i] = -1;
            var queue = new Queue<int>(); queue.Enqueue(0); Distances[0] = 0;
            int exit = 0;
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                if (!reserved[cell] && Distances[cell] > Distances[exit]) exit = cell;
                for (int d = 0; d < 4; d++)
                {
                    int next = Neighbor(cell, d);
                    if (IsOpen(cell, d) && next >= 0 && Distances[next] < 0)
                    { Distances[next] = Distances[cell] + 1; queue.Enqueue(next); }
                }
            }
            Exit = exit;
            Keys = new int[3];
            if (keyRooms)
            { for (int k = 0; k < 3; k++) Keys[k] = Rooms[k].ChestCell; return; }
            var candidates = new List<int>();
            for (int i = 1; i < Count; i++) if (i != Exit) candidates.Add(i);
            // Spread objectives through shallow, middle, and deep reachable parts.
            candidates.Sort((a, b) => Distances[a] != Distances[b] ? Distances[a].CompareTo(Distances[b]) : a.CompareTo(b));
            for (int k = 0; k < 3; k++)
            {
                int start = k * candidates.Count / 3;
                int end = (k + 1) * candidates.Count / 3;
                Keys[k] = candidates[random.Next(start, end)];
            }
        }
        private int CellDistance(int a, int b) => Math.Abs(a % Width - b % Width) + Math.Abs(a / Width - b / Width);
        private Room[] ReserveRooms(Random random, bool[] reserved)
        {
            if (Width < 9 || Height < 9) throw new ArgumentException("Key rooms require at least a 9x9 maze.");
            var rooms = new List<Room>();
            for (int r = 0; r < 3; r++)
            {
                int roomWidth = Width >= 15 && r == 1 ? 3 : 2;
                int roomHeight = Height >= 15 && r == 2 ? 3 : 2;
                var choices = new List<int>();
                for (int z = 1; z < Height - roomHeight; z++) for (int x = 1; x < Width - roomWidth; x++)
                {
                    if (x + z < (Width + Height) / 3) continue;
                    bool clear = true;
                    for (int dz = -1; dz <= roomHeight; dz++) for (int dx = -1; dx <= roomWidth; dx++)
                        if (reserved[(z + dz) * Width + x + dx]) clear = false;
                    if (clear) choices.Add(z * Width + x);
                }
                if (choices.Count == 0) throw new InvalidOperationException("Cannot reserve separated key rooms.");
                // Prefer rooms far apart rather than an obvious cluster.
                int best = choices[random.Next(choices.Count)];
                int bestScore = -1;
                foreach (int candidate in choices)
                {
                    int score = CellDistance(candidate, 0);
                    foreach (Room room in rooms) score = Math.Min(score, CellDistance(candidate, room.Cells[0]) * 2);
                    score = score * 10 + random.Next(10);
                    if (score > bestScore) { best = candidate; bestScore = score; }
                }
                var cells = new List<int>();
                for (int dz = 0; dz < roomHeight; dz++) for (int dx = 0; dx < roomWidth; dx++) cells.Add(best + dz * Width + dx);
                var created = new Room { Cells = cells.ToArray() };
                foreach (int cell in created.Cells) reserved[cell] = true;
                rooms.Add(created);
            }
            return rooms.ToArray();
        }
        private void Connect(int cell, int direction, int next)
        { openings[cell] |= (byte)(1 << direction); openings[next] |= (byte)(1 << ((direction + 2) % 4)); }
        public bool IsOpen(int cell, int direction) => (openings[cell] & (1 << direction)) != 0;
        public int Neighbor(int cell, int direction)
        {
            int x = cell % Width, z = cell / Width;
            switch (direction)
            {
                case 0: return z + 1 < Height ? cell + Width : -1;
                case 1: return x + 1 < Width ? cell + 1 : -1;
                case 2: return z > 0 ? cell - Width : -1;
                case 3: return x > 0 ? cell - 1 : -1;
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }
}
