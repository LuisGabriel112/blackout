using Blackout.Player;
using UnityEngine;

namespace Blackout.Detection
{
    /// <summary>
    /// Turns the player's movement into noise: silent when still, quiet when crouched,
    /// loud when walking. Drives the <see cref="StimulusSource"/> on the same object.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(StimulusSource))]
    public class PlayerNoise : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float walkRadius = 8f;
        [SerializeField, Min(0f)] private float crouchRadius = 2.5f;
        [Tooltip("Horizontal speed below this counts as standing still.")]
        [SerializeField, Min(0f)] private float stillThreshold = 0.1f;

        private CharacterController controller;
        private PlayerCrouch crouch;
        private StimulusSource source;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            crouch = GetComponent<PlayerCrouch>();
            source = GetComponent<StimulusSource>();
        }

        private void Update()
        {
            Vector3 v = controller.velocity;
            float speed = new Vector2(v.x, v.z).magnitude;

            if (speed < stillThreshold)
                source.Radius = 0f;
            else
                source.Radius = crouch != null && crouch.IsCrouching ? crouchRadius : walkRadius;
        }
    }
}
