using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace Soulbound
{
    // Procedural first-person pistol placeholder; replace with authored arms/weapon art later.
    public sealed class PistolViewModel : MonoBehaviour
    {
        private Pistol pistol;
        private Transform model, slide, flash, magazine;
        private readonly List<Material> materials = new List<Material>();
        private Camera main, overlay;
        private UniversalAdditionalCameraData data;
        private float recoil, firedAt = -10f;
        private int originalMask;
        public void Configure(Camera view, Pistol weapon, Material template)
        {
            pistol = weapon; main = view; originalMask = view.cullingMask;
            Material metal = MakeMaterial(template, "Pistol gunmetal", new Color(0.18f, 0.20f, 0.23f), 0.7f);
            Material grip = MakeMaterial(template, "Pistol grip", new Color(0.055f, 0.045f, 0.035f), 0.05f);
            Material detail = MakeMaterial(template, "Pistol accents", new Color(0.42f, 0.44f, 0.46f), 0.8f);
            Material ember = MakeMaterial(template, "Muzzle flash", new Color(1f, 0.75f, 0.2f), 0);
            ember.EnableKeyword("_EMISSION"); ember.SetColor("_EmissionColor", new Color(5, 2.5f, 0.4f));
            model = new GameObject("Pistol Model").transform;
            model.SetParent(view.transform, false); model.localPosition = new Vector3(0.20f, -0.18f, 0.48f);
            Part("Frame", new Vector3(0, -0.025f, 0), new Vector3(0.065f, 0.065f, 0.21f), metal);
            slide = Part("Slide", new Vector3(0, 0.024f, 0.025f), new Vector3(0.068f, 0.055f, 0.25f), metal);
            Part("Grip", new Vector3(0, -0.105f, -0.055f), new Vector3(0.06f, 0.13f, 0.065f), grip).localRotation = Quaternion.Euler(-15, 0, 0);
            Part("Muzzle", new Vector3(0, 0.025f, 0.156f), new Vector3(0.039f, 0.038f, 0.024f), grip);
            Part("Round barrel",new Vector3(0,.025f,.172f),new Vector3(.032f,.015f,.032f),detail,PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90,0,0);
            Part("Barrel bore",new Vector3(0,.025f,.189f),new Vector3(.020f,.002f,.020f),grip,PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90,0,0);
            Part("Front Sight", new Vector3(0, 0.06f, 0.12f), new Vector3(0.012f, 0.012f, 0.019f), detail);
            Part("Rear Sight", new Vector3(0, 0.06f, -0.075f), new Vector3(0.045f, 0.012f, 0.018f), detail);
            Part("Trigger Guard Bottom", new Vector3(0, -0.082f, 0.035f), new Vector3(0.046f, 0.012f, 0.09f), metal);
            Part("Trigger Guard Front", new Vector3(0, -0.056f, 0.076f), new Vector3(0.045f, 0.055f, 0.012f), metal);
            Part("Trigger", new Vector3(0, -0.05f, 0.014f), new Vector3(0.012f, 0.035f, 0.015f), detail);
            magazine = Part("Magazine", new Vector3(0,-0.165f,-0.07f), new Vector3(0.05f,0.035f,0.07f), metal);
            Part("Ejection port",new Vector3(0.035f,0.033f,0.025f),new Vector3(0.003f,0.024f,0.04f),grip);
            Part("Slide rail",new Vector3(0,-0.005f,0.035f),new Vector3(0.071f,0.013f,0.20f),detail);
            for (int side = -1; side <= 1; side += 2) for (int i = 0; i < 5; i++)
                Part("Slide Serration", new Vector3(side * 0.035f, 0.024f, -0.06f + i * 0.012f), new Vector3(0.002f, 0.039f, 0.005f), grip);
            flash = Part("Muzzle Flash", new Vector3(0, 0.025f, 0.21f), new Vector3(0.025f, 0.025f, 0.07f), ember);
            flash.gameObject.SetActive(false);
            data = view.GetComponent<UniversalAdditionalCameraData>();
            if (data != null && data.renderType == CameraRenderType.Base)
            {
                var cameraObject = new GameObject("Weapon Overlay Camera"); cameraObject.layer = 2;
                cameraObject.transform.SetParent(view.transform, false);
                overlay = cameraObject.AddComponent<Camera>(); overlay.fieldOfView = view.fieldOfView;
                overlay.nearClipPlane = 0.01f; overlay.farClipPlane = 3f; overlay.cullingMask = 1 << 2;
                var overlayData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
                // This URP version defaults new overlay cameras to clear depth (read-only API).
                overlayData.renderType = CameraRenderType.Overlay;
                view.cullingMask &= ~(1 << 2); data.cameraStack.Add(overlay);
            }
            pistol.Fired += Fire;
        }
        private void Fire() { recoil = 1f; firedAt = Time.time; }
        private void LateUpdate()
        {
            if (model == null) return;
            recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * 7f);
            model.localPosition = new Vector3(0.20f, -0.18f, 0.48f - recoil * 0.045f);
            model.localRotation = Quaternion.Euler(-recoil * 9f, 0, 0);
            if (pistol.Reloading)
            {
                model.localRotation = Quaternion.Euler(12, -12, -22);
                model.localPosition += new Vector3(0,-0.05f,0);
                magazine.localPosition = new Vector3(0,-0.22f - Mathf.Abs(Mathf.Sin(Time.time * 5)) * 0.07f,-0.07f);
            }
            else magazine.localPosition = new Vector3(0,-0.165f,-0.07f);
            slide.localPosition = new Vector3(0, 0.024f, 0.025f - recoil * 0.035f);
            flash.gameObject.SetActive(Time.time - firedAt < 0.045f);
        }
        private Transform Part(string name, Vector3 position, Vector3 scale, Material material, PrimitiveType shape = PrimitiveType.Cube)
        {
            var piece = GameObject.CreatePrimitive(shape); piece.name = name; piece.layer = 2;
            piece.GetComponent<Collider>().enabled = false; Destroy(piece.GetComponent<Collider>());
            piece.transform.SetParent(model, false); piece.transform.localPosition = position; piece.transform.localScale = scale;
            var renderer = piece.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return piece.transform;
        }
        private Material MakeMaterial(Material template, string name, Color color, float metallic)
        {
            var material = new Material(template) { name = name };
            material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", 0.45f);
            materials.Add(material); return material;
        }
        private void OnDestroy()
        {
            if (pistol != null) pistol.Fired -= Fire;
            if (data != null && overlay != null) data.cameraStack.Remove(overlay);
            if (main != null) main.cullingMask = originalMask;
            if (model != null) Destroy(model.gameObject);
            if (overlay != null) Destroy(overlay.gameObject);
            foreach (var material in materials) Destroy(material);
        }
    }
}
