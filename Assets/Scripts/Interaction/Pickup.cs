using Blackout.Inventory;
using UnityEngine;

namespace Blackout.Interaction
{
    /// <summary>
    /// World item that goes into the interactor's inventory and is destroyed.
    /// Resources are finite: pickups never respawn.
    /// </summary>
    public class Pickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int amount = 1;

        private bool consumed;

        public string Prompt => item != null ? $"Pick up {item.DisplayName}" : "Pick up";

        public void Interact(PlayerInteractor interactor)
        {
            if (consumed || item == null)
                return;

            var inventory = interactor.GetComponentInParent<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogWarning("[Pickup] Interactor has no PlayerInventory.", interactor);
                return;
            }

            if (!inventory.TryAdd(item, amount))
            {
                Debug.Log($"[Pickup] No room for {amount}x {item.DisplayName}.", this);
                return;
            }

            consumed = true;
            Debug.Log($"[Pickup] Picked up {amount}x {item.DisplayName} (total: {inventory.Count(item)}).", this);
            Destroy(gameObject);
        }
    }
}
