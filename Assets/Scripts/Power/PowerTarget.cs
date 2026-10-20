using UnityEngine;

namespace Blackout.Power
{
    /// <summary>
    /// Switches objects on and off with a <see cref="BreakerBox"/>: lights, fans, a lift, a door.
    /// </summary>
    public class PowerTarget : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BreakerBox breaker;
        [Tooltip("Active while powered, inactive while not. Toggled on both edges.")]
        [SerializeField] private GameObject[] poweredObjects;
        [Tooltip("Inverse of the above: active only while unpowered, e.g. an emergency sign.")]
        [SerializeField] private GameObject[] unpoweredObjects;

        private void OnEnable()
        {
            if (breaker == null)
            {
                Debug.LogWarning("[PowerTarget] No breaker assigned.", this);
                return;
            }

            breaker.PowerChanged += Apply;
            Apply(breaker.IsPowered);
        }

        private void OnDisable()
        {
            if (breaker != null)
                breaker.PowerChanged -= Apply;
        }

        private void Apply(bool powered)
        {
            foreach (GameObject target in poweredObjects)
                if (target != null)
                    target.SetActive(powered);

            foreach (GameObject target in unpoweredObjects)
                if (target != null)
                    target.SetActive(!powered);
        }
    }
}
