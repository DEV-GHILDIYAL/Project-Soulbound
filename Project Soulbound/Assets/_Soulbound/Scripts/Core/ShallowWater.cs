using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Soulbound
{
    public sealed class ShallowWater : MonoBehaviour
    {
        private readonly List<Vector3> patches = new List<Vector3>();
        private Material material;
        private Texture2D colour, normal;
        private readonly List<Texture2D> rippleFrames = new List<Texture2D>();
        private Mesh plane;
        private float size;
        public void Build(Material template,float cellSize)
        {
            size=cellSize-.30f;
            material=new Material(template) { name="Shallow catacomb water",renderQueue=(int)RenderQueue.Transparent };
            material.SetColor("_BaseColor",new Color(.11f,.19f,.16f,.38f));
            material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_ZWrite",0);
            material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_SrcBlendAlpha",(int)BlendMode.One);material.SetInt("_DstBlendAlpha",(int)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_Metallic",.12f);material.SetFloat("_Smoothness",.87f);material.SetFloat("_BumpScale",.32f);
            material.SetOverrideTag("RenderType","Transparent");material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.EnableKeyword("_NORMALMAP");
            material.DisableKeyword("_ALPHATEST_ON");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");material.DisableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);
            material.SetShaderPassEnabled("ShadowCaster",false);
            const int resolution=128;var pixels=new Color[resolution*resolution];var bumps=new Color[pixels.Length];
            for(int y=0;y<resolution;y++)for(int x=0;x<resolution;x++)
            {
                float u=(float)x/resolution,v=(float)y/resolution;
                float edge=Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v));
                float erosion=Mathf.PerlinNoise(u*9,v*9)*.045f;
                float alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01((edge-erosion)*12));
                pixels[y*resolution+x]=new Color(.85f,.92f,.88f,alpha);
                float dx=Mathf.Cos(u*Mathf.PI*12+v*Mathf.PI*4)*.20f+Mathf.Cos(v*Mathf.PI*8-u*Mathf.PI*6)*.08f;
                float dz=Mathf.Cos(u*Mathf.PI*12+v*Mathf.PI*4)*.07f+Mathf.Cos(v*Mathf.PI*8-u*Mathf.PI*6)*.16f;
                Vector3 n=new Vector3(-dx,-dz,1).normalized;bumps[y*resolution+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
            }
            colour=new Texture2D(resolution,resolution,TextureFormat.RGBA32,true,false) { name="Water soft wet edges",wrapMode=TextureWrapMode.Clamp };
            normal=new Texture2D(resolution,resolution,TextureFormat.RGBA32,true,true) { name="Water ripple normals",wrapMode=TextureWrapMode.Repeat };
            colour.SetPixels(pixels);colour.Apply(true,true);normal.SetPixels(bumps);normal.Apply(true,true);
            // URP Lit samples its bump map with base UVs, so separate bump offsets
            // have no effect. Cycle precomputed normal frames without moving edges.
            for(int frame=0;frame<32;frame++)
            {
                const int nsize=64;var data=new Color[nsize*nsize];float phase=frame*Mathf.PI*2/32;
                for(int y=0;y<nsize;y++)for(int x=0;x<nsize;x++)
                {
                    float u=(float)x/nsize,v=(float)y/nsize;
                    float a=Mathf.Cos(u*Mathf.PI*12+v*Mathf.PI*4+phase),b=Mathf.Cos(v*Mathf.PI*8-u*Mathf.PI*6-phase);
                    Vector3 n=new Vector3(-a*.20f-b*.08f,-a*.07f-b*.16f,1).normalized;
                    data[y*nsize+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
                }
                var texture=new Texture2D(nsize,nsize,TextureFormat.RGBA32,true,true) { name="Ripple frame "+frame,wrapMode=TextureWrapMode.Repeat };
                texture.SetPixels(data);texture.Apply(true,true);rippleFrames.Add(texture);
            }
            material.SetTexture("_BaseMap",colour);material.SetTexture("_BumpMap",normal);
            plane=new Mesh { name="Shallow water surface" };
            plane.vertices=new[] { new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f) };
            plane.uv=new[] { Vector2.zero,Vector2.up,Vector2.one,Vector2.right };plane.triangles=new[] { 0,1,2,0,2,3 };plane.RecalculateNormals();plane.RecalculateTangents();plane.RecalculateBounds();
        }
        public void Patch(Transform parent,Vector3 center)
        {
            patches.Add(center);var surface=new GameObject("Shallow water — visible stone underneath",typeof(MeshFilter),typeof(MeshRenderer));
            surface.transform.SetParent(parent,false);surface.transform.position=center+Vector3.up*.055f;surface.transform.localScale=new Vector3(size,1,size);
            surface.GetComponent<MeshFilter>().sharedMesh=plane;var renderer=surface.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        public bool Contains(Vector3 point)
        { foreach(var c in patches)if(Mathf.Abs(point.x-c.x)<size*.43f && Mathf.Abs(point.z-c.z)<size*.43f)return true;return false; }
        private void Update()
        {
            if(material!=null && rippleFrames.Count>0)material.SetTexture("_BumpMap",rippleFrames[Mathf.FloorToInt(Time.time*8)%rippleFrames.Count]);
        }
        private void OnDestroy() { if(material!=null)Destroy(material);if(colour!=null)Destroy(colour);if(normal!=null)Destroy(normal);if(plane!=null)Destroy(plane);foreach(var frame in rippleFrames)Destroy(frame); }
    }
}
