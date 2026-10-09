using UnityEngine;
using UnityEngine.InputSystem;
namespace Soulbound
{
    public static class PlayerControls
    {
        private static int frame = -1;
        private static Vector2 move, look;
        private static bool interact, hold, shoot, heal, cancel, menuConfirm, menuToggle;
        private static int step;
        private static float nextStep;
        private static int previousDirection;
        public static bool UsingGamepad { get; private set; }
        private static Vector2 Deadzone(Vector2 value)
        {
            float magnitude = value.magnitude;
            return magnitude <= 0.15f ? Vector2.zero : value.normalized * Mathf.Clamp01((magnitude - 0.15f) / 0.85f);
        }
        private static void Read()
        {
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            var keyboard = Keyboard.current; var mouse = Mouse.current; var pad = Gamepad.current;
            Vector2 keys = Vector2.zero;
            if (keyboard != null)
                keys = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0), (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            Vector2 left = pad == null ? Vector2.zero : Deadzone(pad.leftStick.ReadValue());
            Vector2 right = pad == null ? Vector2.zero : Deadzone(pad.rightStick.ReadValue());
            if (pad == null) UsingGamepad = false;
            else if (left.sqrMagnitude > 0 || right.sqrMagnitude > 0 || pad.buttonSouth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame || pad.dpad.ReadValue().sqrMagnitude > 0 || pad.startButton.wasPressedThisFrame) UsingGamepad = true;
            if ((keyboard != null && keyboard.anyKey.wasPressedThisFrame) || (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 1 || mouse.leftButton.wasPressedThisFrame))) UsingGamepad = false;
            if(pad!=null&&(pad.leftStickButton.wasPressedThisFrame||pad.rightStickButton.wasPressedThisFrame))UsingGamepad=true;
            move = Vector2.ClampMagnitude(keys + left, 1f);
            look = right;
            interact = (keyboard != null && keyboard.fKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            hold = (keyboard != null && keyboard.fKey.isPressed) || (pad != null && pad.buttonSouth.isPressed);
            shoot = (mouse != null && mouse.leftButton.wasPressedThisFrame) || (pad != null && pad.rightTrigger.wasPressedThisFrame);
            heal = (keyboard != null && keyboard.hKey.wasPressedThisFrame) || (pad != null && pad.buttonWest.wasPressedThisFrame);
            cancel = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame);
            menuToggle = pad != null && pad.startButton.wasPressedThisFrame;
            menuConfirm = (keyboard != null && keyboard.enterKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            int direction = 0;
            if (keyboard != null)
            {
                if (keyboard.rightArrowKey.isPressed || keyboard.downArrowKey.isPressed) direction = 1;
                if (keyboard.leftArrowKey.isPressed || keyboard.upArrowKey.isPressed) direction = -1;
            }
            if (pad != null)
            {
                Vector2 navigation = pad.dpad.ReadValue() + left;
                if (navigation.x > 0.5f || navigation.y < -0.5f) direction = 1;
                if (navigation.x < -0.5f || navigation.y > 0.5f) direction = -1;
            }
            step = 0;
            if (direction != 0 && (direction != previousDirection || Time.unscaledTime >= nextStep))
            { step = direction; nextStep = Time.unscaledTime + 0.22f; }
            previousDirection = direction;
        }
        public static Vector2 Move { get { Read(); return move; } }
        public static Vector2 StickLook { get { Read(); return look; } }
        public static bool InteractPressed { get { Read(); return interact; } }
        public static bool InteractHeld { get { Read(); return hold; } }
        public static bool ShootPressed { get { Read(); return shoot; } }
        public static bool ReloadPressed => (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
        public static bool HealPressed { get { Read(); return heal; } }
        public static bool CancelPressed { get { Read(); return cancel; } }
        public static bool MenuTogglePressed { get { Read(); return menuToggle; } }
        public static bool ConfirmPressed { get { Read(); return menuConfirm; } }
        public static int MenuStep { get { Read(); return step; } }
        public static float Peek
        {
            get
            {
                var key=Keyboard.current;var pad=Gamepad.current;
                bool left=(key!=null&&key.qKey.isPressed)||(pad!=null&&pad.leftStickButton.isPressed);
                bool right=(key!=null&&key.eKey.isPressed)||(pad!=null&&pad.rightStickButton.isPressed);
                return (right?1f:0)-(left?1f:0);
            }
        }
        public static string InteractLabel { get { Read(); return UsingGamepad ? "A / Cross" : "F"; } }
    }
}
