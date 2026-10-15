using UnityEngine;

namespace Blackout.Inventory
{
    /// <summary>Static data for one kind of item.</summary>
    [CreateAssetMenu(fileName = "Item", menuName = "Blackout/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("Stable identifier (e.g. for saves). Do not change once in use.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, Min(1)] private int maxStackSize = 1;

        public string Id => id;
        public string DisplayName => displayName;
        public int MaxStackSize => maxStackSize;
    }
}
