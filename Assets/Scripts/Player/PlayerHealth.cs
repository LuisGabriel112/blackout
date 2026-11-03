using System;
using System.Collections;
using UnityEngine;

namespace Blackout.Player
{
    /// <summary>
    /// Minimal player health. On death it freezes movement, waits, and puts the player back at
    /// the spawn point with full health (no scene reload, so no Build Settings dependency).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float respawnDelay = 3f;
        [Tooltip("Debug only: draws the health in the corner of the Game view.")]
        [SerializeField] private bool showDebugLabel = true;

        /// <summary>Raised with (current, max) whenever health changes.</summary>
        public event Action<float, float> HealthChanged;

        /// <summary>Raised once when health reaches zero.</summary>
        public event Action Died;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public bool IsDead => Current <= 0f;

        private CharacterController controller;
        private FirstPersonController movement;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            movement = GetComponent<FirstPersonController>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            Current = maxHealth;
        }

        private void Start() => HealthChanged?.Invoke(Current, maxHealth);

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
                return;

            Current = Mathf.Max(0f, Current - amount);
            Debug.Log($"[PlayerHealth] -{amount:0} -> {Current:0}/{maxHealth:0}", this);
            HealthChanged?.Invoke(Current, maxHealth);

            if (Current <= 0f)
                Die();
        }

        private void Die()
        {
            Debug.Log("[PlayerHealth] Died.", this);
            if (movement != null)
                movement.enabled = false;
            Died?.Invoke();
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            // A CharacterController must be off while it is teleported.
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            controller.enabled = true;

            Current = maxHealth;
            if (movement != null)
                movement.enabled = true;
            HealthChanged?.Invoke(Current, maxHealth);
        }

        private void OnGUI()
        {
            if (!showDebugLabel)
                return;
            GUI.Label(new Rect(10f, 10f, 200f, 24f), IsDead ? "DEAD" : $"HP {Current:0}/{maxHealth:0}");
        }
    }
}
