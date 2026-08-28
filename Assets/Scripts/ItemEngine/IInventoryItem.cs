using UnityEngine;

// 道具身份与展示的抽象：容器与 UI 只依赖此接口，不感知具体道具类型
public interface IInventoryItem
{
    ushort ItemID { get; }       // 稳定数字 id：存档序列化 + 调试
    string DisplayName { get; }  // 名称，tooltip/日志用
    Sprite Icon { get; }         // 道具栏图标
    uint StackMax { get; }       // 单格堆叠上限；1 = 不可堆叠

    // 通用使用入口；不能主动使用的道具直接返回 false
    // bool TryUse(ItemUseContext ctx);
}
