// 单格道具数据
public struct ItemStack
{
    public ItemId id;
    public ushort count;

    public bool IsEmpty => id == ItemId.None;
}
