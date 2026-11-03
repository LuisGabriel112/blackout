using UnityEngine;

namespace Blackout.Power
{
    /// <summary>
    /// Subtle mains flicker for a lamp: smooth noise on the light intensity, plus the odd brief dip
    /// like a failing bulb. The range is left alone, so the radius the lamp is detected from never changes.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LampFlicker : MonoBehaviour
    {
        [Tooltip("Optional bulb whose emission follows the flicker.")]
        [SerializeField] private Renderer bulb;
        [SerializeField, Range(0f, 0.5f)] private float amount = 0.08f;
        [SerializeField, Min(0f)] private float speed = 6f;
        [Tooltip("Chance per second of a brief, deeper dip.")]
        [SerializeField, Range(0f, 1f)] private float dipChance = 0.1f;
        [SerializeField, Range(0f, 1f)] private float dipDepth = 0.5f;
        [SerializeField, Min(0.01f)] private float dipDuration = 0.12f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Light lamp;
        private MaterialPropertyBlock block;
        private float baseIntensity;
        private Color baseEmission;
        private float seed;
        private float dipEnd;

        private void Awake()
        {
            lamp = GetComponent<Light>();
            baseIntensity = lamp.intensity;
            seed = Random.value * 100f;

            if (bulb != null)
            {
                baseEmission = bulb.sharedMaterial.GetColor(EmissionColorId);
                block = new MaterialPropertyBlock();
            }
        }

        private void OnDisable()
        {
            if (lamp != null)
                lamp.intensity = baseIntensity;
            if (bulb != null)
                bulb.SetPropertyBlock(null);
        }

        private void Update()
        {
            float factor = 1f + (Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f) * amount;

            if (Time.time >= dipEnd && Random.value < dipChance * Time.deltaTime)
                dipEnd = Time.time + dipDuration;
            if (Time.time < dipEnd)
                factor *= 1f - dipDepth;

            lamp.intensity = baseIntensity * factor;

            if (bulb != null)
            {
                block.SetColor(EmissionColorId, baseEmission * factor);
                bulb.SetPropertyBlock(block);
            }
        }
    }
}
