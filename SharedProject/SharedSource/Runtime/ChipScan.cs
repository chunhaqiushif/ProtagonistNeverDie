using System.Collections.Generic;
using Barotrauma;
using Barotrauma.Items.Components;

namespace ProtagonistNeverDie;

/// <summary>
/// 识别耳机容器内插入了哪些芯片。
/// </summary>
internal static partial class PluginCore
{
    // ---------------------------------------------------------------------
    // 芯片识别
    // ---------------------------------------------------------------------
    private static Dictionary<Identifier, Item> CollectChips(Item ownerItem)
    {
        Dictionary<Identifier, Item> result = new Dictionary<Identifier, Item>();
        if (ownerItem == null) return result;

        ItemContainer container = ownerItem.GetComponent<ItemContainer>();
        if (container == null) return result;

        foreach (Item contained in container.Inventory.AllItems)
        {
            if (contained == null || contained.Prefab == null) continue;
            Identifier identifier = contained.Prefab.Identifier;
            if (!ChipIdentifiers.Contains(identifier)) continue;
            if (!result.ContainsKey(identifier)) result[identifier] = contained;
        }

        return result;
    }
}
