using System;
using System.Collections.Generic;
using Blackout.Interaction;
using Blackout.Inventory;
using UnityEngine;

namespace Blackout.Power
{
    /// <summary>
    /// Interactable panel that needs parts installed before it can be switched on.
    /// Parts are taken from the player's inventory one requirement at a time, in order,
    /// and are never given back: a wasted part is gone for good.
    /// </summary>
    public class BreakerBox : MonoBehaviour, IInteractable
    {
        [Serializable]
        public class Requirement
        {
            [SerializeField] private ItemDefinition item;
            [SerializeField, Min(1)] private int amount = 1;

            public ItemDefinition Item => item;
            public int Amount => amount;
        }

        [Header("Parts")]
        [Tooltip("Installed in this order. Each one is consumed whole or not at all.")]
        [SerializeField] private List<Requirement> requirements = new List<Requirement>();

        [Header("Debug")]
        [SerializeField] private bool startPowered;

        private int installed;
        private bool powered;

        /// <summary>True once every requirement is installed and the breaker has been flipped.</summary>
        public bool IsPowered => powered;

        /// <summary>Raised with the new state whenever <see cref="IsPowered"/> changes.</summary>
        public event Action<bool> PowerChanged;

        public string Prompt
        {
            get
            {
                if (powered)
                    return "Breaker is live";
                Requirement next = Next();
                if (next == null)
                    return "Flip breaker";
                string name = next.Item != null ? next.Item.DisplayName : "part";
                return next.Amount > 1 ? $"Install {next.Amount}x {name}" : $"Install {name}";
            }
        }

        private void Start()
        {
            if (!startPowered)
                return;
            installed = requirements.Count;
            SetPowered(true);
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (powered)
                return;

            Requirement next = Next();
            if (next == null)
            {
                SetPowered(true);
                return;
            }

            if (next.Item == null)
            {
                Debug.LogWarning($"[BreakerBox] Requirement {installed} has no item assigned.", this);
                return;
            }

            var inventory = interactor.GetComponentInParent<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogWarning("[BreakerBox] Interactor has no PlayerInventory.", interactor);
                return;
            }

            if (!inventory.TryRemove(next.Item, next.Amount))
            {
                Debug.Log($"[BreakerBox] Need {next.Amount}x {next.Item.DisplayName}, have {inventory.Count(next.Item)}.", this);
                return;
            }

            installed++;
            Debug.Log($"[BreakerBox] Installed {next.Amount}x {next.Item.DisplayName} ({installed}/{requirements.Count}).", this);
        }

        private Requirement Next() => installed < requirements.Count ? requirements[installed] : null;

        private void SetPowered(bool value)
        {
            if (powered == value)
                return;
            powered = value;
            Debug.Log(powered ? "[BreakerBox] Power restored." : "[BreakerBox] Power lost.", this);
            PowerChanged?.Invoke(powered);
        }
    }
}
