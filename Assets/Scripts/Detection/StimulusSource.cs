using System.Collections.Generic;
using UnityEngine;

namespace Blackout.Detection
{
    public enum StimulusKind
    {
        /// <summary>Seen from afar, but only with a clear line of sight.</summary>
        Light,
        /// <summary>Heard through walls, within its radius.</summary>
        Noise
    }

    /// <summary>
    /// Anything that gives the player away: a lit lamp, a running generator, footsteps.
    /// Registers itself while enabled, so a lamp switched on by a <c>PowerTarget</c>
    /// (which toggles the GameObject) becomes detectable with no extra wiring.
    /// </summary>
    public class StimulusSource : MonoBehaviour
    {
        private static readonly List<StimulusSource> all = new List<StimulusSource>();

        /// <summary>Every enabled source. Read-only; do not cache across frames.</summary>
        public static IReadOnlyList<StimulusSource> All => all;

        [SerializeField] private StimulusKind kind = StimulusKind.Light;
        [SerializeField, Min(0f)] private float radius = 10f;
        [Tooltip("Relative weight when a listener perceives several sources at once.")]
        [SerializeField, Min(0f)] private float intensity = 1f;
        [Tooltip("If a Light sits on this object, use its range as the radius.")]
        [SerializeField] private bool matchLightRange = true;

        public StimulusKind Kind => kind;
        public float Intensity => intensity;
        public Vector3 Position => transform.position;

        /// <summary>Detection radius in meters. Zero means silent / invisible.</summary>
        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0f, value);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();

        private void Awake()
        {
            if (!matchLightRange)
                return;
            Light lamp = GetComponent<Light>();
            if (lamp != null)
                radius = lamp.range;
        }

        private void OnEnable() => all.Add(this);

        private void OnDisable() => all.Remove(this);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = kind == StimulusKind.Light ? new Color(1f, 0.8f, 0.3f) : new Color(0.3f, 0.8f, 1f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
