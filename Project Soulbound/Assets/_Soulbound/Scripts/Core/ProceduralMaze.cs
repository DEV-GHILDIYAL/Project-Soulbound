using System.Collections.Generic;
using UnityEngine;

namespace Soulbound
{
    public sealed class ProceduralMaze : MonoBehaviour
    {
        [SerializeField, Range(9, 31)] private int width = 15;
        [SerializeField, Range(9, 31)] private int height = 15;
        [SerializeField, Min(3f)] private float cellSize = 4f;
        [SerializeField] private bool randomizeSeed = true;
        [SerializeField] private int seed = 12345;
        [SerializeField] private Material floorMaterial, wallMaterial, chestMaterial, exitMaterial;
        [SerializeField] private bool dungeonLook = true;
        [SerializeField, Range(0f, 1f)] private float propDensity = 0.8f;
        private DungeonDressing dressing;
        public int Seed { get; private set; }
        public MazeLayout Layout { get; private set; }
        public Vector3 StartPosition => CellPosition(0);
        public Vector3 EnemyPosition { get; private set; }
        public Vector3[] Patrol { get; private set; }
        public List<Vector3> RoomApproaches { get; } = new List<Vector3>();
        private static int? replaySeed;
        public void ReplayNextRun() => replaySeed = Seed;
        public void Generate(Transform player)
        {
            Seed = replaySeed ?? (randomizeSeed ? System.Guid.NewGuid().GetHashCode() : seed);
            replaySeed = null;
            Layout = new MazeLayout(width, height, Seed, true);
            var environment = new GameObject("Generated Maze - Seed " + Seed).transform;
            environment.SetParent(transform, false);
            if (dungeonLook && wallMaterial != null)
            {
                dressing = gameObject.AddComponent<DungeonDressing>();
                dressing.Begin(environment, Seed, cellSize, wallMaterial);
            }
            Block(environment, "Floor", new Vector3(0, -0.25f, 0), new Vector3(width * cellSize + 0.25f, 0.5f, height * cellSize + 0.25f), dressing != null ? dressing.Mortar : floorMaterial);
            var locks = new List<KeyRoom>();
            for (int cell = 0; cell < Layout.Count; cell++)
            {
                if (dressing != null) dressing.Section(HorrorDungeon.SectionFor(Layout,cell));
                Vector3 center = CellPosition(cell);
                if (!Layout.IsOpen(cell, 0)) Wall(environment, center + Vector3.forward * cellSize / 2, true);
                if (!Layout.IsOpen(cell, 1)) Wall(environment, center + Vector3.right * cellSize / 2, false);
                if (cell / width == 0) Wall(environment, center - Vector3.forward * cellSize / 2, true);
                if (cell % width == 0) Wall(environment, center - Vector3.right * cellSize / 2, false);
                if (dressing != null) dressing.Cell(Layout, cell, center);
            }
            int roomIndex = 0;
            if (dressing != null) dressing.Corners(Layout);
            foreach (MazeLayout.Room room in Layout.Rooms)
            {
                int key = room.ChestCell;
                if (dressing != null) dressing.Chest(CellPosition(key));
                else Marker(environment, "Puzzle Chest Location (not interactive yet)", CellPosition(key), new Vector3(0.9f, 0.65f, 0.65f), chestMaterial);
                Vector3 normal = Quaternion.Euler(0, room.Direction * 90f, 0) * Vector3.forward;
                Vector3 entrance = (CellPosition(room.DoorCell) + CellPosition(room.InsideCell)) * 0.5f;
                if (dressing != null) dressing.RoomFrame(entrance, room.Direction);
                var gate = Block(environment, "Sealed " + roomIndex + " Key Room", entrance + Vector3.up * 1.45f,
                    new Vector3(cellSize - 0.25f, 2.9f, 0.24f), wallMaterial);
                gate.layer = 1; gate.transform.localRotation = Quaternion.Euler(0, room.Direction * 90f, 0);
                gate.AddComponent<ChamberGateVisual>().Build(wallMaterial, roomIndex);
                var reward = new GameObject("Room Chest Key " + (roomIndex + 1));
                reward.transform.SetParent(environment, false); reward.transform.localPosition = CellPosition(key);
                Vector3 panel = entrance - normal * 1.2f;
                RoomApproaches.Add(CellPosition(room.DoorCell));
                var lockController = gate.AddComponent<KeyRoom>();
                locks.Add(lockController);
                lockController.Configure(player, FindFirstObjectByType<RunState>(), gate.transform, reward.transform, panel,
                    unchecked(Seed + roomIndex * 7919), roomIndex);
                roomIndex++;
            }
            HorrorDungeon horror = null;
            if (wallMaterial != null) { horror = gameObject.AddComponent<HorrorDungeon>(); horror.Build(this,player,environment,wallMaterial,locks.ToArray()); }
            if (dressing != null) for (int cell = 0; cell < Layout.Count; cell++)
            {
                if (horror != null && horror.ReserveDressing(cell)) continue;
                dressing.Section(HorrorDungeon.SectionFor(Layout,cell));
                dressing.Props(Layout,cell,CellPosition(cell),propDensity);
            }
            if (dressing != null)
            {
                dressing.Exit(CellPosition(Layout.Exit), false);
                dressing.Finish(player.GetComponentInChildren<Camera>());
            }
            var exit = Block(environment, "Three Key Exit Seal", CellPosition(Layout.Exit) + Vector3.up * 1.2f,
                new Vector3(1.25f, 2.05f, 0.1f), exitMaterial);
            exit.layer = 1;
            exit.AddComponent<ExitDoor>().Configure(player, FindFirstObjectByType<RunState>());
            var routes = new List<Vector3>();
            int enemyCell = 1;
            for (int cell = 1; cell < Layout.Count; cell++)
            {
                if (cell == Layout.Exit || System.Array.IndexOf(Layout.Keys, cell) >= 0) continue;
                bool inRoom = false;
                foreach (var room in Layout.Rooms) if (System.Array.IndexOf(room.Cells, cell) >= 0) inRoom = true;
                if (inRoom) continue;
                if (Layout.Distances[cell] > Layout.Distances[enemyCell]) enemyCell = cell;
                if (Layout.Distances[cell] >= 3) routes.Add(CellPosition(cell));
            }
            EnemyPosition = CellPosition(enemyCell);
            Patrol = routes.ToArray();
            var supplies = new List<Vector3>(routes);
            var supplyRandom = new System.Random(unchecked(Seed ^ 0x36A41));
            for (int i = supplies.Count - 1; i > 0; i--)
            { int j = supplyRandom.Next(i + 1); Vector3 temp = supplies[i]; supplies[i] = supplies[j]; supplies[j] = temp; }
            for (int i = 0; i < Mathf.Min(8, supplies.Count); i++)
            {
                bool healing = i >= 6;
                var pickup = Block(environment, healing ? "Healing Supply" : "Ammo Supply", supplies[i] + Vector3.up * 0.35f,
                    healing ? new Vector3(0.35f, 0.45f, 0.35f) : new Vector3(0.45f, 0.22f, 0.3f), healing ? exitMaterial : chestMaterial);
                pickup.GetComponent<Collider>().enabled = false;
                pickup.AddComponent<SupplyPickup>().Configure(player, FindFirstObjectByType<RunState>(), healing);
            }
            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.position = StartPosition + Vector3.up * 0.05f;
            for (int d = 0; d < 4; d++) if (Layout.IsOpen(0, d))
            { player.rotation = Quaternion.Euler(0, d * 90f, 0); break; }
            if (controller != null) controller.enabled = true;
            Physics.SyncTransforms();
            Debug.Log($"Maze seed {Seed}: {width}x{height}; five horror sections, varied halls, three multi-step key journeys.", this);
        }
        public Vector3 CellPosition(int cell) => new Vector3(
            (cell % width - (width - 1) * 0.5f) * cellSize, 0,
            (cell / width - (height - 1) * 0.5f) * cellSize);
        public float CellSize => cellSize;
        private void Wall(Transform parent, Vector3 position, bool horizontal)
        {
            position.y = 1.5f;
            Block(parent, "Wall", position, horizontal ? new Vector3(cellSize, 3, 0.25f) : new Vector3(0.25f, 3, cellSize), dressing != null ? dressing.Mortar : wallMaterial);
            if (dressing != null) { position.y = 0; dressing.Wall(position, horizontal); }
        }
        private void Marker(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            position.y = size.y / 2;
            var marker = Block(parent, name, position, size, material);
            // Reserved locations are visual only; they cannot lock required routes.
            marker.GetComponent<Collider>().enabled = false;
        }
        private GameObject Block(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name; block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }
    }
}
