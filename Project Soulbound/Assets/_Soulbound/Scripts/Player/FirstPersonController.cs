using UnityEngine;
using UnityEngine.InputSystem;

namespace Soulbound
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera view;
        [SerializeField] private Health health;
        [SerializeField] private RunState run;

        [Header("Movement")]
        [SerializeField, Min(0)] private float speed = 2.8f;
        [SerializeField, Min(0.01f)] private float acceleration = 28f;
        [SerializeField, Min(0.01f)] private float braking = 45f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.25f;

        [Header("Mouse Look")]
        [SerializeField, Min(0)] private float sensitivity = 0.1f;
        [SerializeField, Min(0)] private float stickLookSpeed = 150f;
        [Tooltip("Zero gives immediate mouse look. Higher values soften changes.")]
        [SerializeField, Range(0f, 0.15f)] private float lookSmoothTime = 0.025f;
        [SerializeField, Range(45f, 89f)] private float pitchLimit = 85f;

        [Header("Camera Feel (set amounts to zero to disable)")]
        [SerializeField, Range(0f, 0.08f)] private float bobAmount = 0.045f;
        [SerializeField, Min(0f)] private float bobCyclesPerMetre = 0.36f;
        [SerializeField, Range(0f, 3f)] private float strafeRoll = 0.6f;
        [SerializeField, Min(0.01f)] private float cameraResponse = 12f;
        [Header("Hold to peek")]
        [SerializeField,Range(0,.5f)] private float peekDistance=.35f;
        [SerializeField,Range(0,20)] private float peekAngle=12f;
        [SerializeField,Min(1)] private float peekResponse=10f;
        private float peek;

        private CharacterController controller;
        private Vector3 horizontalVelocity, cameraOrigin, cameraOffset;
        private Quaternion cameraRotation;
        private Vector2 targetLook, smoothLook, lookVelocity;
        private float verticalSpeed, bobPhase, bobWeight, roll;
        private bool capturedCursor;
        public bool InputBlocked { get; private set; }
        public void SetInteractionMode(bool active) { InputBlocked = active; SetCursor(!active); }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (view == null) view = GetComponentInChildren<Camera>();
            if (health == null) health = GetComponent<Health>();
            if (view == null)
            {
                Debug.LogError("FirstPersonController needs a child camera.", this);
                enabled = false;
                return;
            }
            cameraOrigin = view.transform.localPosition;
            cameraRotation = view.transform.localRotation;
        }

        private void OnEnable() => SetCursor(true);
        private void OnDisable()
        {
            SetCursor(false);
            horizontalVelocity = Vector3.zero;
            if (view != null)
            {
                view.transform.localPosition = cameraOrigin;
                view.transform.localRotation = cameraRotation;
            }
        }

        private void SetCursor(bool locked)
        {
            capturedCursor = locked;
            if(!locked)
            {
                peek=0;
                if(view!=null) { view.transform.localPosition=cameraOrigin+cameraOffset;view.transform.localRotation=cameraRotation*Quaternion.Euler(smoothLook.y,0,roll); }
            }
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
            // Drop pending smoothing when focus changes, so reacquiring cannot jerk the view.
            targetLook = smoothLook;
            lookVelocity = Vector2.zero;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) SetCursor(false);
        }

        private void Update()
        {
            if (view == null || !controller.enabled || Time.deltaTime <= 0f) return;
            if ((health != null && !health.IsAlive) || (run != null && run.HasEnded))
            {
                horizontalVelocity = Vector3.zero;
                if (capturedCursor) SetCursor(false);
                return;
            }

            var mouse = Mouse.current;
            if (!InputBlocked && PlayerControls.MenuTogglePressed && Application.isFocused) SetCursor(!capturedCursor);
            else if (!InputBlocked && PlayerControls.CancelPressed) SetCursor(false);
            else if (!InputBlocked && mouse != null && mouse.leftButton.wasPressedThisFrame && Application.isFocused)
                SetCursor(true);

            bool canControl = !InputBlocked && capturedCursor && Cursor.lockState == CursorLockMode.Locked && Application.isFocused;
            Vector2 input = Vector2.zero;
            if (canControl)
            {
                // Mouse delta is already per-frame displacement: do not multiply by deltaTime.
                Vector2 delta = mouse == null ? Vector2.zero : mouse.delta.ReadValue() * sensitivity * MenuSettings.Look;
                delta += PlayerControls.StickLook * stickLookSpeed * Time.deltaTime * MenuSettings.Look;
                targetLook.x += delta.x;
                targetLook.y = Mathf.Clamp(targetLook.y - delta.y, -pitchLimit, pitchLimit);
                if (lookSmoothTime <= 0f) smoothLook = targetLook;
                else smoothLook = Vector2.SmoothDamp(smoothLook, targetLook, ref lookVelocity,
                    lookSmoothTime, Mathf.Infinity, Time.deltaTime);
                transform.Rotate(0f, smoothLook.x, 0f);
                // Keep yaw relative to the current root rotation and avoid long-run angle growth.
                targetLook.x -= smoothLook.x;
                smoothLook.x = 0f;
                input = PlayerControls.Move;
            }
            input = Vector2.ClampMagnitude(input, 1f);
            Vector3 desired = (transform.right * input.x + transform.forward * input.y) * speed;
            float rate = input.sqrMagnitude > 0f ? acceleration : braking;
            if (controller.isGrounded && input.sqrMagnitude > 0f)
            {
                // Feet change direction immediately; ramp speed, not sideways momentum.
                float walkingSpeed = Mathf.MoveTowards(horizontalVelocity.magnitude,
                    desired.magnitude, rate * Time.deltaTime);
                horizontalVelocity = desired.normalized * walkingSpeed;
            }
            else
            {
                if (!controller.isGrounded) rate *= airControl;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired, rate * Time.deltaTime);
            }

            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            Vector3 previousPosition = transform.position;
            CollisionFlags collisions = controller.Move((horizontalVelocity + Vector3.up * verticalSpeed) * Time.deltaTime);
            if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;

            // Use actual travel, so pushing into a wall does not produce walking bob.
            Vector3 travelled = transform.position - previousPosition;
            travelled.y = 0f;
            float distance = travelled.magnitude;
            bool walking = canControl && controller.isGrounded && distance > 0.0001f;
            float blend = 1f - Mathf.Exp(-cameraResponse * Time.deltaTime);
            float travelSpeed = distance / Time.deltaTime;
            float targetWeight = walking && speed > 0f ? Mathf.Clamp01(travelSpeed / speed) : 0f;
            bobWeight = Mathf.Lerp(bobWeight, targetWeight, blend);
            if (walking)
            {
                bobPhase = Mathf.Repeat(bobPhase + distance * bobCyclesPerMetre * Mathf.PI * 2f, Mathf.PI * 2f);
            }
            // Smooth the motion envelope rather than filtering away each footstep.
            // One lateral cycle contains two vertical steps, with the neutral pose at phase zero.
            cameraOffset = new Vector3(Mathf.Sin(bobPhase) * bobAmount * 0.55f,
                (Mathf.Cos(bobPhase * 2f) - 1f) * bobAmount * 0.5f, 0f) * bobWeight;
            if (!walking && bobWeight < 0.001f) bobPhase = 0f;
            float sidewaysSpeed = Time.deltaTime > 0f ? Vector3.Dot(travelled, transform.right) / Time.deltaTime : 0f;
            float desiredRoll = walking && speed > 0f ? -Mathf.Clamp(sidewaysSpeed / speed, -1f, 1f) * strafeRoll : 0f;
            roll = Mathf.Lerp(roll, desiredRoll, blend);
            peek=Mathf.Lerp(peek,canControl?PlayerControls.Peek:0,1-Mathf.Exp(-peekResponse*Time.deltaTime));
            Vector3 neutral=transform.TransformPoint(cameraOrigin+cameraOffset);
            Vector3 offset=transform.right*(peek*peekDistance);
            float requested=offset.magnitude;
            if(requested>.001f && Physics.SphereCast(neutral,.12f,offset.normalized,out RaycastHit hit,requested,~(1<<2),QueryTriggerInteraction.Ignore))
            { offset=offset.normalized*Mathf.Max(0,hit.distance-.025f); }
            float actual=peekDistance>.001f?Vector3.Dot(offset,transform.right)/peekDistance:0;
            view.transform.position=neutral+offset;
            view.transform.localRotation = cameraRotation * Quaternion.Euler(smoothLook.y, 0f, roll-actual*peekAngle);
        }
    }
}
