using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blackout.Player
{
    /// <summary>
    /// Crouching on a CharacterController: shrinks the capsule and lowers the camera,
    /// and refuses to stand up under an obstacle. Exposes state for other systems (e.g. noise).
    /// Assumes the transform origin sits at the player's feet.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerCrouch : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference crouch;
        [Tooltip("Off: press to toggle. On: crouch only while the button is held.")]
        [SerializeField] private bool holdToCrouch = false;

        [Header("References")]
        [SerializeField] private Transform cameraPivot;

        [Header("Dimensions")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 1.0f;
        [Tooltip("Distance from the top of the capsule down to the camera.")]
        [SerializeField] private float cameraOffsetFromTop = 0.2f;
        [Tooltip("Height change speed in meters per second.")]
        [SerializeField] private float transitionSpeed = 6f;

        [Header("Movement")]
        [SerializeField, Range(0f, 1f)] private float crouchSpeedMultiplier = 0.5f;

        [Header("Obstruction")]
        [SerializeField] private LayerMask obstructionMask = ~0;

        /// <summary>Raised with the new value whenever <see cref="IsCrouching"/> changes.</summary>
        public event Action<bool> CrouchChanged;

        public bool IsCrouching { get; private set; }
        public float SpeedMultiplier => IsCrouching ? crouchSpeedMultiplier : 1f;

        private readonly Collider[] overlapBuffer = new Collider[8];
        private CharacterController controller;
        private float currentHeight;
        private bool wantsToCrouch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            currentHeight = standingHeight;
            ApplyHeight();
        }

        private void OnEnable() => crouch?.action.Enable();

        private void OnDisable() => crouch?.action.Disable();

        private void Update()
        {
            ReadInput();

            // Only hold mode reaches a blocked stand-up here: it keeps retrying, so the
            // player stands automatically once clear. Toggle mode discards it in ReadInput.
            bool shouldCrouch = wantsToCrouch || (IsCrouching && !CanStand());
            SetCrouching(shouldCrouch);

            float targetHeight = IsCrouching ? crouchingHeight : standingHeight;
            if (!Mathf.Approximately(currentHeight, targetHeight))
            {
                currentHeight = Mathf.MoveTowards(currentHeight, targetHeight, transitionSpeed * Time.deltaTime);
                ApplyHeight();
            }
        }

        private void ReadInput()
        {
            if (crouch == null)
                return;

            InputAction action = crouch.action;
            if (holdToCrouch)
                wantsToCrouch = action.IsPressed();
            else if (action.WasPressedThisFrame())
            {
                // A stand-up attempt under an obstacle is discarded, not queued:
                // the player stays crouched until pressing again once clear.
                if (!wantsToCrouch || CanStand())
                    wantsToCrouch = !wantsToCrouch;
            }
        }

        private void SetCrouching(bool value)
        {
            if (IsCrouching == value)
                return;

            IsCrouching = value;
            CrouchChanged?.Invoke(value);
        }

        private bool CanStand()
        {
            float radius = controller.radius * 0.9f;
            Vector3 origin = transform.position;
            // Start slightly above the feet so the floor itself never counts as an obstacle.
            Vector3 bottom = origin + Vector3.up * (controller.radius + 0.1f);
            Vector3 top = origin + Vector3.up * (standingHeight - controller.radius);

            int count = Physics.OverlapCapsuleNonAlloc(
                bottom, top, radius, overlapBuffer, obstructionMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                if (overlapBuffer[i] != controller)
                    return false;
            }
            return true;
        }

        private void ApplyHeight()
        {
            controller.height = currentHeight;
            controller.center = new Vector3(0f, currentHeight * 0.5f, 0f);

            if (cameraPivot != null)
            {
                Vector3 local = cameraPivot.localPosition;
                local.y = currentHeight - cameraOffsetFromTop;
                cameraPivot.localPosition = local;
            }
        }

        private void OnValidate()
        {
            crouchingHeight = Mathf.Clamp(crouchingHeight, 0.1f, standingHeight);
            cameraOffsetFromTop = Mathf.Clamp(cameraOffsetFromTop, 0f, crouchingHeight);
        }
    }
}
