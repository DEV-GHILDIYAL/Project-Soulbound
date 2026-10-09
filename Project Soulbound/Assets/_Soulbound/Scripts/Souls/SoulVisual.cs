using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Soulbound
{
    public sealed class SoulVisual : MonoBehaviour
    {
        private readonly List<Material> owned = new List<Material>();
        private Transform spirit, halo;
        private Soul soul;
        private Material glow;
        private TrailRenderer trail;
        private readonly List<Transform> tails = new List<Transform>();
        private Transform crown;
        public void Build(Material template)
        {
            soul = GetComponent<Soul>();
            glow = Make(template, "Soul core", new Color(0.25f, 0.90f, 0.85f, 1), false);
            var mist = Make(template, "Soul aura", new Color(0.16f, 0.65f, 0.73f, 0.23f), true);
            var eyes = Make(template, "Soul eyes", new Color(0.9f, 1f, 1f, 1), false);
            spirit = new GameObject("Spirit").transform; spirit.SetParent(transform, false);
            spirit.localPosition = Vector3.up * 1.35f;
            Piece(spirit, "Soul face", new Vector3(0, 0.05f, 0), new Vector3(0.33f, 0.40f, 0.29f), glow);
            Piece(spirit, "Aura", Vector3.zero, Vector3.one * 0.64f, mist);
            foreach (float x in new[] { -0.08f, 0.08f })
                Piece(spirit, "Spirit eye", new Vector3(x, 0.11f, 0.135f), new Vector3(0.055f, 0.08f, 0.035f), eyes);
            for (int i = 0; i < 6; i++)
                tails.Add(Piece(spirit, "Wisp tail", new Vector3(0, -0.18f - i * 0.075f, 0), Vector3.one * (0.23f - i * 0.03f), mist));
            crown = new GameObject("Soul Crown").transform; crown.SetParent(spirit,false);
            for (int i = 0; i < 8; i++)
            { float angle = i*Mathf.PI/4; Piece(crown,"Crown mote",new Vector3(Mathf.Cos(angle)*.22f,.28f,Mathf.Sin(angle)*.22f),Vector3.one*.035f,eyes); }
            halo = new GameObject("Orbiting Wisps").transform; halo.SetParent(spirit, false);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI * 2 / 3;
                Piece(halo, "Orbit mote", new Vector3(Mathf.Cos(angle) * 0.29f, 0, Mathf.Sin(angle) * 0.29f), Vector3.one * 0.055f, eyes);
            }
            trail = spirit.gameObject.AddComponent<TrailRenderer>(); trail.sharedMaterial = mist;
            trail.time = 0.45f; trail.startWidth = 0.22f; trail.endWidth = 0; trail.minVertexDistance = 0.08f;
            trail.startColor = new Color(0.15f, 0.7f, 0.8f, 0.4f); trail.endColor = new Color(0.1f, 0.4f, 0.6f, 0);
        }
        private void LateUpdate()
        {
            if (spirit == null) return;
            spirit.localPosition = Vector3.up * (1.35f + Mathf.Sin(Time.time * 3.5f) * 0.055f);
            if (Camera.main != null)
            {
                Vector3 direction = Camera.main.transform.position - spirit.position; direction.y = 0;
                if (direction.sqrMagnitude > 0.01f) spirit.rotation = Quaternion.LookRotation(direction);
            }
            halo.localRotation = Quaternion.Euler(15, Time.time * 110f, 0);
            crown.localRotation = Quaternion.Euler(0,-Time.time*65f,0);
            for (int i = 0; i < tails.Count; i++) tails[i].localPosition = new Vector3(Mathf.Sin(Time.time*5f-i*.65f)*(.02f+i*.006f),-.18f-i*.075f,0);
            bool urgent = soul != null && soul.Remaining < 3f;
            Color colour = urgent ? new Color(1f, 0.42f, 0.15f) : new Color(0.25f, 0.90f, 0.85f);
            float pulse = 1.8f + Mathf.Sin(Time.time * (urgent ? 10 : 3)) * 0.4f;
            glow.SetColor("_BaseColor", colour); glow.SetColor("_EmissionColor", colour * pulse);
            trail.emitting = soul != null && !soul.Resolved;
        }
        private Transform Piece(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Sphere); part.name = name;
            part.GetComponent<Collider>().enabled = false; Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = scale;
            var renderer = part.GetComponent<Renderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
            return part.transform;
        }
        private Material Make(Material source, string name, Color colour, bool transparent)
        {
            var material = new Material(source) { name = name };
            material.SetColor("_BaseColor", colour); material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", new Color(colour.r, colour.g, colour.b) * (transparent ? 0.5f : 2f));
            if (transparent)
            {
                material.SetFloat("_Surface", 1); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_SrcBlendAlpha", (int)BlendMode.One); material.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0); material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = 3000;
            }
            owned.Add(material); return material;
        }
        private void OnDestroy() { foreach (var material in owned) Destroy(material); }
    }
}
