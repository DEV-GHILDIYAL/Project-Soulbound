using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace Soulbound
{
    public static class RuntimeUI
    {
        public static readonly Color Gold = new Color(0.9f, 0.77f, 0.5f);
        public static readonly Color Dark = new Color(0.04f, 0.055f, 0.065f, 0.94f);
        public static GameObject Canvas(string name, int order)
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("UI Event System", typeof(EventSystem));
                var input = events.AddComponent<InputSystemUIInputModule>(); input.AssignDefaultActions();
                // Controller navigation is handled explicitly by the existing menu logic.
                input.move = null; input.submit = null; input.cancel = null;
            }
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            return root;
        }
        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        public static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = Rect(parent, name, anchor, anchor, position, size); rect.gameObject.AddComponent<Image>().color = Dark; return rect;
        }
        public static Text Label(Transform parent, string value, float x, float y, float width, float height, int size = 16)
        {
            var rect = Rect(parent, "Text", new Vector2(0,1), new Vector2(0,1), new Vector2(x,-y), new Vector2(width,height));
            var text = rect.gameObject.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value; text.fontSize = size; text.color = Color.white; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.raycastTarget = false; return text;
        }
        public static Button Button(Transform parent, string value, float x, float y, float width, float height, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(parent, value, new Vector2(0,1), new Vector2(0,1), new Vector2(x,-y), new Vector2(width,height));
            rect.gameObject.AddComponent<Image>().color = new Color(0.17f,0.2f,0.22f);
            var button = rect.gameObject.AddComponent<Button>(); button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(action); Label(rect, value, 8, 0, width - 16, height, 15).alignment = TextAnchor.MiddleCenter; return button;
        }
    }
}
