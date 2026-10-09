using System;
using System.Collections.Generic;
using Soulbound;
public static class MazeLayoutChecks
{
    static void Require(bool ok, string error) { if (!ok) throw new Exception(error); }
    public static void Main()
    {
        int checkedCount = 0;
        var fingerprints = new HashSet<string>();
        foreach (int size in new[] { 3, 9, 15, 31 })
        for (int seed = 0; seed < 250; seed++)
        {
            var maze = new MazeLayout(size, size, seed);
            var replay = new MazeLayout(size, size, seed);
            var visited = new HashSet<int>();
            var queue = new Queue<int>(); visited.Add(0); queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int neighbor = maze.Neighbor(cell, d);
                    Require(maze.IsOpen(cell, d) == replay.IsOpen(cell, d), "Seed replay differs");
                    if (!maze.IsOpen(cell, d)) continue;
                    Require(neighbor >= 0, "Open outer boundary");
                    Require(maze.IsOpen(neighbor, (d + 2) % 4), "One-way passage");
                    if (visited.Add(neighbor)) queue.Enqueue(neighbor);
                }
            }
            Require(visited.Count == maze.Count, "Disconnected cells");
            Require(maze.Exit != 0 && visited.Contains(maze.Exit), "Invalid exit");
            var keys = new HashSet<int>();
            for (int i = 0; i < 3; i++)
            {
                int key = maze.Keys[i];
                Require(key != 0 && key != maze.Exit && visited.Contains(key) && keys.Add(key), "Invalid key location");
                Require(key == replay.Keys[i], "Key replay differs");
            }
            Require(maze.Exit == replay.Exit, "Exit replay differs");
            var fingerprint = new System.Text.StringBuilder();
            for (int cell = 0; cell < maze.Count; cell++) for (int d = 0; d < 4; d++) fingerprint.Append(maze.IsOpen(cell, d) ? '1' : '0');
            fingerprints.Add(fingerprint.ToString());
            checkedCount++;
        }
        foreach (var shape in new[] { (3, 31), (31, 3), (7, 11) })
        {
            var maze = new MazeLayout(shape.Item1, shape.Item2, int.MinValue);
            foreach (int distance in maze.Distances) Require(distance >= 0, "Rectangular layout disconnected");
            checkedCount++;
        }
        Require(fingerprints.Count > 750, "Insufficient layout variation");
        Console.WriteLine($"PASS: {checkedCount} layouts; connected paths, reciprocal walls, outer boundaries, reachable distinct objectives, same-seed replay. {fingerprints.Count} unique square layouts.");
    }
}
