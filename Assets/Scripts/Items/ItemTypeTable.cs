using System;
using System.Collections.Generic;
using UnityEngine;

// 道具注册表：收集全部 ItemDefinition，供存档 id 还原 / GM / 掉落表遍历
[CreateAssetMenu(fileName = "ItemTypeTable", menuName = "Echo/ItemTypeTable")]
public class ItemTypeTable : ScriptableObject
{
    public List<ItemDefinition> items = new List<ItemDefinition>();

    Dictionary<ushort, ItemDefinition> byId;
    Dictionary<string, ItemDefinition> byName; // key: 资产名（GM 按名取用）

    void OnEnable() => Build();

    void Build()
    {
        byId = new Dictionary<ushort, ItemDefinition>();
        byName = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
        foreach (ItemDefinition d in items)
        {
            if (d == null) continue;
            if (!byId.ContainsKey(d.numericId)) byId.Add(d.numericId, d);
            if (!byName.ContainsKey(d.name)) byName.Add(d.name, d);
        }
    }

    public ItemDefinition GetById(ushort numericId) =>
        byId != null && byId.TryGetValue(numericId, out ItemDefinition d) ? d : null;

    public ItemDefinition GetByName(string assetName) =>
        byName != null && byName.TryGetValue(assetName, out ItemDefinition d) ? d : null;

    public IReadOnlyList<ItemDefinition> All => items;

#if UNITY_EDITOR
    void OnValidate()
    {
        var seen = new HashSet<ushort>();
        foreach (ItemDefinition d in items)
        {
            if (d == null) continue;
            if (!seen.Add(d.numericId))
                Debug.LogError($"[ItemTypeTable] numericId 重复: {d.numericId} ({d.name})", this);
        }
    }
#endif
}
