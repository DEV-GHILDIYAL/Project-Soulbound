using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Soulbound
{
    // Dungeon appearance and wall-side props; structural topology belongs to ProceduralMaze.
    public sealed class DungeonDressing : MonoBehaviour
    {
        private readonly Dictionary<(Material material, int x, int z), List<CombineInstance>> batches = new Dictionary<(Material, int, int), List<CombineInstance>>();
        private readonly List<Object> owned = new List<Object>();
        private readonly List<Light> torches = new List<Light>();
        private Material stone, darkStone, mortar, wood, iron, brass, flame, rune, bone, cloth;
        private Mesh cube, cylinder, sphere;
        private Transform geometry;
        private System.Random random;
        private float cellSize;
        private System.Random propsRandom;
        private Material[] sectionStone, sectionDark;
        private Material[] wallStone, wallDamp;
        private int currentSection;
        public void Section(int index)
        { currentSection = index; stone = sectionStone[index]; darkStone = sectionDark[index]; }
        public int PropClusters { get; private set; }

        public void Begin(Transform parent, int seed, float size, Material template)
        {
            cellSize = size; random = new System.Random(seed);
            propsRandom = new System.Random(unchecked(seed ^ 0x5A17B3));
            geometry = new GameObject("Dungeon Details").transform;
            geometry.SetParent(parent, false);
            var temporary = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube = temporary.GetComponent<MeshFilter>().sharedMesh;
            temporary.SetActive(false); Destroy(temporary);
            cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
            sphere = PrimitiveMesh(PrimitiveType.Sphere);
            stone = Material(template, "Weathered Limestone", new Color(0.40f, 0.37f, 0.30f));
            darkStone = Material(template, "Damp Stone", new Color(0.25f, 0.29f, 0.27f));
            mortar = Material(template, "Dark Mortar", new Color(0.11f, 0.13f, 0.14f));
            wood = Material(template, "Chest Timber", new Color(0.28f, 0.12f, 0.045f));
            iron = Material(template, "Forged Iron", new Color(0.12f, 0.14f, 0.16f), 0.65f);
            brass = Material(template, "Aged Brass", new Color(0.55f, 0.34f, 0.10f), 0.65f);
            bone = Material(template, "Old Bones", new Color(0.61f, 0.56f, 0.40f));
            cloth = Material(template, "Tattered Banner", new Color(0.27f, 0.075f, 0.07f));
            flame = Material(template, "Torch Ember", new Color(1f, 0.42f, 0.06f));
            flame.EnableKeyword("_EMISSION"); flame.SetColor("_EmissionColor", new Color(3f, 1.1f, 0.12f));
            rune = Material(template, "Exit Seal", new Color(0.06f, 0.35f, 0.31f));
            rune.EnableKeyword("_EMISSION"); rune.SetColor("_EmissionColor", new Color(0.1f, 1.5f, 1.1f));
            Texture2D texture = StoneTexture(seed);
            stone.SetTexture("_BaseMap", texture); darkStone.SetTexture("_BaseMap", texture);
            var colors = new[] { new Color(.32f,.29f,.26f), new Color(.22f,.35f,.33f), new Color(.37f,.24f,.18f), new Color(.43f,.39f,.31f), new Color(.27f,.29f,.43f) };
            sectionStone = new Material[5]; sectionDark = new Material[5];
            for (int i = 0; i < 5; i++)
            {
                sectionStone[i] = Material(template,"Section stone " + i,colors[i]);
                sectionDark[i] = Material(template,"Section damp stone " + i,colors[i] * .65f);
                sectionStone[i].SetTexture("_BaseMap",texture); sectionDark[i].SetTexture("_BaseMap",texture);
            }
            BuildWallMaterials(seed);
            Section(0);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.12f,.145f,.16f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.045f,.06f,.07f); RenderSettings.fogDensity = .038f;
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) light.intensity = .07f;

            // A local profile overrides the template grade without editing shared assets.
            var volume = geometry.gameObject.AddComponent<Volume>();
            volume.isGlobal = true; volume.priority = 50;volume.weight=1;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Soulbound Global Horror Grade"; owned.Add(profile);
            volume.sharedProfile = profile;
            var bloom = profile.Add<Bloom>(true); owned.Add(bloom);
            bloom.threshold.Override(1.1f); bloom.intensity.Override(.18f);
            var vignette = profile.Add<Vignette>(true); owned.Add(vignette);
            vignette.intensity.Override(.22f); vignette.smoothness.Override(.72f);
            var grading = profile.Add<ColorAdjustments>(true); owned.Add(grading);
            grading.postExposure.Override(.3f); grading.contrast.Override(8f); grading.saturation.Override(-18f);
            grading.colorFilter.Override(new Color(.90f,.96f,1f));
            var tone=profile.Add<Tonemapping>(true);owned.Add(tone);tone.mode.Override(TonemappingMode.ACES);
            var grain=profile.Add<FilmGrain>(true);owned.Add(grain);grain.type.Override(FilmGrainLookup.Thin1);grain.intensity.Override(.12f);grain.response.Override(.8f);
            var fringe=profile.Add<ChromaticAberration>(true);owned.Add(fringe);fringe.intensity.Override(.025f);
        }

        public Material Mortar => mortar;
        public void NarrowPassage(Vector3 center,Quaternion rotation,int section)
        {
            const float clearance=2f, roofBottom=2.3f, top=3.04f;
            float thickness=(cellSize-clearance)*.5f,length=cellSize-.04f;
            for(int side=-1;side<=1;side+=2)
            {
                Vector3 wall=center+rotation*new Vector3(side*(clearance*.5f+thickness*.5f),top*.5f,0);
                Vector3 size=new Vector3(thickness,top,length);
                Box(wallStone[section],wall,size,rotation);PropCollider("Narrow stone wall",wall,size,rotation);
                // Recessed masonry courses face the clear lane; no coplanar overlay.
                for(int row=0;row<4;row++)for(int column=0;column<4;column++)
                {
                    float blockLength=length/4;
                    Vector3 p=center+rotation*new Vector3(side*(clearance*.5f-.023f),.35f+row*.70f,-length*.5f+(column+.5f)*blockLength);
                    Box((row+column)%5==0?wallDamp[section]:wallStone[section],p,new Vector3(.035f,.665f,blockLength-.035f),rotation);
                }
            }
            // Solid soffit closes the entire space up to the existing roof,
            // including the end faces that used to expose a hollow upper gap.
            Vector3 cap=center+Vector3.up*((roofBottom+top)*.5f);
            Vector3 capSize=new Vector3(cellSize,top-roofBottom,length);
            Box(wallDamp[section],cap,capSize,rotation);PropCollider("Filled narrow ceiling",cap,capSize,rotation);
        }
        public void Wall(Vector3 position, bool horizontal)
        {
            Quaternion rotation = horizontal ? Quaternion.identity : Quaternion.Euler(0, 90, 0);
            // Staggered stone courses on both faces; shared meshes rather than thousands of objects.
            for (int side = -1; side <= 1; side += 2)
            for (int row = 0; row < 4; row++)
            {
                float length = cellSize - 0.02f;
                int count = row % 2 == 0 ? 4 : 5;
                float blockWidth = length / count;
                for (int column = 0; column < count; column++)
                {
                    Vector3 local = new Vector3(-length / 2 + (column + 0.5f) * blockWidth,
                        0.36f + row * 0.7f, side * 0.155f);
                    Box(random.Next(4) == 0 ? wallDamp[currentSection] : wallStone[currentSection], position + rotation * local,
                        new Vector3(blockWidth - 0.035f, 0.665f, 0.055f), rotation);
                }
            }
            Box(wallStone[currentSection], position + Vector3.up * 2.94f,
                horizontal ? new Vector3(cellSize - 0.36f, 0.13f, 0.36f) : new Vector3(0.36f, 0.13f, cellSize - 0.36f));
        }

        public void Cell(MazeLayout layout, int cell, Vector3 center)
        {
            const int tiles = 4;
            float tileSize = cellSize / tiles;
            for (int z = 0; z < tiles; z++) for (int x = 0; x < tiles; x++)
                Box(random.Next(5) == 0 ? darkStone : stone, center + new Vector3(
                    -cellSize / 2 + (x + 0.5f) * tileSize, -0.019f,
                    -cellSize / 2 + (z + 0.5f) * tileSize), new Vector3(tileSize - 0.025f, 0.05f, tileSize - 0.025f));
            Box(mortar, center + Vector3.up * 3.12f, new Vector3(cellSize, 0.18f, cellSize));
            // Recessed cross beams give the corridors a roof silhouette.
            Box(darkStone, center + Vector3.up * 2.94f, new Vector3(cellSize, 0.14f, 0.18f));
            Box(darkStone, center + Vector3.up * 2.86f, new Vector3(0.18f, 0.14f, cellSize));
            if (cell % 6 != 0 && System.Array.IndexOf(layout.Keys, cell) < 0 && cell != layout.Exit) return;
            for (int d = 0; d < 4; d++) if (!layout.IsOpen(cell, d))
            {
                Vector3 normal = Quaternion.Euler(0, d * 90f, 0) * Vector3.forward;
                Torch(center + normal * (cellSize / 2 - 0.36f), -normal);
                break;
            }
        }

        public void Chest(Vector3 center)
        {
            Box(wood, center + Vector3.up * 0.34f, new Vector3(1.05f, 0.62f, 0.70f));
            Box(wood, center + Vector3.up * 0.70f, new Vector3(1.10f, 0.14f, 0.75f));
            foreach (float x in new[] { -0.36f, 0.36f })
                Box(iron, center + new Vector3(x, 0.43f, 0), new Vector3(0.07f, 0.72f, 0.78f));
            Box(brass, center + new Vector3(0, 0.50f, -0.39f), new Vector3(0.18f, 0.20f, 0.06f));
            Box(rune, center + new Vector3(0, 0.52f, -0.43f), new Vector3(0.035f, 0.07f, 0.018f));
        }
        public void Corners(MazeLayout layout)
        {
            for (int z = 0; z <= layout.Height; z++) for (int x = 0; x <= layout.Width; x++)
            {
                bool edge = x == 0 || z == 0 || x == layout.Width || z == layout.Height;
                bool wall = edge;
                if (x > 0 && z > 0) wall |= !layout.IsOpen((z-1)*layout.Width+x-1,0) || !layout.IsOpen((z-1)*layout.Width+x-1,1);
                if (x < layout.Width && z > 0) wall |= !layout.IsOpen((z-1)*layout.Width+x,0);
                if (x > 0 && z < layout.Height) wall |= !layout.IsOpen(z*layout.Width+x-1,1);
                if (!wall) continue;
                Vector3 corner = new Vector3((x-layout.Width*.5f)*cellSize,0,(z-layout.Height*.5f)*cellSize);
                Box(darkStone,corner+Vector3.up*1.5f,new Vector3(.38f,3,.38f));
                Box(stone,corner+Vector3.up*.13f,new Vector3(.46f,.26f,.46f));
                Box(stone,corner+Vector3.up*2.84f,new Vector3(.46f,.26f,.46f));
                PropCollider("Wall corner pillar",corner+Vector3.up*1.5f,new Vector3(.38f,3,.38f),Quaternion.identity);
            }
        }

        public void Props(MazeLayout layout, int cell, Vector3 center, float density)
        {
            // Entire objective/approach cells remain clear, including both sides of each gate.
            if (cell == 0 || cell == layout.Exit || System.Array.IndexOf(layout.Keys, cell) >= 0) return;
            foreach (var room in layout.Rooms)
                if (cell == room.DoorCell || cell == room.InsideCell) return;
            var closed = new List<int>();
            for (int d = 0; d < 4; d++) if (!layout.IsOpen(cell, d)) closed.Add(d);
            if (closed.Count == 0 || propsRandom.NextDouble() > density) return;
            int direction = closed[propsRandom.Next(closed.Count)];
            Quaternion rotation = Quaternion.Euler(0, direction * 90f, 0);
            Vector3 wall = center + rotation * new Vector3(0, 0, cellSize / 2 - 0.50f);
            // Place small clusters against closed walls, never across open passages.
            switch (propsRandom.Next(12))
            {
                case 0: Barrel(wall, rotation); break;
                case 1: Crate(wall, rotation); break;
                case 2: Rubble(wall, rotation); break;
                case 3: Remains(wall, rotation); break;
                case 4: BrokenPillar(wall, rotation); break;
                case 5: WallRack(wall, rotation); break;
                case 6: Banner(wall, rotation); break;
                case 7: Urns(wall, rotation); break;
                case 8: Bench(wall, rotation); break;
                case 9: Shield(wall, rotation); break;
                case 10: Shrine(wall, rotation); break;
                default: Chains(wall, rotation); break;
            }
            PropClusters++;
        }

        private Mesh PrimitiveMesh(PrimitiveType type)
        {
            var temporary = GameObject.CreatePrimitive(type);
            Mesh result = temporary.GetComponent<MeshFilter>().sharedMesh;
            temporary.SetActive(false); Destroy(temporary); return result;
        }
        private void Barrel(Vector3 center, Quaternion rotation)
        {
            Shape(cylinder, wood, center + Vector3.up * 0.5f, new Vector3(0.64f, 0.5f, 0.64f), rotation);
            foreach (float y in new[] { 0.13f, 0.50f, 0.88f })
                Shape(cylinder, iron, center + Vector3.up * y, new Vector3(0.67f, 0.026f, 0.67f), rotation);
            Box(wood, center + Vector3.up * 1.015f, new Vector3(0.48f, 0.025f, 0.07f), rotation);
            PropCollider("Barrel", center + Vector3.up * 0.5f, new Vector3(0.68f, 1.04f, 0.68f), rotation);
        }
        private void Crate(Vector3 center, Quaternion rotation)
        {
            Box(wood, center + Vector3.up * 0.38f, new Vector3(0.75f, 0.75f, 0.64f), rotation);
            for (int i = -1; i <= 1; i++)
                Box(darkStone, center + rotation * new Vector3(i * 0.23f, 0.38f, -0.328f), new Vector3(0.012f, 0.71f, 0.012f), rotation);
            foreach (float y in new[] { 0.08f, 0.68f })
                Box(iron, center + Vector3.up * y, new Vector3(0.79f, 0.075f, 0.68f), rotation);
            Box(wood, center + rotation * new Vector3(0, 0.39f, -0.35f), new Vector3(0.09f, 0.87f, 0.05f), rotation * Quaternion.Euler(0, 0, 40));
            PropCollider("Crate", center + Vector3.up * 0.38f, new Vector3(0.79f, 0.76f, 0.70f), rotation);
        }
        private void Rubble(Vector3 center, Quaternion rotation)
        {
            for (int i = 0; i < 7; i++)
            {
                Vector3 offset = new Vector3((float)propsRandom.NextDouble() * 1.1f - 0.55f, 0.045f,
                    (float)propsRandom.NextDouble() * 0.38f - 0.19f);
                Box(i % 2 == 0 ? darkStone : stone, center + rotation * offset,
                    new Vector3(0.12f + (float)propsRandom.NextDouble() * 0.18f, 0.08f, 0.13f),
                    rotation * Quaternion.Euler(0, propsRandom.Next(180), propsRandom.Next(-10, 10)));
            }
        }
        private void Remains(Vector3 center, Quaternion rotation)
        {
            Shape(sphere, bone, center + rotation * new Vector3(-0.26f, 0.15f, 0), new Vector3(0.24f, 0.25f, 0.26f), rotation);
            // Dark sockets and jaw suggest a skull without a large imported model.
            for (int side = -1; side <= 1; side += 2)
                Shape(sphere, mortar, center + rotation * new Vector3(-0.26f + side * 0.055f, 0.19f, -0.11f), new Vector3(0.065f, 0.065f, 0.035f), rotation);
            Box(bone, center + rotation * new Vector3(-0.26f, 0.055f, -0.035f), new Vector3(0.15f, 0.055f, 0.14f), rotation);
            for (int i = 0; i < 3; i++)
                Shape(cylinder, bone, center + rotation * new Vector3(0.06f + i * 0.15f, 0.035f, 0),
                    new Vector3(0.04f, 0.17f, 0.04f), rotation * Quaternion.Euler(90, i * 25, 15));
        }
        private void BrokenPillar(Vector3 center, Quaternion rotation)
        {
            Box(stone, center + Vector3.up * 0.10f, new Vector3(0.72f, 0.20f, 0.68f), rotation);
            Shape(cylinder, darkStone, center + Vector3.up * 0.48f, new Vector3(0.48f, 0.30f, 0.48f), rotation);
            Box(stone, center + Vector3.up * 0.79f, new Vector3(0.48f, 0.10f, 0.45f), rotation * Quaternion.Euler(0, 0, 8));
            Rubble(center + rotation * new Vector3(0.1f, 0, -0.12f), rotation);
            PropCollider("Broken Pillar", center + Vector3.up * 0.42f, new Vector3(0.72f, 0.84f, 0.68f), rotation);
        }
        private void WallRack(Vector3 center, Quaternion rotation)
        {
            Vector3 anchor = center + rotation * new Vector3(0, 0, 0.28f);
            Box(wood, anchor + Vector3.up * 1.7f, new Vector3(0.72f, 0.10f, 0.08f), rotation);
            for (int i = -1; i <= 1; i++)
            {
                Box(iron, anchor + rotation * new Vector3(i * 0.21f, 1.4f, -0.02f), new Vector3(0.025f, 0.62f, 0.03f), rotation);
                Box(iron, anchor + rotation * new Vector3(i * 0.21f, 1.09f, -0.02f), new Vector3(0.13f, 0.04f, 0.04f), rotation);
            }
        }
        private void Banner(Vector3 center, Quaternion rotation)
        {
            Vector3 anchor = center + rotation * new Vector3(0, 0, 0.27f);
            Box(iron, anchor + Vector3.up * 2.42f, new Vector3(0.80f, 0.055f, 0.06f), rotation);
            Box(cloth, anchor + Vector3.up * 1.86f, new Vector3(0.62f, 1.05f, 0.025f), rotation);
            Box(brass, anchor + rotation * new Vector3(0, 1.92f, -0.03f), new Vector3(0.22f, 0.22f, 0.02f), rotation * Quaternion.Euler(0, 0, 45));
            for (int i = 0; i < 3; i++) Box(cloth, anchor + rotation * new Vector3((i - 1) * 0.21f, 1.28f - i * 0.04f, 0), new Vector3(0.17f, 0.22f, 0.025f), rotation);
        }
        private void PropCollider(string name, Vector3 center, Vector3 size, Quaternion rotation)
        {
            var prop = new GameObject(name + " Collider"); prop.transform.SetParent(geometry, false);
            prop.transform.localPosition = center; prop.transform.localRotation = rotation;
            prop.AddComponent<BoxCollider>().size = size;
        }
        private void Urns(Vector3 center, Quaternion rotation)
        {
            foreach (float x in new[] { -0.24f, 0.24f })
            {
                Vector3 p = center + rotation * new Vector3(x, 0, 0);
                Shape(sphere, stone, p + Vector3.up * 0.3f, new Vector3(0.34f, 0.45f, 0.34f), rotation);
                Shape(cylinder, darkStone, p + Vector3.up * 0.56f, new Vector3(0.16f, 0.08f, 0.16f), rotation);
                Shape(cylinder, brass, p + Vector3.up * 0.64f, new Vector3(0.24f, 0.025f, 0.24f), rotation);
            }
            PropCollider("Urns", center + Vector3.up * 0.34f, new Vector3(0.86f, 0.68f, 0.4f), rotation);
        }
        private void Bench(Vector3 center, Quaternion rotation)
        {
            Box(wood, center + Vector3.up * 0.45f, new Vector3(1.2f, 0.10f, 0.42f), rotation);
            foreach (float x in new[] { -0.45f, 0.45f })
                Box(iron, center + rotation * new Vector3(x, 0.22f, 0), new Vector3(0.09f, 0.44f, 0.30f), rotation);
            Box(cloth, center + rotation * new Vector3(0.1f, 0.515f, 0), new Vector3(0.40f, 0.035f, 0.37f), rotation);
            PropCollider("Bench", center + Vector3.up * 0.25f, new Vector3(1.2f, 0.50f, 0.42f), rotation);
        }
        private void Shield(Vector3 center, Quaternion rotation)
        {
            Vector3 anchor = center + rotation * new Vector3(0, 1.65f, 0.28f);
            Shape(cylinder, iron, anchor, new Vector3(0.6f, 0.04f, 0.6f), rotation * Quaternion.Euler(90, 0, 0));
            Shape(sphere, brass, anchor - rotation * Vector3.forward * 0.07f, Vector3.one * 0.19f, rotation);
            Box(wood, anchor + Vector3.down * 0.63f, new Vector3(0.04f, 0.72f, 0.04f), rotation);
        }
        private void Shrine(Vector3 center, Quaternion rotation)
        {
            Box(darkStone, center + Vector3.up * 0.34f, new Vector3(0.62f, 0.68f, 0.55f), rotation);
            Box(stone, center + Vector3.up * 0.72f, new Vector3(0.78f, 0.12f, 0.62f), rotation);
            foreach (float x in new[] { -0.23f, 0.23f })
            {
                Shape(cylinder, bone, center + rotation * new Vector3(x, 0.9f, 0), new Vector3(0.06f, 0.11f, 0.06f), rotation);
                Box(flame, center + rotation * new Vector3(x, 1.03f, 0), new Vector3(0.025f, 0.06f, 0.025f), rotation);
            }
            PropCollider("Candle Shrine", center + Vector3.up * 0.4f, new Vector3(0.78f, 0.8f, 0.62f), rotation);
        }
        private void Chains(Vector3 center, Quaternion rotation)
        {
            for (int column = -1; column <= 1; column += 2)
                for (int link = 0; link < 10; link++)
                    Box(iron, center + rotation * new Vector3(column * 0.21f, 2.5f - link * 0.12f, 0.25f),
                        new Vector3(0.045f, 0.09f, 0.025f), rotation * Quaternion.Euler(0, link % 2 == 0 ? 0 : 90, 0));
        }
        public void RoomFrame(Vector3 entrance, int direction)
        {
            Quaternion rotation = Quaternion.Euler(0, direction * 90f, 0);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(stone, entrance + rotation * new Vector3(side * (cellSize / 2 - 0.12f), 1.5f, 0),
                    new Vector3(0.24f, 3f, 0.38f), rotation);
                Box(brass, entrance + rotation * new Vector3(side * (cellSize / 2 - 0.12f), 2.55f, -0.23f),
                    new Vector3(0.16f, 0.35f, 0.06f), rotation);
            }
            Box(darkStone, entrance + Vector3.up * 2.95f, new Vector3(cellSize, 0.14f, 0.38f), rotation);
        }

        public void Exit(Vector3 center, bool seal = true)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Box(stone, center + new Vector3(side * 0.85f, 1.1f, 0), new Vector3(0.34f, 2.2f, 0.42f));
                Box(brass, center + new Vector3(side * 0.85f, 0.12f, 0), new Vector3(0.44f, 0.24f, 0.5f));
            }
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI / 6f;
                Box(stone, center + new Vector3(Mathf.Cos(angle) * 0.85f, 2.1f + Mathf.Sin(angle) * 0.6f, 0),
                    new Vector3(0.38f, 0.35f, 0.42f), Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg - 90));
            }
            if (seal) Box(rune, center + Vector3.up * 1.2f, new Vector3(1.25f, 2.05f, 0.07f));
            for (int i = 0; i < 3; i++) Box(brass, center + new Vector3((i - 1) * 0.32f, 1.35f, -0.06f), new Vector3(0.13f, 0.27f, 0.05f));
        }

        public void Finish(Camera camera)
        {
            foreach (var batch in batches)
            for (int start = 0; start < batch.Value.Count; start += 1024)
            {
                var mesh = new Mesh { name = "Dungeon " + batch.Key.material.name, indexFormat = IndexFormat.UInt32 };
                owned.Add(mesh);
                mesh.CombineMeshes(batch.Value.GetRange(start, Mathf.Min(1024, batch.Value.Count - start)).ToArray());
                var section = new GameObject(mesh.name); section.transform.SetParent(geometry, false);
                section.AddComponent<MeshFilter>().sharedMesh = mesh;
                section.AddComponent<MeshRenderer>().sharedMaterial = batch.Key.material;
            }
            batches.Clear();
            if (camera == null) return;
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask=1<<0;
            data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;camera.allowHDR=true;
            var lantern = new GameObject("Player Lantern"); lantern.transform.SetParent(camera.transform, false);
            var light = lantern.AddComponent<Light>(); light.type = LightType.Point;
            bool horror = FindFirstObjectByType<HorrorAmbience>() != null;
            light.range = horror ? 4.5f : 5f; light.intensity = horror ? .85f : 1.6f; light.color = new Color(0.85f, 0.75f, 0.57f);
            light.shadows = LightShadows.None;
        }
        private void Torch(Vector3 position, Vector3 direction)
        {
            Box(iron, position + Vector3.up * 1.65f, new Vector3(0.13f, 0.38f, 0.13f));
            Box(wood, position + Vector3.up * 1.98f, new Vector3(0.10f, 0.50f, 0.10f));
            Box(flame, position + Vector3.up * 2.28f, new Vector3(0.15f, 0.28f, 0.15f), Quaternion.Euler(0, 0, 12));
            if (torches.Count >= 24) return;
            var torch = new GameObject("Torch Light"); torch.transform.SetParent(geometry, false);
            torch.transform.localPosition = position + Vector3.up * 2.3f + direction * 0.15f;
            var light = torch.AddComponent<Light>(); light.type = LightType.Point;
            light.color = currentSection == 1 ? new Color(.42f,.75f,.72f) : currentSection == 4 ? new Color(.52f,.55f,1f) : new Color(1f,.53f,.19f); light.range = 5.5f; light.intensity = 2.7f;
            light.shadows = LightShadows.None; torches.Add(light);
        }
        private void Update()
        {
            for (int i = 0; i < torches.Count; i++)
                if (torches[i] != null) torches[i].intensity = 2.35f + Mathf.PerlinNoise(i * 7.3f, Time.time * 2.2f) * .55f;
        }
        private Material Material(Material template, string name, Color color, float metallic = 0)
        {
            var material = new Material(template) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", metallic > 0 ? 0.35f : 0.1f);
            owned.Add(material); return material;
        }
        private Texture2D StoneTexture(int seed)
        {
            var texture = new Texture2D(128, 128, TextureFormat.RGB24, true) { name = "Seeded Stone Grain", wrapMode = TextureWrapMode.Repeat };
            var grain = new System.Random(seed); var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float value = 0.65f + Mathf.PerlinNoise(x * 0.06f, y * 0.06f) * 0.3f + (float)grain.NextDouble() * 0.12f;
                if (grain.Next(90) == 0) value *= 0.55f;
                pixels[y * 128 + x] = new Color(value, value, value);
            }
            texture.SetPixels(pixels); texture.Apply(true, true); owned.Add(texture); return texture;
        }
        private void BuildWallMaterials(int seed)
        {
            const int size = 256;
            var rng = new System.Random(unchecked(seed ^ 0x3E29));
            var heights = new float[size * size]; var cracks = new float[size * size];
            var diffuse = new Color[size * size]; var normals = new Color[size * size];
            float offset = rng.Next(1000);
            // Independent wall maps leave the existing floor/water materials intact.
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float broad=Mathf.PerlinNoise(offset+x*.024f,offset+y*.024f);
                float grain=Mathf.PerlinNoise(offset+x*.21f,offset+y*.21f);
                heights[y*size+x]=.30f+broad*.35f+grain*.10f;
            }
            for(int vein=0;vein<7;vein++)
            {
                float x=rng.Next(size),y=rng.Next(size),direction=(float)rng.NextDouble()*Mathf.PI*2;
                for(int step=0;step<110;step++)
                {
                    int px=Mathf.RoundToInt(x),py=Mathf.RoundToInt(y);
                    if(px<2||py<2||px>=size-2||py>=size-2)break;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    { int index=(py+dy)*size+px+dx;cracks[index]=Mathf.Max(cracks[index],dx==0&&dy==0?1f:.3f); }
                    direction+=((float)rng.NextDouble()-.5f)*.45f;x+=Mathf.Cos(direction)*1.7f;y+=Mathf.Sin(direction)*1.7f;
                }
            }
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                int i=y*size+x;float pit=rng.NextDouble()<.018 ? .20f : 0;
                float damp=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.72f,Mathf.PerlinNoise(offset+x*.018f,offset+y*.035f)));
                float value=Mathf.Clamp01(.65f+heights[i]*.65f-cracks[i]*.40f-pit-damp*.18f);
                diffuse[i]=new Color(value,value*.98f,value*.94f);
                heights[i]-=cracks[i]*.18f+pit*.25f;
            }
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=heights[y*size+(x+1)%size]-heights[y*size+(x+size-1)%size];
                float dy=heights[((y+1)%size)*size+x]-heights[((y+size-1)%size)*size+x];
                Vector3 normal=new Vector3(-dx*3.5f,-dy*3.5f,1).normalized;
                // RGB normal with alpha=1 supports the installed URP RG-or-AG decoder.
                normals[y*size+x]=new Color(normal.x*.5f+.5f,normal.y*.5f+.5f,normal.z*.5f+.5f,1);
            }
            var albedo=new Texture2D(size,size,TextureFormat.RGBA32,true,false) { name="Wall stone — cracks and damp grain",wrapMode=TextureWrapMode.Repeat,anisoLevel=4 };
            albedo.SetPixels(diffuse);albedo.Apply(true,true);owned.Add(albedo);
            var bump=new Texture2D(size,size,TextureFormat.RGBA32,true,true) { name="Wall stone — linear surface normals",wrapMode=TextureWrapMode.Repeat,anisoLevel=4 };
            bump.SetPixels(normals);bump.Apply(true,true);owned.Add(bump);
            wallStone=new Material[5];wallDamp=new Material[5];
            for(int section=0;section<5;section++)for(int variant=0;variant<2;variant++)
            {
                var source=variant==0?sectionStone[section]:sectionDark[section];
                var material=new Material(source) { name=variant==0?"Rough wall stone "+section:"Damp wall stone "+section };
                owned.Add(material);material.SetTexture("_BaseMap",albedo);material.SetTexture("_BumpMap",bump);
                material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.65f);material.SetFloat("_Smoothness",variant==0?.08f:.24f);
                Color tint=source.GetColor("_BaseColor")*1.25f;tint.a=1;
                material.SetFloat("_Metallic",0);material.SetColor("_BaseColor",tint);
                if(variant==0)wallStone[section]=material;else wallDamp[section]=material;
            }
        }
        private void Box(Material material, Vector3 position, Vector3 scale, Quaternion? rotation = null)
            => Shape(cube, material, position, scale, rotation ?? Quaternion.identity);
        private void Shape(Mesh shape, Material material, Vector3 position, Vector3 scale, Quaternion rotation)
        {
            // Spatial batches keep URP's nearby-light selection and culling local to corridors.
            var key = (material, Mathf.FloorToInt(position.x / 8f), Mathf.FloorToInt(position.z / 8f));
            if (!batches.TryGetValue(key, out var list)) { list = new List<CombineInstance>(); batches.Add(key, list); }
            list.Add(new CombineInstance { mesh = shape, transform = Matrix4x4.TRS(position, rotation, scale) });
        }
        private void OnDestroy() { foreach (Object asset in owned) if (asset != null) Destroy(asset); }
    }
}
