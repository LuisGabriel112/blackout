using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Blackout.Interaction
{
    /// <summary>
    /// Raycasts from the player camera to find the <see cref="IInteractable"/> being looked at,
    /// and interacts with it when the Interact action is performed.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference interact;

        [Header("References")]
        [Tooltip("Camera the ray is cast from, along its forward axis.")]
        [SerializeField] private Transform playerCamera;

        [Header("Detection")]
        [SerializeField] private float range = 2.5f;
        [Tooltip("Layers that can be hit. Non-interactable hits (walls) still block the ray.")]
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Debug")]
        [SerializeField] private bool logFocusChanges = true;

        /// <summary>The interactable currently under the crosshair, or null.</summary>
        public IInteractable Current { get; private set; }

        /// <summary>Raised with the new target (or null) whenever <see cref="Current"/> changes.</summary>
        public event Action<IInteractable> FocusChanged;

        private void OnEnable()
        {
            if (interact == null)
                return;
            interact.action.performed += OnInteractPerformed;
            interact.action.Enable();
        }

        private void OnDisable()
        {
            if (interact == null)
                return;
            interact.action.performed -= OnInteractPerformed;
            interact.action.Disable();
        }

        private void Update() => UpdateFocus();

        private void UpdateFocus()
        {
            IInteractable hitInteractable = null;

            if (playerCamera != null
                && Physics.Raycast(playerCamera.position, playerCamera.forward, out RaycastHit hit,
                                   range, interactionMask, triggerInteraction))
            {
                hitInteractable = hit.collider.GetComponentInParent<IInteractable>();
            }

            // A destroyed target is no longer hit, so it falls out of focus here too.
            if (ReferenceEquals(hitInteractable, Current))
                return;

            Current = hitInteractable;
            FocusChanged?.Invoke(Current);
            if (logFocusChanges)
                Debug.Log(Current != null ? $"[Interactor] Looking at: {Current.Prompt}" : "[Interactor] Looking at: nothing", this);
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (!IsAlive(Current))
                return;
            Current.Interact(this);
        }

        // Interface references bypass Unity's overloaded null check on destroyed objects.
        private static bool IsAlive(IInteractable interactable)
        {
            return interactable is Object unityObject ? unityObject != null : interactable != null;
        }
    }
}
