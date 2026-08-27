using System;
using System.Collections.Generic;
using UnityEngine;

// 单种道具的静态定义
[Serializable]
public class ItemEntry
{
    public ItemId id;
    public Sprite icon;        // 道具栏图标
    public string displayName; // 名称，预留 tooltip
    public ushort maxStack = 99;
    public BlockId placeable;  // 可放置成的方块，Air = 不可放置
}

// 道具注册表：id → 图标/堆叠/放置，编辑器里直接配
[CreateAssetMenu(fileName = "ItemTypeTable", menuName = "Echo/ItemTypeTable")]
public class ItemTypeTable : ScriptableObject
{
    public List<ItemEntry> entries = new List<ItemEntry>();

    Dictionary<ItemId, ItemEntry> lookup;

    void OnEnable() => BuildLookup();

    void BuildLookup()
    {
        lookup = new Dictionary<ItemId, ItemEntry>();
        foreach (ItemEntry e in entries)
            if (!lookup.ContainsKey(e.id)) lookup.Add(e.id, e);
    }

    // 找不到返回 null（None 未配置等场景）
    public ItemEntry Get(ItemId id)
    {
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(id, out ItemEntry e) ? e : null;
    }
}
