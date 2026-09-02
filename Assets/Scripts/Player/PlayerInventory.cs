using UnityEngine;

// 玩家背包：挂玩家身上，UI 与交互共用同一容器；持有使用道具的统一入口
public class PlayerInventory : MonoBehaviour
{
    public int slotCount = 8;

    Inventory _inv;
    public Inventory Inv => _inv ??= new Inventory(slotCount); // 懒建，免 Awake 顺序问题

    // 使用选中道具：成功自动扣 1
    public bool UseSelected(ItemUseContext ctx) =>
        Inv.TryGetSelected(out ItemStack s) && s.item.TryUse(ctx) && Inv.ConsumeSelected();
}
