using UnityEngine;
namespace Soulbound
{
    // A damped suspension reacts to character contact without unbounded rigidbody forces.
    public sealed class HangingBodyBag : MonoBehaviour
    {
        private Transform player;
        private CharacterController character;
        private Quaternion rest;
        private Vector2 angle, velocity;
        private Mesh mesh;
        private Material bagMaterial;
        private int number;
        public void Build(Material cloth,Material metal,Material tag,Transform target,int index)
        {
            player=target;character=target.GetComponent<CharacterController>();rest=transform.localRotation;number=index;
            // Moving props are excluded from the static layer-zero NavMesh bake.
            gameObject.layer=1;
            var rigidbody=gameObject.AddComponent<Rigidbody>();rigidbody.isKinematic=true;rigidbody.useGravity=false;
            rigidbody.interpolation=RigidbodyInterpolation.Interpolate;
            var collider=gameObject.AddComponent<CapsuleCollider>();collider.center=new Vector3(0,-1.06f,0);collider.height=1.9f;collider.radius=.30f;
            var contact=new GameObject("Contact sensor");contact.transform.SetParent(transform,false);contact.layer=1;
            var sensor=contact.AddComponent<CapsuleCollider>();sensor.center=collider.center;sensor.height=2f;sensor.radius=.48f;sensor.isTrigger=true;
            bagMaterial=new Material(cloth);bagMaterial.name="Worn mortuary canvas";bagMaterial.SetColor("_BaseColor",new Color(.19f,.21f,.16f));bagMaterial.SetFloat("_Smoothness",.22f);
            mesh=BagMesh();var model=new GameObject("Closed canvas body bag",typeof(MeshFilter),typeof(MeshRenderer));model.transform.SetParent(transform,false);model.layer=1;
            model.GetComponent<MeshFilter>().sharedMesh=mesh;model.GetComponent<MeshRenderer>().sharedMaterial=bagMaterial;
            // Rope, two ankle straps, reinforced seams and an uninterrupted front zipper.
            Detail("Suspension",new Vector3(0,-.08f,0),new Vector3(.04f,.16f,.04f),metal);
            Detail("Ankle restraint",new Vector3(0,-.28f,0),new Vector3(.36f,.055f,.24f),metal);
            Detail("Lower restraint",new Vector3(0,-.43f,0),new Vector3(.42f,.055f,.27f),metal);
            for(int i=0;i<25;i++)
            {
                float y=-.36f-i*.059f;float radius=Radius(-y);
                Detail("Zipper tooth",new Vector3((i%2==0?1:-1)*.012f,y,-radius*.58f-.009f),new Vector3(.035f,.021f,.018f),metal);
            }
            Detail("Zip pull",new Vector3(0,-1.79f,-.15f),new Vector3(.055f,.07f,.022f),metal);
            var label=Detail("Mortuary tag",new Vector3(.11f,-.43f,-.18f),new Vector3(.12f,.16f,.012f),tag);
            label.transform.localRotation=Quaternion.Euler(0,0,-12);
        }
        private static float Radius(float depth)
        {
            float[] y={.16f,.35f,.65f,1f,1.35f,1.65f,1.90f,2.02f};
            float[] r={.08f,.17f,.23f,.29f,.32f,.25f,.17f,.01f};
            for(int i=1;i<y.Length;i++)if(depth<=y[i])return Mathf.Lerp(r[i-1],r[i],Mathf.InverseLerp(y[i-1],y[i],depth));
            return .01f;
        }
        private Mesh BagMesh()
        {
            const int rows=26, sides=18;var vertices=new Vector3[(rows+1)*(sides+1)];var uv=new Vector2[vertices.Length];var triangles=new int[rows*sides*6];
            for(int row=0;row<=rows;row++)for(int side=0;side<=sides;side++)
            {
                float depth=Mathf.Lerp(.16f,2.02f,(float)row/rows),theta=side*Mathf.PI*2/sides;
                float radius=(row==0 || row==rows ? 0 : Radius(depth))*(1+.035f*Mathf.Sin(theta*5+row*.8f+number));int index=row*(sides+1)+side;
                vertices[index]=new Vector3(Mathf.Cos(theta)*radius,-depth,Mathf.Sin(theta)*radius*.58f);
                uv[index]=new Vector2((float)side/sides,(float)row/rows);
                if(row<rows&&side<sides)
                { int t=(row*sides+side)*6;triangles[t]=index;triangles[t+1]=index+1;triangles[t+2]=index+sides+1;triangles[t+3]=index+1;triangles[t+4]=index+sides+2;triangles[t+5]=index+sides+1; }
            }
            var result=new Mesh { name="Tapered hanging body bag",vertices=vertices,uv=uv,triangles=triangles };result.RecalculateNormals();result.RecalculateBounds();return result;
        }
        private GameObject Detail(string name,Vector3 position,Vector3 scale,Material material)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.layer=1;obj.transform.SetParent(transform,false);obj.transform.localPosition=position;obj.transform.localScale=scale;
            obj.GetComponent<Collider>().enabled=false;Destroy(obj.GetComponent<Collider>());obj.GetComponent<Renderer>().sharedMaterial=material;return obj;
        }
        private void OnTriggerStay(Collider other)
        {
            if(player==null || character==null || !(other.transform==player || other.transform.IsChildOf(player)))return;
            Vector3 motion=character.velocity;motion.y=0;if(motion.sqrMagnitude<.03f)return;
            Vector3 local=Quaternion.Inverse(transform.parent.rotation*rest)*motion;
            velocity+=new Vector2(-local.z,local.x)*Time.fixedDeltaTime*35f;
            velocity=Vector2.ClampMagnitude(velocity,55f);
        }
        private void FixedUpdate()
        {
            velocity+=(-angle*9f-velocity*2.8f)*Time.fixedDeltaTime;angle+=velocity*Time.fixedDeltaTime;angle=Vector2.ClampMagnitude(angle,11f);
            GetComponent<Rigidbody>().MoveRotation(transform.parent.rotation*rest*Quaternion.Euler(angle.x,0,angle.y));
        }
        private void OnDestroy() { if(mesh!=null)Destroy(mesh);if(bagMaterial!=null)Destroy(bagMaterial); }
    }
}
