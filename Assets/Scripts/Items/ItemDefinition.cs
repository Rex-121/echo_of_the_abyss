using UnityEngine;

// 单种道具的静态定义，一个道具一个资产（Assets/Data/Items/）
// numericId 手工分配且全局唯一（0 禁用），存档用它紧凑序列化
[CreateAssetMenu(fileName = "Item_", menuName = "Echo/ItemDefinition")]
public class ItemDefinition : ScriptableObject, IInventoryItem, IPlaceableItem
{
    public ushort numericId;
    public string displayName = "";
    public Sprite icon;          // 道具栏图标
    public uint maxStack = 99;
    public BlockId placeable;    // 可放置成的方块，Air = 不可放置

    public ushort ItemID => numericId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public uint StackMax => maxStack;
    public BlockId Block => placeable;

    // 使用型道具覆写此方法；默认不可主动使用
    public virtual bool TryUse(ItemUseContext ctx) => false;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (maxStack < 1) maxStack = 1;
        if (numericId == 0)
            Debug.LogError($"[Item] {name} numericId 不能为 0", this);
    }
#endif
}
