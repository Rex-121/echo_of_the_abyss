// 单格道具数据：item 即身份（同种道具共享同一实例），null 表示空槽
public struct ItemStack
{
    public IInventoryItem item;
    public ushort count;

    public bool IsEmpty => item == null || count == 0;
}
