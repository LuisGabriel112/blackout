using UnityEngine;

namespace Blackout.AI
{
    /// <summary>
    /// Debug aid: tints the Looter by its current state so the AI can be read at a glance, even in
    /// the dark (the tint is emissive). Works on a material instance, so the shared asset is untouched.
    /// </summary>
    [RequireComponent(typeof(Looter))]
    public class LooterStateColor : MonoBehaviour
    {
        [Tooltip("Defaults to the first Renderer found on this object or its children.")]
        [SerializeField] private Renderer target;
        [SerializeField] private Color patrol = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color investigate = new Color(1f, 0.85f, 0.1f);
        [SerializeField] private Color search = new Color(1f, 0.5f, 0f);
        [SerializeField] private Color chase = new Color(0.9f, 0.1f, 0.1f);
        [SerializeField, Min(0f)] private float emissionBoost = 0.7f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Looter looter;
        private Material material;
        private Looter.State? shown;

        private void Awake()
        {
            looter = GetComponent<Looter>();
            if (target == null)
                target = GetComponentInChildren<Renderer>();
            if (target == null)
            {
                Debug.LogWarning("[LooterStateColor] No Renderer found: nothing to tint.", this);
                enabled = false;
                return;
            }

            material = target.material;
            material.EnableKeyword("_EMISSION");
        }

        private void Update()
        {
            if (shown == looter.Current)
                return;

            shown = looter.Current;
            Color c = ColorFor(shown.Value);
            material.SetColor(BaseColorId, c);
            material.SetColor(EmissionColorId, c * emissionBoost);
        }

        private void OnDestroy()
        {
            if (material != null)
                Destroy(material);
        }

        private Color ColorFor(Looter.State state)
        {
            switch (state)
            {
                case Looter.State.Investigate: return investigate;
                case Looter.State.Search: return search;
                case Looter.State.Chase: return chase;
                default: return patrol;
            }
        }
    }
}
