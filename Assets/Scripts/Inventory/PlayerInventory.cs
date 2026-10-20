using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blackout.Inventory
{
    /// <summary>
    /// Minimal slot-based inventory. Each slot holds one item type up to its max stack size.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Serializable]
        public class Stack
        {
            [SerializeField] private ItemDefinition item;
            [SerializeField] private int amount;

            public Stack(ItemDefinition item) => this.item = item;

            public ItemDefinition Item => item;
            public int Amount { get => amount; internal set => amount = value; }
        }

        [SerializeField, Min(1)] private int slotCount = 12;
        [Tooltip("Read-only view for debugging in the Inspector.")]
        [SerializeField] private List<Stack> stacks = new List<Stack>();

        /// <summary>Raised after any change to the contents.</summary>
        public event Action InventoryChanged;

        public IReadOnlyList<Stack> Stacks => stacks;
        public int SlotCount => slotCount;

        /// <summary>
        /// Adds all of <paramref name="amount"/> or nothing.
        /// Fills existing stacks of the item first, then empty slots.
        /// </summary>
        public bool TryAdd(ItemDefinition item, int amount = 1)
        {
            if (item == null || amount <= 0)
                return false;
            if (GetSpaceFor(item) < amount)
                return false;

            int remaining = amount;
            foreach (Stack stack in stacks)
            {
                if (stack.Item != item)
                    continue;
                int added = Mathf.Min(remaining, item.MaxStackSize - stack.Amount);
                stack.Amount += added;
                remaining -= added;
                if (remaining == 0)
                    break;
            }

            while (remaining > 0)
            {
                var stack = new Stack(item);
                stack.Amount = Mathf.Min(remaining, item.MaxStackSize);
                remaining -= stack.Amount;
                stacks.Add(stack);
            }

            InventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Removes all of <paramref name="amount"/> or nothing.
        /// Drains the newest stacks first, so partial stacks stay at the front.
        /// </summary>
        public bool TryRemove(ItemDefinition item, int amount = 1)
        {
            if (item == null || amount <= 0)
                return false;
            if (Count(item) < amount)
                return false;

            int remaining = amount;
            for (int i = stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                Stack stack = stacks[i];
                if (stack.Item != item)
                    continue;
                int taken = Mathf.Min(remaining, stack.Amount);
                stack.Amount -= taken;
                remaining -= taken;
                if (stack.Amount == 0)
                    stacks.RemoveAt(i);
            }

            InventoryChanged?.Invoke();
            return true;
        }

        /// <summary>Total amount of <paramref name="item"/> across all stacks.</summary>
        public int Count(ItemDefinition item)
        {
            int total = 0;
            foreach (Stack stack in stacks)
                if (stack.Item == item)
                    total += stack.Amount;
            return total;
        }

        private int GetSpaceFor(ItemDefinition item)
        {
            int space = (slotCount - stacks.Count) * item.MaxStackSize;
            foreach (Stack stack in stacks)
                if (stack.Item == item)
                    space += item.MaxStackSize - stack.Amount;
            return space;
        }
    }
}
