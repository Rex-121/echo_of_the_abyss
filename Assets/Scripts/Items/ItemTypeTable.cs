using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// 道具注册表：收集全部 ItemDefinition，供存档 id 还原 / GM / 掉落表遍历
[CreateAssetMenu(fileName = "ItemTypeTable", menuName = "Echo/ItemTypeTable")]
public class ItemTypeTable : SerializedScriptableObject
{
    public List<ItemDefinitionSO> items = new ();

    Dictionary<ushort, IInventoryItem> byId;
    Dictionary<string, IInventoryItem> byName; // key: 资产名（GM 按名取用）

    void OnEnable() => Build();

    void Build()
    {
        byId = new Dictionary<ushort, IInventoryItem>();
        byName = new Dictionary<string, IInventoryItem>(StringComparer.Ordinal);
        foreach (IInventoryItem d in items)
        {
            if (d == null) continue;
            if (!byId.ContainsKey(d.itemID)) byId.Add(d.itemID, d);
            if (!byName.ContainsKey(d.displayName)) byName.Add(d.displayName, d);
        }
    }

    public IInventoryItem GetById(ushort numericId) =>
        byId != null && byId.TryGetValue(numericId, out IInventoryItem d) ? d : null;

    public IInventoryItem GetByName(string assetName) =>
        byName != null && byName.TryGetValue(assetName, out IInventoryItem d) ? d : null;

    public IReadOnlyList<IInventoryItem> All => items;

#if UNITY_EDITOR
    void OnValidate()
    {
        var seen = new HashSet<ushort>();
        foreach (IInventoryItem d in items)
        {
            if (d == null) continue;
            if (!seen.Add(d.itemID))
                Debug.LogError($"[ItemTypeTable] numericId 重复: {d.itemID} ({d.displayName})", this);
        }
    }
#endif
}
