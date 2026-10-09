using UnityEngine;
using UnityEngine.InputSystem;
namespace Soulbound
{
    // Procedural placeholder audio is owned by the run; replace with authored clips later.
    public sealed class HorrorAmbience : MonoBehaviour
    {
        private Transform player;
        private ProceduralMaze maze;
        private RunState run;
        private FirstPersonController controller;
        private CharacterController body;
        private Light flashlight;
        private AudioSource ambient, steps;
        private AudioClip hum, footstep;
        private ShallowWater water;
        private const float BatteryLife = 600f;
        private float battery = BatteryLife, nextStep;
        private bool lit = true;
        public string FlashlightStatus => "Lamp " + Mathf.CeilToInt(battery / BatteryLife * 100) + "% · G / LB";
        public void Configure(ProceduralMaze source,Transform target)
        {
            maze=source;player=target;run=FindFirstObjectByType<RunState>();controller=player.GetComponent<FirstPersonController>();body=player.GetComponent<CharacterController>();
            var lamp=new GameObject("Player flashlight"); lamp.transform.SetParent(player.GetComponentInChildren<Camera>().transform,false);
            flashlight=lamp.AddComponent<Light>();flashlight.type=LightType.Spot;flashlight.range=15;flashlight.spotAngle=55;flashlight.intensity=2;flashlight.color=new Color(.87f,.86f,.72f);flashlight.shadows=LightShadows.Soft;
            ambient=gameObject.AddComponent<AudioSource>();ambient.loop=true;ambient.volume=.055f;ambient.spatialBlend=0;
            steps=gameObject.AddComponent<AudioSource>();steps.spatialBlend=0;steps.volume=.10f;
            hum=MakeClip("Dungeon drone",3f,false);footstep=MakeClip("Footstep / splash",.18f,true);ambient.clip=hum;ambient.Play();
        }
        public static AudioClip MakeClip(string name,float length,bool strike)
        {
            const int rate=12000;var data=new float[Mathf.RoundToInt(length*rate)];var rng=new System.Random(41);float filtered=0;
            for(int i=0;i<data.Length;i++)
            {
                float t=(float)i/rate;filtered=Mathf.Lerp(filtered,(float)rng.NextDouble()*2-1,.12f);
                float envelope=strike?Mathf.Exp(-t*28):Mathf.Sin(Mathf.PI*i/(data.Length-1));
                data[i]=(Mathf.Sin(t*2*Mathf.PI*(strike?95:49))*.3f+filtered*.6f)*envelope;
            }
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        public static void Noise(Vector3 point,float radius)
        { foreach(var enemy in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))enemy.HearNoise(point,radius); }
        private void Update()
        {
            if(player==null)return;
            if(run.HasEnded) { flashlight.enabled=false;ambient.Stop();return; }
            bool toggle=(Keyboard.current!=null && Keyboard.current.gKey.wasPressedThisFrame)||(Gamepad.current!=null && Gamepad.current.leftShoulder.wasPressedThisFrame);
            if(!controller.InputBlocked && toggle)lit=!lit;
            if(lit)battery=Mathf.Max(0,battery-Time.deltaTime);
            flashlight.enabled=lit && battery>0;
            int x=Mathf.Clamp(Mathf.RoundToInt(player.position.x/maze.CellSize+(maze.Layout.Width-1)*.5f),0,maze.Layout.Width-1);
            int z=Mathf.Clamp(Mathf.RoundToInt(player.position.z/maze.CellSize+(maze.Layout.Height-1)*.5f),0,maze.Layout.Height-1);
            int section=HorrorDungeon.SectionFor(maze.Layout,z*maze.Layout.Width+x);
            if(water==null)water=maze.GetComponent<ShallowWater>();
            ambient.pitch=Mathf.Lerp(ambient.pitch,new[] { .8f,1.3f,.65f,1f,1.5f }[section],Time.deltaTime);
            if(!controller.InputBlocked && body.isGrounded && new Vector2(body.velocity.x,body.velocity.z).magnitude>.5f && Time.time>=nextStep)
            {
                bool wet=water!=null&&water.Contains(player.position);
                nextStep=Time.time+.48f;steps.pitch=wet?1.6f: .8f;steps.PlayOneShot(footstep,wet?1f:.65f);
                if(wet)Noise(player.position,7f);
            }
        }
        private void OnDestroy() { if(flashlight!=null)Destroy(flashlight.gameObject);if(hum!=null)Destroy(hum);if(footstep!=null)Destroy(footstep); }
    }
}
