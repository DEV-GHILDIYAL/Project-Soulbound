using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Soulbound
{
    // World-spanning puzzles, journal and section dressing share one seeded run.
    public sealed class HorrorDungeon : MonoBehaviour
    {
        public static readonly string[] Sections = { "PRISON WING", "FLOODED CATACOMBS", "FURNACE WORKSHOP", "ABANDONED CHAPEL", "ASTRAL WING" };
        public HorrorQuestState State { get; } = new HorrorQuestState();
        private readonly List<HorrorInteraction> nodes = new List<HorrorInteraction>();
        private readonly List<string> pages = new List<string>();
        private readonly List<Material> owned = new List<Material>();
        private Texture2D frost;
        private readonly HashSet<int> occupied = new HashSet<int>();
        public List<Vector3> RequiredApproaches { get; } = new List<Vector3>();
        public bool ReserveDressing(int cell) => occupied.Contains(cell);
        private ProceduralMaze maze;
        private Transform player, environment;
        private FirstPersonController controller;
        private Health health;
        private RunState run;
        private Camera view;
        private Material iron, skin, fabric, paper, glass, ember;
        private GameObject canvas, journal;
        private Text journalText, prompt, zoneText, status;
        private bool reading;
        private int page, openedFrame;
        private float toastUntil;
        private System.Random random;
        private int[] pressureOrder;
        private int correctStatue;
        private HorrorAmbience ambience;
        public static int SectionFor(MazeLayout layout, int cell)
        {
            float x = (cell % layout.Width + .5f) / layout.Width, z = (cell / layout.Width + .5f) / layout.Height;
            var anchors = new[] { new Vector2(.15f,.18f),new Vector2(.18f,.80f),new Vector2(.82f,.20f),new Vector2(.46f,.52f),new Vector2(.84f,.82f) };
            // Districts grow around their key destinations, rather than placing a
            // furnace vault in a visually unrelated prison district.
            if (layout.Rooms.Length == 3)
            {
                for (int room = 0; room < 3; room++)
                {
                    float rx = 0, rz = 0;
                    foreach (int c in layout.Rooms[room].Cells) { rx += c % layout.Width + .5f; rz += c / layout.Width + .5f; }
                    int index = room == 0 ? 1 : room == 1 ? 2 : 4;
                    anchors[index] = new Vector2(rx/layout.Rooms[room].Cells.Length/layout.Width,rz/layout.Rooms[room].Cells.Length/layout.Height);
                }
            }
            int nearest = 0; float distance = float.MaxValue;
            for (int i = 0; i < anchors.Length; i++)
            { float d = (new Vector2(x,z)-anchors[i]).sqrMagnitude * (i == 3 ? 1.2f : 1f); if (d < distance) { distance = d; nearest = i; } }
            return nearest;
        }
        public void Build(ProceduralMaze source, Transform target, Transform parent, Material template, KeyRoom[] rooms)
        {
            maze = source; player = target; environment = parent; random = new System.Random(unchecked(maze.Seed ^ 0x711A5));
            controller = player.GetComponent<FirstPersonController>(); health = player.GetComponent<Health>();
            run = FindFirstObjectByType<RunState>(); view = player.GetComponentInChildren<Camera>(); health.Damaged += Damage;
            iron = Mat(template,"Rust",new Color(.20f,.13f,.09f)); skin = Mat(template,"Ashen remains",new Color(.40f,.37f,.33f));
            fabric = Mat(template,"Shroud",new Color(.13f,.10f,.11f)); paper = Mat(template,"Old parchment",new Color(.72f,.60f,.38f));
            // Frosted opaque glass deliberately obscures the outside silhouette.
            glass = Mat(template,"Frosted window",new Color(.18f,.30f,.33f)); glass.EnableKeyword("_EMISSION"); glass.SetColor("_EmissionColor",new Color(.09f,.20f,.23f));
            frost=new Texture2D(64,64,TextureFormat.RGB24,true);frost.name="Window grime and frost";var pixels=new Color[4096];
            for(int i=0;i<pixels.Length;i++) { float grain=.55f+(float)random.NextDouble()*.4f;pixels[i]=new Color(grain,grain,grain); }
            frost.SetPixels(pixels);frost.Apply(true,true);glass.SetTexture("_BaseMap",frost);
            ember = Mat(template,"Boiler ember",new Color(.18f,.08f,.03f)); ember.EnableKeyword("_EMISSION"); ember.SetColor("_EmissionColor",Color.black);
            occupied.Add(0); occupied.Add(maze.Layout.Exit);
            foreach (var room in maze.Layout.Rooms) { foreach (int cell in room.Cells) occupied.Add(cell); occupied.Add(room.DoorCell); }
            BuildUI();
            ambience=gameObject.AddComponent<HorrorAmbience>();ambience.Configure(maze,player);
            correctStatue = random.Next(5); pressureOrder = new[] { 0,1,2 };
            for (int i = 2; i > 0; i--) { int j = random.Next(i+1); int temp = pressureOrder[i]; pressureOrder[i] = pressureOrder[j]; pressureOrder[j] = temp; }
            Note("guard-note","Guard's diary",0,"The caretaker took the chapel crest into the flooded burial tunnels. Find his grave marked with an eye. His epitaph records which saint receives the crest.");
            Note("epitaph","Caretaker's epitaph",1,"I watched the prison from the water. Carry my crest to the chapel. Only the " + RoomChallenge.Symbols[correctStatue] + " saint should receive it; the others are false witnesses.","guard-note");
            Item("crest","Caretaker's grave — take crest",1,"crest","epitaph");
            for (int i = 0; i < 5; i++) Station("statue-"+i,RoomChallenge.Symbols[i]+" saint — place crest",3,"statue","crest");
            Note("maintenance","Furnace maintenance log",2,"The boiler has lost its valve and crank. A spare valve was stored in the prison; the crank was taken to the catacombs. Fit both at the workshop. Then turn pressure handles in this order: " + string.Join(" → ",System.Array.ConvertAll(pressureOrder,i => new[] { "Copper", "Iron", "Ash" }[i])) + ". The furnace seal opens when pressure is restored.");
            Item("valve","Spare valve",0,"valve","maintenance"); Item("crank","Iron crank",1,"crank","maintenance");
            Station("valve-fit","Valve socket",2,"valve-fit","valve"); Station("crank-fit","Crank assembly",2,"crank-fit","crank");
            for (int i = 0; i < 3; i++) Station("pressure-"+i,new[] { "Copper", "Iron", "Ash" }[i]+" pressure handle",2,"pressure","valve-fit","crank-fit");
            string[] clues = rooms[2].Challenge.Clues;
            Note("astral-0","Astronomer's journal I",3,"The missing ritual medallion rests in the prison shrine. Place it in the astral socket before attempting alignment.\n\n" + clues[0] + "\n" + clues[1]);
            Note("astral-1","Astronomer's journal II",1,clues[2] + "\n" + clues[4]);
            Note("astral-2","Observatory inscription",4,clues[3] + "\nThe alignment reveals the final key.");
            Item("medallion","Prison shrine — take medallion",0,"medallion","astral-0");
            Station("medallion-fit","Astral medallion socket",4,"medallion-fit","medallion");
            for (int i = 0; i < rooms.Length; i++) rooms[i].SetJourney(this,i);
            BodyBagStorage();
            Dress();
        }
        private Material Mat(Material source, string name, Color color)
        { var result = new Material(source) { name = name }; result.SetColor("_BaseColor",color); owned.Add(result); return result; }
        private int Site(int section)
        {
            var sites = new List<int>();
            for (int cell = 1; cell < maze.Layout.Count; cell++) if (!occupied.Contains(cell) && SectionFor(maze.Layout,cell) == section) sites.Add(cell);
            if (sites.Count == 0) for (int cell = 1; cell < maze.Layout.Count; cell++) if (!occupied.Contains(cell)) sites.Add(cell);
            if (sites.Count == 0) throw new System.InvalidOperationException("No reachable quest site remains.");
            int chosen = sites[random.Next(sites.Count)]; occupied.Add(chosen); return chosen;
        }
        private void Note(string id,string title,int section,string text,params string[] requirements) => Node(id,title,section,text,null,requirements);
        private void Item(string id,string title,int section,string reward,params string[] requirements) => Node(id,title,section,null,reward,requirements);
        private void Station(string id,string title,int section,string reward,params string[] requirements) => Node(id,title,section,null,reward,requirements);
        private void Node(string id,string title,int section,string text,string reward,string[] requirements)
        {
            int cell = Site(section); Vector3 center = maze.CellPosition(cell);
            RequiredApproaches.Add(center);
            int direction = 0; for (int d = 0; d < 4; d++) if (!maze.Layout.IsOpen(cell,d)) { direction = d; break; }
            Quaternion rot = Quaternion.Euler(0,direction * 90,0);
            Vector3 point = center + rot * new Vector3(0,.85f,1.1f);
            var root = new GameObject(title); root.transform.SetParent(environment,false); root.transform.position = point; root.transform.rotation = rot;
            var box = root.AddComponent<BoxCollider>(); box.size = new Vector3(.75f,1.4f,.65f); box.isTrigger = true;
            var node = root.AddComponent<HorrorInteraction>(); node.Id=id; node.Title=title; node.Content=text; node.Reward=reward; node.Requirements=requirements; nodes.Add(node);
            Part(root.transform,PrimitiveType.Cube,new Vector3(0,-.55f,0),new Vector3(.8f,.45f,.6f),iron);
            if (text != null) Part(root.transform,PrimitiveType.Cube,new Vector3(0,.08f,-.12f),new Vector3(.5f,.06f,.35f),paper);
            else if (id.StartsWith("statue"))
            {
                Part(root.transform,PrimitiveType.Capsule,new Vector3(0,.15f,0),new Vector3(.36f,.55f,.3f),skin);
                Part(root.transform,PrimitiveType.Sphere,new Vector3(0,.72f,0),Vector3.one*.26f,skin);
            }
            else if (id == "crest")
            {
                Part(root.transform,PrimitiveType.Cube,new Vector3(0,.25f,.15f),new Vector3(.55f,.8f,.18f),skin);

                Part(root.transform,PrimitiveType.Sphere,new Vector3(0,.15f,-.25f),new Vector3(.22f,.22f,.06f),paper);
            }
            else if (id.Contains("valve") || id.StartsWith("pressure"))
            {
                for(int i=0;i<6;i++)
                {
                    float angle=i*Mathf.PI/3;
                    Part(root.transform,PrimitiveType.Sphere,new Vector3(Mathf.Cos(angle)*.22f,.25f+Mathf.Sin(angle)*.22f,-.12f),Vector3.one*.11f,paper);
                }
                Part(root.transform,PrimitiveType.Cube,new Vector3(0,.25f,-.12f),new Vector3(.45f,.05f,.08f),iron);
            }
            else if (id.Contains("crank"))
            {
                Part(root.transform,PrimitiveType.Cube,new Vector3(0,.23f,-.12f),new Vector3(.35f,.07f,.07f),iron);
                Part(root.transform,PrimitiveType.Cube,new Vector3(.15f,.36f,-.12f),new Vector3(.07f,.3f,.07f),paper);
            }
            else
            {
                Part(root.transform,PrimitiveType.Cylinder,new Vector3(0,.12f,0),new Vector3(.32f,.08f,.32f),paper);
                Part(root.transform,PrimitiveType.Cube,new Vector3(0,.25f,0),new Vector3(.1f,.4f,.1f),iron);
            }

            var light = root.AddComponent<Light>(); light.type=LightType.Point; light.range=3; light.intensity=.65f; light.color=new Color(.78f,.68f,.48f); light.shadows=LightShadows.None;
        }
        private void BuildUI()
        {
            canvas = RuntimeUI.Canvas("Exploration journal",55);
            var panel = RuntimeUI.Panel(canvas.transform,"Journal",new Vector2(.5f,.5f),Vector2.zero,new Vector2(720,520)); journal=panel.gameObject;
            journalText=RuntimeUI.Label(panel,"",28,24,664,365,19); journalText.alignment=TextAnchor.UpperLeft;
            RuntimeUI.Button(panel,"Previous",28,410,155,42,()=>ShowPage(-1)); RuntimeUI.Button(panel,"Next",194,410,155,42,()=>ShowPage(1));
            RuntimeUI.Button(panel,"Close · B / Esc",515,410,177,42,Close);
            RuntimeUI.Label(panel,"J / controller View: journal · arrows / D-pad: pages",28,468,664,26,15);
            journal.SetActive(false);
            var bottom=RuntimeUI.Rect(canvas.transform,"World prompt",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,125),new Vector2(660,38));
            prompt=RuntimeUI.Label(bottom,"",0,0,660,38,17); prompt.alignment=TextAnchor.MiddleCenter;
            zoneText=RuntimeUI.Label(canvas.transform,"",24,22,580,32,16); zoneText.color=RuntimeUI.Gold;
            status=RuntimeUI.Label(canvas.transform,"",24,60,650,32,16);
        }
        private void ShowPage(int delta)
        {
            if (pages.Count == 0) { journalText.text="JOURNAL\n\nNo notes found yet. Read inscriptions and diaries in the dungeon.\n\nQuest items: " + InventoryText(); return; }
            page=(page+delta+pages.Count)%pages.Count;
            journalText.text="JOURNAL  " +(page+1)+" / "+pages.Count+"\n\n"+pages[page]+"\n\nQuest items: "+InventoryText();
        }
        private string InventoryText()
        { var list=new List<string>(); foreach(string id in new[] { "crest","valve","crank","medallion" }) if(State.Has(id)) list.Add(id); return list.Count==0 ? "none" : string.Join(", ",list); }
        private void Open() { reading=true; openedFrame=Time.frameCount; controller.SetInteractionMode(true); journal.SetActive(true); ShowPage(0); }
        private void Close() { if(!reading)return; reading=false; journal.SetActive(false); controller.SetInteractionMode(false); }
        private void Damage(float amount) { Close(); Toast("Reading interrupted."); }
        public void Toast(string text) { status.text=text; toastUntil=Time.time+4; }
        private void Use(HorrorInteraction node)
        {
            if(!State.HasAll(node.Requirements)) { Toast("Something is missing. Search the notes and related rooms."); return; }
            if(node.Id.StartsWith("statue-"))
            {
                if(node.Id != "statue-"+correctStatue) { Toast("The crest does not fit this saint. Check the epitaph."); return; }
            }
            if(node.Id.StartsWith("pressure-"))
            {
                if(State.Has("pressure")) { Toast("The boiler is already running."); return; }
                bool complete=State.TurnPressure(int.Parse(node.Id.Substring(9)),pressureOrder);
                if(complete) { HorrorAmbience.Noise(node.transform.position,22f);ember.SetColor("_BaseColor",new Color(.7f,.2f,.04f));ember.SetColor("_EmissionColor",new Color(1.1f,.23f,.03f)); }
                Toast(complete ? "The boiler starts. Furnace seal released." : "Pressure sequence: " + State.PressureStep + " / 3. Check the maintenance log."); return;
            }
            if(node.Content != null)
            {
                if(State.Add(node.Id)) { pages.Add(node.Title+"\n\n"+node.Content); page=pages.Count-1; }
                else { int index=pages.FindIndex(p=>p.StartsWith(node.Title+"\n")); if(index>=0)page=index; }
                Open(); return;
            }
            if(State.Add(node.Reward)) { Toast(node.Title+" — complete"); if(node.Reward=="statue")HorrorAmbience.Noise(node.transform.position,12f); if(node.Reward == node.Id && !node.Id.Contains("fit"))node.gameObject.SetActive(false); }
            else Toast("Already completed.");
        }
        private void Update()
        {
            if(player == null)return;
            if(run.HasEnded || !health.IsAlive) { Close(); prompt.text=""; zoneText.text=""; return; }
            int x=Mathf.Clamp(Mathf.RoundToInt(player.position.x/maze.CellSize+(maze.Layout.Width-1)*.5f),0,maze.Layout.Width-1);
            int z=Mathf.Clamp(Mathf.RoundToInt(player.position.z/maze.CellSize+(maze.Layout.Height-1)*.5f),0,maze.Layout.Height-1);
            zoneText.text=Sections[SectionFor(maze.Layout,z*maze.Layout.Width+x)]+"  ·  J / View: journal";
            if(Time.time>toastUntil)status.text=ambience.FlashlightStatus;
            bool journalPressed=(Keyboard.current!=null && Keyboard.current.jKey.wasPressedThisFrame)||(Gamepad.current!=null && Gamepad.current.selectButton.wasPressedThisFrame);
            if(reading)
            {
                prompt.text="";
                if(Time.frameCount>openedFrame+1) { if(PlayerControls.CancelPressed || journalPressed)Close(); else if(PlayerControls.MenuStep!=0)ShowPage(PlayerControls.MenuStep); }
                return;
            }
            prompt.text=""; if(controller.InputBlocked)return;
            if(journalPressed) { Open(); return; }
            if(Physics.Raycast(view.transform.position,view.transform.forward,out RaycastHit hit,2.7f,~(1<<2),QueryTriggerInteraction.Collide))
            {
                var node=hit.collider.GetComponent<HorrorInteraction>();
                if(node!=null) { prompt.text=PlayerControls.InteractLabel+": "+node.Title; if(PlayerControls.InteractPressed)Use(node); }
            }
        }
        private void Dress()
        {
            var water=gameObject.AddComponent<ShallowWater>();water.Build(iron,maze.CellSize);
            for(int cell=0;cell<maze.Layout.Count;cell++)
            {
                if(occupied.Contains(cell))continue;
                Vector3 c=maze.CellPosition(cell); int zone=SectionFor(maze.Layout,cell);
                bool ns=maze.Layout.IsOpen(cell,0)&&maze.Layout.IsOpen(cell,2)&&!maze.Layout.IsOpen(cell,1)&&!maze.Layout.IsOpen(cell,3);
                bool ew=maze.Layout.IsOpen(cell,1)&&maze.Layout.IsOpen(cell,3)&&!maze.Layout.IsOpen(cell,0)&&!maze.Layout.IsOpen(cell,2);
                if((ns||ew) && cell%4==0)
                {
                    occupied.Add(cell);
                    var dressing=maze.GetComponent<DungeonDressing>();
                    if(dressing!=null)dressing.NarrowPassage(c,Quaternion.Euler(0,ew?90:0,0),zone);
                    continue;
                }
                int d=-1;for(int i=0;i<4;i++)if(!maze.Layout.IsOpen(cell,i)){d=i;break;} if(d<0)continue;
                var root=new GameObject(Sections[zone]+" detail").transform;root.SetParent(environment,false);root.position=c;root.rotation=Quaternion.Euler(0,d*90,0);

                if(zone==0 && cell%5==0)
                {
                    for(int bar=0;bar<6;bar++)Part(root,PrimitiveType.Cylinder,new Vector3(-.9f+bar*.36f,1.35f,1.6f),new Vector3(.04f,1.2f,.04f),iron);
                    root.gameObject.AddComponent<HorrorWindow>().Configure(player,null,random.NextDouble()*4,true);
                }
                if(zone==1)
                {
                    if(cell%3!=0)water.Patch(environment,c);
                    for(int i=0;i<3;i++)Part(root,PrimitiveType.Cylinder,new Vector3(-.8f+i*.65f,1.8f,1.7f),new Vector3(.08f,1.1f,.08f),iron);
                }
                if(zone==2 && cell%3==0)
                {
                    Part(root,PrimitiveType.Cylinder,new Vector3(0,.8f,1.55f),new Vector3(.65f,.8f,.65f),iron);
                    Part(root,PrimitiveType.Cube,new Vector3(0,.6f,1.15f),new Vector3(.3f,.18f,.03f),ember);
                }
                if(zone==3 && cell%5==0)
                { Part(root,PrimitiveType.Cube,new Vector3(0,1.7f,1.7f),new Vector3(.18f,1.5f,.1f),paper);Part(root,PrimitiveType.Cube,new Vector3(0,1.95f,1.68f),new Vector3(.75f,.13f,.12f),paper); }
                if(cell%13==0 && random.NextDouble()<.65) Window(root);
            }
        }
        private void BodyBagStorage()
        {
            var candidates = new List<int>();
            for(int cell=1;cell<maze.Layout.Count;cell++)
                if(!occupied.Contains(cell) && SectionFor(maze.Layout,cell)==0 &&
                    ((maze.Layout.IsOpen(cell,0)&&maze.Layout.IsOpen(cell,2)&&!maze.Layout.IsOpen(cell,1)&&!maze.Layout.IsOpen(cell,3)) ||
                     (maze.Layout.IsOpen(cell,1)&&maze.Layout.IsOpen(cell,3)&&!maze.Layout.IsOpen(cell,0)&&!maze.Layout.IsOpen(cell,2)))) candidates.Add(cell);
            if(candidates.Count==0)for(int cell=1;cell<maze.Layout.Count;cell++)
                if(!occupied.Contains(cell)&&SectionFor(maze.Layout,cell)==0)candidates.Add(cell);
            if(candidates.Count==0)return;
            int site=candidates[random.Next(candidates.Count)];occupied.Add(site);
            var storage=new GameObject("Prison mortuary — body bag storage").transform;
            storage.SetParent(environment,false);storage.position=maze.CellPosition(site);
            if(maze.Layout.IsOpen(site,1)&&maze.Layout.IsOpen(site,3))storage.rotation=Quaternion.Euler(0,90,0);
            // Two wall-side rows preserve the centre lane and every cell entrance.
            for(int side=-1;side<=1;side+=2) for(int slot=0;slot<3;slot++)
            {
                var anchor=new GameObject("Suspended body bag");anchor.transform.SetParent(storage,false);
                anchor.transform.localPosition=new Vector3(side*1.25f,2.85f,(slot-1)*1.05f);
                anchor.transform.localRotation=Quaternion.Euler(0,side<0?90:-90,0);
                anchor.AddComponent<HangingBodyBag>().Build(fabric,iron,paper,player,slot+(side>0?3:0));
            }
        }
        private void Window(Transform parent)
        {
            var root=new GameObject("Fogged observation window").transform;root.SetParent(parent,false);root.localPosition=new Vector3(0,1.7f,1.68f);
            Part(root,PrimitiveType.Cube,Vector3.zero,new Vector3(1.65f,1.15f,.08f),glass);
            for(int s=-1;s<=1;s+=2)
            { Part(root,PrimitiveType.Cube,new Vector3(s*.85f,0,-.05f),new Vector3(.1f,1.35f,.12f),iron);Part(root,PrimitiveType.Cube,new Vector3(0,s*.62f,-.05f),new Vector3(1.8f,.1f,.12f),iron); }
            // Blurred silhouette is painted into the pane: the structural wall stays sealed.
            var figure=new GameObject("Outside silhouette").transform;figure.SetParent(root,false);figure.localPosition=new Vector3(0,0,-.055f);
            Part(figure,PrimitiveType.Sphere,new Vector3(0,.2f,0),new Vector3(.32f,.4f,.025f),fabric);
            Part(figure,PrimitiveType.Capsule,new Vector3(0,-.25f,0),new Vector3(.5f,.4f,.02f),fabric);
            for(int s=-1;s<=1;s+=2)Part(figure,PrimitiveType.Sphere,new Vector3(s*.5f,.1f,0),new Vector3(.16f,.23f,.018f),fabric);
            root.gameObject.AddComponent<HorrorWindow>().Configure(player,figure.gameObject,random.NextDouble()*5,true);
        }
        private GameObject Part(Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {
            var obj=GameObject.CreatePrimitive(type); obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;
            obj.GetComponent<Collider>().enabled=false;Destroy(obj.GetComponent<Collider>());obj.GetComponent<Renderer>().sharedMaterial=material;return obj;
        }
        private void OnDestroy()
        { if(health!=null)health.Damaged-=Damage;Close();if(canvas!=null)Destroy(canvas);foreach(var material in owned)Destroy(material);if(frost!=null)Destroy(frost); }
    }
}
