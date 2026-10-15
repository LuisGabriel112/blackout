using UnityEngine;
using UnityEngine.InputSystem;

namespace Blackout.Player
{
    /// <summary>
    /// First-person walking and mouse/gamepad look on a CharacterController.
    /// Assumes the transform origin sits at the player's feet.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference move;
        [SerializeField] private InputActionReference look;

        [Header("References")]
        [Tooltip("Camera (or its pivot). Receives pitch; the body receives yaw.")]
        [SerializeField] private Transform cameraPivot;

        [Header("Look")]
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField] private float mouseSensitivity = 0.1f;
        [Tooltip("Degrees per second at full stick deflection.")]
        [SerializeField] private float gamepadSensitivity = 180f;
        [SerializeField, Range(0f, 89f)] private float pitchLimit = 85f;
        [SerializeField] private bool lockCursorOnStart = true;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float gravity = -20f;
        [Tooltip("Downward speed kept while grounded so the controller hugs slopes.")]
        [SerializeField] private float groundedStickSpeed = -2f;

        private CharacterController controller;
        private PlayerCrouch crouch;
        private float pitch;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            crouch = GetComponent<PlayerCrouch>();
        }

        private void OnEnable()
        {
            move?.action.Enable();
            look?.action.Enable();
        }

        private void OnDisable()
        {
            move?.action.Disable();
            look?.action.Disable();
        }

        private void Start()
        {
            if (lockCursorOnStart)
                SetCursorLocked(true);
        }

        private void Update()
        {
            HandleCursor();
            HandleLook();
            HandleMove();
        }

        private void HandleCursor()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetCursorLocked(false);
            else if (Cursor.lockState != CursorLockMode.Locked
                     && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                SetCursorLocked(true);
        }

        private void HandleLook()
        {
            if (look == null || cameraPivot == null)
                return;

            Vector2 input = look.action.ReadValue<Vector2>();
            bool fromPointer = look.action.activeControl?.device is Pointer;

            // Ignore the mouse while the cursor is free; the gamepad keeps working.
            if (fromPointer && Cursor.lockState != CursorLockMode.Locked)
                return;

            // Pointer delta is already per-frame; stick values are a rate.
            Vector2 delta = fromPointer
                ? input * mouseSensitivity
                : input * (gamepadSensitivity * Time.deltaTime);

            transform.Rotate(0f, delta.x, 0f, Space.Self);

            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            Vector2 input = move != null ? move.action.ReadValue<Vector2>() : Vector2.zero;

            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            direction = Vector3.ClampMagnitude(direction, 1f);

            float speed = walkSpeed * (crouch != null ? crouch.SpeedMultiplier : 1f);

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = groundedStickSpeed;
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = direction * speed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
