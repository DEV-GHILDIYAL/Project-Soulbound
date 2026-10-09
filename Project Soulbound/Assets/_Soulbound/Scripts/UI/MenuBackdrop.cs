using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Soulbound
{
    public sealed class MenuBackdrop : MonoBehaviour
    {
        private readonly List<Material> owned=new List<Material>();
        private Transform soul;
        private Light glow;
        public void Build()
        {
            RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.045f,.065f,.075f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.065f;RenderSettings.fogColor=new Color(.013f,.022f,.028f);
            var camera=new GameObject("Menu Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(-1,1.7f,-6);camera.transform.rotation=Quaternion.Euler(2,9,0);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RenderSettings.fogColor;
            Material stone=Mat("Menu damp stone",new Color(.15f,.20f,.20f)),dark=Mat("Menu shadow stone",new Color(.055f,.07f,.075f)),iron=Mat("Menu iron",new Color(.13f,.12f,.09f));
            Box("Floor",new Vector3(0,-.15f,3),new Vector3(12,.3f,22),dark);
            for(int side=-1;side<=1;side+=2)for(int z=0;z<8;z++)for(int row=0;row<5;row++)
                Box("Stone course",new Vector3(side*4,row*.66f+.33f,z*2-3),new Vector3(.4f,.62f,1.96f),row%2==0?stone:dark);
            for(int z=0;z<4;z++)
            {
                float depth=z*3+1;
                for(int side=-1;side<=1;side+=2)Box("Ruined arch column",new Vector3(side*2.65f,1.65f,depth),new Vector3(.55f,3.3f,.6f),stone);
                Box("Arch lintel",new Vector3(0,3.2f,depth),new Vector3(5.8f,.4f,.6f),dark);
            }
            for(int i=0;i<7;i++)Box("Sealed gate",new Vector3(1.7f+(i-3)*.31f,1.35f,7),new Vector3(.27f,2.7f,.2f),iron);
            var orbMaterial=Mat("Menu soul glow",new Color(.14f,.6f,.55f));orbMaterial.EnableKeyword("_EMISSION");orbMaterial.SetColor("_EmissionColor",new Color(.2f,1.4f,1.15f));
            var orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);orb.name="Distant wandering soul";orb.transform.position=new Vector3(2.05f,1.55f,2.7f);orb.transform.localScale=new Vector3(.24f,.36f,.24f);orb.GetComponent<Renderer>().sharedMaterial=orbMaterial;Destroy(orb.GetComponent<Collider>());soul=orb.transform;
            glow=orb.AddComponent<Light>();glow.type=LightType.Point;glow.color=new Color(.16f,.65f,.56f);glow.range=6;glow.intensity=3;
            var light=new GameObject("Cold gate light").AddComponent<Light>();light.type=LightType.Point;light.transform.position=new Vector3(2,2,5);light.range=8;light.intensity=2;light.color=new Color(.22f,.35f,.42f);
        }
        private Material Mat(string name,Color color)
        { var material=new Material(Shader.Find("Universal Render Pipeline/Lit")) { name=name };material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.2f);owned.Add(material);return material; }
        private void Box(string name,Vector3 position,Vector3 scale,Material material)
        { var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.position=position;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=material;Destroy(obj.GetComponent<Collider>()); }
        private void Update() { if(soul!=null)soul.position=new Vector3(2.05f+Mathf.Sin(Time.unscaledTime*.2f)*.2f,1.55f+Mathf.Sin(Time.unscaledTime*.8f)*.10f,2.7f);if(glow!=null)glow.intensity=2.8f+Mathf.Sin(Time.unscaledTime*1.3f)*.4f; }
        private void OnDestroy() { foreach(var material in owned)Destroy(material); }
    }
}
