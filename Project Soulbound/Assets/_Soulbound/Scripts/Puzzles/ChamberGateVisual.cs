using System.Collections.Generic;
using UnityEngine;
namespace Soulbound
{
    public sealed class ChamberGateVisual : MonoBehaviour
    {
        private readonly List<Material> owned = new List<Material>();
        public void Build(Material template, int kind)
        {
            GetComponent<Renderer>().enabled = false;
            var timber = Make(template, "Gate oak", new Color(0.24f, 0.13f, 0.065f), 0);
            var iron = Make(template, "Gate iron", new Color(0.15f, 0.18f, 0.20f), 0.65f);
            var bronze = Make(template, "Gate bronze", new Color(0.57f, 0.38f, 0.16f), 0.6f);
            Color colour = kind == 0 ? new Color(0.3f, 0.7f, 0.85f) : kind == 1 ? new Color(1f, 0.45f, 0.12f) : new Color(0.65f, 0.4f, 0.9f);
            var seal = Make(template, "Chamber seal", colour, 0);
            seal.EnableKeyword("_EMISSION"); seal.SetColor("_EmissionColor", colour * 1.5f);
            float width = transform.localScale.x, height = transform.localScale.y;
            for (int i = 0; i < 10; i++)
                Part("Oak plank", new Vector3(-width / 2 + (i + 0.5f) * width / 10, 0, 0), new Vector3(width / 10 - 0.015f, height - 0.07f, 0.19f), timber);
            for (int side = -1; side <= 1; side += 2)
            {
                foreach (float y in new[] { -0.92f, 0f, 0.94f })
                {
                    Part("Iron cross strap", new Vector3(0, y, side * 0.13f), new Vector3(width - 0.06f, 0.12f, 0.07f), iron);
                    for (int rivet = 0; rivet < 7; rivet++)
                        Part("Bronze rivet", new Vector3(-width / 2 + 0.18f + rivet * (width - 0.36f) / 6, y, side * 0.175f), new Vector3(0.045f, 0.045f, 0.022f), bronze);
                }
                for (int x = -1; x <= 1; x += 2)
                    Part("Door border", new Vector3(x * (width / 2 - 0.07f), 0, side * 0.12f), new Vector3(0.12f, height, 0.09f), iron);
                Part("Lock plate", new Vector3(0, -0.1f, side * 0.20f), new Vector3(0.46f, 0.55f, 0.10f), bronze);
                Part("Rune seal", new Vector3(0, -0.1f, side * 0.26f), new Vector3(0.19f, 0.19f, 0.035f), seal, 45);
            }
        }
        private Material Make(Material source, string name, Color colour, float metallic)
        {
            var material = new Material(source) { name = name };
            material.SetColor("_BaseColor", colour); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", 0.25f);
            owned.Add(material); return material;
        }
        private void Part(string name, Vector3 position, Vector3 size, Material material, float angle = 0)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube); piece.name = name; piece.layer = 1;
            piece.GetComponent<Collider>().enabled = false; Destroy(piece.GetComponent<Collider>());
            piece.transform.SetParent(transform, false);
            // Gate root is scaled for its collider. Compensate so visible parts keep metre dimensions.
            Vector3 parentScale = transform.localScale;
            piece.transform.localPosition = new Vector3(position.x / parentScale.x, position.y / parentScale.y, position.z / parentScale.z);
            piece.transform.localScale = new Vector3(size.x / parentScale.x, size.y / parentScale.y, size.z / parentScale.z);
            if (angle != 0) piece.transform.localRotation = Quaternion.Euler(0, 0, angle);
            piece.GetComponent<Renderer>().sharedMaterial = material;
        }
        private void OnDestroy() { foreach (var material in owned) Destroy(material); }
    }
}
