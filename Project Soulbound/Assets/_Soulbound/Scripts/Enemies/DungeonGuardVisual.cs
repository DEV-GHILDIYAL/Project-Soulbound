using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace Soulbound
{
    public sealed class DungeonGuardVisual : MonoBehaviour
    {
        private readonly List<Material> owned = new List<Material>();
        private Transform leftArm, rightArm, leftLeg, rightLeg, torso;
        private EnemyAI ai;
        private NavMeshAgent agent;
        private float phase;
        private Transform[] thighs = new Transform[2], shins = new Transform[2], boots = new Transform[2];
        private readonly Vector3[] planted = new Vector3[2], swingStart = new Vector3[2], landing = new Vector3[2];
        private readonly bool[] swinging = new bool[2];
        private Vector3 previousPosition;
        private bool feetReady;
        private AudioSource footsteps;
        private AudioClip footstepClip;
        private readonly RaycastHit[] groundHits = new RaycastHit[12];
        private Transform head;
        public void Build(Material template, int generation)
        {
            agent = GetComponent<NavMeshAgent>();
            Material flesh = Make(template,"Ashen creature skin",generation>0?new Color(.38f,.34f,.32f):new Color(.44f,.47f,.39f),0);
            Material bone = Make(template,"Old ivory claws",new Color(.54f,.50f,.37f),0);
            Material shadow = Make(template,"Sunken sockets",new Color(.035f,.025f,.023f),0);
            Material cloth = Make(template,"Rotten burial cloth",new Color(.13f,.14f,.10f),0);
            Material eyes = Make(template,"Faint sickly eyes",new Color(.65f,.59f,.27f),0);
            eyes.EnableKeyword("_EMISSION");eyes.SetColor("_EmissionColor",new Color(.7f,.5f,.12f));
            torso = Pivot("Hunched creature torso",new Vector3(0,1.12f,0));
            Part(torso,"Gaunt rib cage",new Vector3(0,.05f,0),new Vector3(.43f,.34f,.28f),flesh,PrimitiveType.Capsule);
            Part(torso,"Narrow abdomen",new Vector3(0,-.22f,-.01f),new Vector3(.24f,.23f,.22f),shadow,PrimitiveType.Capsule);
            Part(torso,"Hunched back",new Vector3(0,.25f,-.13f),new Vector3(.39f,.31f,.27f),flesh,PrimitiveType.Sphere);
            Part(torso,"Pelvis cloth",new Vector3(0,-.32f,0),new Vector3(.35f,.22f,.26f),cloth,PrimitiveType.Capsule);
            for(int rib=0;rib<4;rib++)for(int side=-1;side<=1;side+=2)
            {
                Part(torso,"Raised rib",new Vector3(side*.105f,.18f-rib*.075f,.13f),new Vector3(.16f,.035f,.045f),flesh,PrimitiveType.Capsule).localRotation=Quaternion.Euler(0,0,side*72);
            }
            head=new GameObject("Elongated creature face").transform;head.SetParent(torso,false);head.localPosition=new Vector3(0,.41f,.14f);
            Part(head,"Cranium",Vector3.zero,new Vector3(.26f,.33f,.27f),flesh,PrimitiveType.Sphere);
            Part(head,"Hollow jaw",new Vector3(0,-.13f,.055f),new Vector3(.17f,.19f,.20f),flesh,PrimitiveType.Sphere);
            Part(head,"Open dark mouth",new Vector3(0,-.14f,.154f),new Vector3(.095f,.11f,.022f),shadow,PrimitiveType.Sphere);
            foreach(float side in new[] { -.065f,.065f })
            {
                Part(head,"Deep eye socket",new Vector3(side,.018f,.122f),new Vector3(.092f,.066f,.045f),shadow,PrimitiveType.Sphere);
                Part(head,"Small pale eye",new Vector3(side,.018f,.148f),new Vector3(.026f,.02f,.012f),eyes,PrimitiveType.Sphere);
            }
            leftArm=Pivot("Long left arm",new Vector3(-.28f,1.38f,.04f));rightArm=Pivot("Long right arm",new Vector3(.28f,1.35f,.06f));
            foreach(var arm in new[] {leftArm,rightArm})
            {
                Part(arm,"Bony shoulder",Vector3.zero,new Vector3(.19f,.19f,.21f),flesh,PrimitiveType.Sphere);
                Part(arm,"Upper arm",new Vector3(0,-.22f,.015f),new Vector3(.11f,.23f,.11f),flesh,PrimitiveType.Capsule);
                Part(arm,"Elbow",new Vector3(0,-.42f,.035f),Vector3.one*.13f,flesh,PrimitiveType.Sphere);
                Part(arm,"Long forearm",new Vector3(0,-.65f,.085f),new Vector3(.095f,.26f,.10f),flesh,PrimitiveType.Capsule).localRotation=Quaternion.Euler(-10,0,0);
                Part(arm,"Narrow hand",new Vector3(0,-.91f,.13f),new Vector3(.13f,.20f,.07f),flesh,PrimitiveType.Sphere);
                for(int finger=0;finger<4;finger++)Part(arm,"Hooked finger",new Vector3(-.05f+finger*.033f,-1.06f,.16f),new Vector3(.022f,.13f,.022f),bone,PrimitiveType.Capsule).localRotation=Quaternion.Euler(-22,0,0);
            }
            leftLeg = Pivot("Left leg", new Vector3(-0.15f, 0.82f, 0));
            rightLeg = Pivot("Right leg", new Vector3(0.15f, 0.82f, 0));
            for(int i=0;i<2;i++)
            {
                thighs[i]=Pivot("Thigh segment",Vector3.zero);shins[i]=Pivot("Shin segment",Vector3.zero);boots[i]=Pivot("Grounded boot",Vector3.zero);
                Part(thighs[i],"Thin thigh",Vector3.zero,new Vector3(.15f,.5f,.17f),flesh,PrimitiveType.Capsule);
                Part(shins[i],"Thin shin",Vector3.zero,new Vector3(.11f,.5f,.13f),flesh,PrimitiveType.Capsule);
                Part(boots[i],"Bare distorted foot",new Vector3(0,0,.06f),new Vector3(.17f,.16f,.30f),flesh,PrimitiveType.Sphere);
            }
            previousPosition=transform.position;
            footsteps=gameObject.AddComponent<AudioSource>();footsteps.spatialBlend=1;footsteps.minDistance=1;footsteps.maxDistance=12;footsteps.volume=.18f;
            footstepClip=HorrorAmbience.MakeClip("Guard footfall",.14f,true);
        }
        private Transform Pivot(string name, Vector3 position)
        { var pivot = new GameObject(name).transform; pivot.SetParent(transform, false); pivot.localPosition = position; return pivot; }
        private Transform Part(Transform parent, string name, Vector3 position, Vector3 size, Material material,PrimitiveType type=PrimitiveType.Cube)
        {
            var part = GameObject.CreatePrimitive(type); part.name = name;
            part.GetComponent<Collider>().enabled = false; Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part.transform;
        }
        private Material Make(Material source, string name, Color colour, float metallic)
        {
            var material = new Material(source) { name = name };
            material.SetColor("_BaseColor", colour); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", 0.12f);
            owned.Add(material); return material;
        }
        private void LateUpdate()
        {
            if (leftLeg == null) return;
            if (ai == null) ai = GetComponent<EnemyAI>();
            Vector3 travel=transform.position-previousPosition;travel.y=0;previousPosition=transform.position;
            if(travel.magnitude>1f) { feetReady=false;travel=Vector3.zero; }
            float speed=Time.deltaTime>0?travel.magnitude/Time.deltaTime:0;
            phase += travel.magnitude/.85f*Mathf.PI*2;
            float swing = Mathf.Sin(phase) * Mathf.Clamp01(speed) * 19f;
            StepFeet(speed,travel);
            leftArm.localRotation = Quaternion.Euler(-swing * 0.7f, 0, 0);
            bool attack = ai != null && (ai.State == "Attack windup" || ai.State == "Lunging" || ai.State == "Attacking");
            bool combat = ai != null && ai.CombatStance && ai.State != "Stopped";
            float blend = 1f - Mathf.Exp(-10f * Time.deltaTime);
            float weightShift=Mathf.Sin(phase*2)*.022f*Mathf.Clamp01(speed);
            torso.localPosition = Vector3.Lerp(torso.localPosition, new Vector3(Mathf.Sin(phase)*.018f, (combat ? 1.06f : 1.12f)+weightShift, .035f), blend);
            torso.localRotation = Quaternion.Slerp(torso.localRotation, Quaternion.Euler(combat ? 22f : 16f, 0, combat ? ai.DodgeLean : 0), blend);
            head.localRotation=Quaternion.Euler(-12,Mathf.Sin(Time.time*.65f)*4,combat?-5:7);
            leftArm.localRotation = Quaternion.Euler(combat ? -35f - swing * 0.3f : -swing * 0.7f, 0, combat ? 12f : 0);
            Quaternion pose = Quaternion.Euler(attack ? -65f : combat ? -30f + swing * 0.4f : swing * 0.7f, 0, attack ? -15f : 0);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, pose, 1f - Mathf.Exp(-12f * Time.deltaTime));
            if(ai!=null)
            {
                float hit=ai.HitReaction;
                if(hit>0)
                {
                    torso.localRotation=Quaternion.Euler(16-hit*24,0,hit*12);
                    head.localRotation=Quaternion.Euler(-12-hit*20,0,hit*15);
                    leftArm.localRotation=Quaternion.Euler(-25,0,-hit*30);rightArm.localRotation=Quaternion.Euler(-15,0,hit*35);
                }
                else if(ai.IsWindingUp)
                {
                    rightArm.localRotation=Quaternion.Slerp(rightArm.localRotation,Quaternion.Euler(-110,-35,-35),blend);
                    leftArm.localRotation=Quaternion.Slerp(leftArm.localRotation,Quaternion.Euler(-45,0,25),blend);
                    torso.localRotation=Quaternion.Slerp(torso.localRotation,Quaternion.Euler(15,-22,0),blend);
                }
                else if(ai.State=="Claw strike" || ai.State=="Lunging")
                {
                    float t=ai.AttackSwing;
                    rightArm.localRotation=Quaternion.Euler(Mathf.Lerp(-110,-20,t),Mathf.Lerp(-35,45,t),Mathf.Lerp(-35,30,t));
                    torso.localRotation=Quaternion.Euler(26,Mathf.Lerp(-22,24,t),0);
                }
                else if(ai.IsRecovering)
                {
                    rightArm.localRotation=Quaternion.Slerp(rightArm.localRotation,Quaternion.Euler(-15,35,25),blend);
                    torso.localRotation=Quaternion.Slerp(torso.localRotation,Quaternion.Euler(24,12,0),blend);
                }
                else if(ai.State=="Dodging")
                {
                    torso.localPosition=new Vector3(torso.localPosition.x,1.01f+weightShift,torso.localPosition.z);
                    torso.localRotation=Quaternion.Euler(28,0,ai.DodgeLean);
                    leftArm.localRotation=Quaternion.Euler(-55,0,-20);rightArm.localRotation=Quaternion.Euler(-50,0,20);
                }
            }
        }
        private Vector3 Ground(Vector3 point)
        {
            // Ray origin is relative to the agent floor, never the hip or last foot.
            // A wall, crate or ceiling cannot become the new foot height.
            float floor=transform.position.y;
            int count=Physics.RaycastNonAlloc(new Vector3(point.x,floor+.40f,point.z),Vector3.down,groundHits,.65f,1<<0,QueryTriggerInteraction.Ignore);
            float best=float.NegativeInfinity;
            for(int i=0;i<count;i++)
                if(groundHits[i].normal.y>.7f && Mathf.Abs(groundHits[i].point.y-floor)<.18f)best=Mathf.Max(best,groundHits[i].point.y);
            point.y=(float.IsNegativeInfinity(best)?floor:best)+.09f;
            return point;
        }
        private void StepFeet(float speed,Vector3 travel)
        {
            if(!feetReady)
            { for(int i=0;i<2;i++)planted[i]=Ground(transform.TransformPoint(new Vector3(i==0?-.15f:.15f,0,.10f)));feetReady=true; }
            for(int i=0;i<2;i++)
            {
                Vector3 hip=transform.TransformPoint(new Vector3(i==0?-.15f:.15f,.82f,0));
                float cycle=Mathf.Repeat(phase/(Mathf.PI*2)+i*.5f,1);
                bool lift=speed>.08f && cycle>.60f;
                if(speed<=.08f && !swinging[i] && (planted[i]-Ground(hip)).sqrMagnitude>.10f)lift=true;
                if(lift&&!swinging[i])
                {
                    swingStart[i]=planted[i];Vector3 direction=travel.sqrMagnitude>.00001f?travel.normalized:transform.forward;
                    landing[i]=Ground(hip+direction*(speed>.08f?.48f:0));
                }
                if(lift)
                {
                    float t=Mathf.Clamp01((cycle-.60f)/.40f);if(speed<=.08f)t=1;
                    planted[i]=Vector3.Lerp(swingStart[i],landing[i],Mathf.SmoothStep(0,1,t))+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.13f);
                }
                else if(swinging[i])
                { planted[i]=landing[i];if(speed>.08f)footsteps.PlayOneShot(footstepClip,Random.Range(.8f,1f)); }
                swinging[i]=lift;
                CreatureLegSolver.Solve(hip,planted[i],transform.forward,out Vector3 ankle,out Vector3 knee);
                planted[i]=ankle;
                Segment(thighs[i],hip,knee);Segment(shins[i],knee,ankle);
                boots[i].position=ankle;boots[i].rotation=Quaternion.Slerp(boots[i].rotation,transform.rotation,Time.deltaTime*10);
            }
        }
        private static void Segment(Transform segment,Vector3 start,Vector3 end)
        {
            Vector3 delta=end-start;
            segment.position=(start+end)*.5f;
            if(delta.sqrMagnitude>.0001f)segment.rotation=Quaternion.FromToRotation(Vector3.up,delta);
            segment.localScale=new Vector3(1,Mathf.Clamp(delta.magnitude,.12f,.44f),1);
        }
        private void OnDestroy() { foreach (var material in owned) Destroy(material);if(footstepClip!=null)Destroy(footstepClip); }
    }
}
