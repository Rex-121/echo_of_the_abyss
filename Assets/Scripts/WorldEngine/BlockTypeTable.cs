using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 单种方块的静态定义
[Serializable]
public class BlockEntry
{
    public BlockId id;
    public TileBase tile; // Air 留空
    public byte maxHp;
}

// 方块注册表：id → 外观与硬度，编辑器里直接换 tile/调数值
[CreateAssetMenu(fileName = "BlockTypeTable", menuName = "Echo/BlockTypeTable")]
public class BlockTypeTable : ScriptableObject
{
    public List<BlockEntry> entries = new List<BlockEntry>();

    Dictionary<BlockId, BlockEntry> lookup;

    void OnEnable() => BuildLookup();

    void BuildLookup()
    {
        lookup = new Dictionary<BlockId, BlockEntry>();
        foreach (BlockEntry e in entries)
            if (!lookup.ContainsKey(e.id)) lookup.Add(e.id, e);
    }

    // 找不到返回 null（Air 未配置等场景）
    public BlockEntry Get(BlockId id)
    {
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(id, out BlockEntry e) ? e : null;
    }
}
