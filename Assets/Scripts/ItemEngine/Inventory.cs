using System;
using UnityEngine;

// 纯数据层：定长槽容器，UI 订阅事件刷新，不持有任何渲染对象
public class Inventory
{
    readonly ItemStack[] slots;

    public int SlotCount => slots.Length;
    public int Selected { get; private set; }

    // 单槽堆变化（含变空）
    public event Action<int> OnSlotChanged;
    // 选中槽变化（同索引不触发）
    public event Action OnSelectedChanged;

    public Inventory(int slotCount)
    {
        slots = new ItemStack[slotCount];
    }

    public void Use(Vector3 pointAt, Vector3 world)
    {
        
        
        var item = slots[Selected];
        Debug.Log($"使用道具{item}, {pointAt}, {world}");
    }

    // 加道具，返回实际放入数：先并同种未满堆（引用相等即同种），再开空槽；StackMax≤1 视为不可堆叠
    public int Add(IInventoryItem item, int amount)
    {
        if (item == null || amount <= 0) return 0;

        int max = item.stackMax < 1 ? 1 : (int)item.stackMax;
        int put = 0;

        if (max > 1)
            for (int i = 0; i < slots.Length && amount > 0; i++)
            {
                if (slots[i].item != item || slots[i].count >= max) continue;
                int n = Mathf.Min(max - slots[i].count, amount);
                slots[i].count += (ushort)n;
                amount -= n; put += n;
                OnSlotChanged?.Invoke(i);
            }

        for (int i = 0; i < slots.Length && amount > 0; i++)
        {
            if (!slots[i].IsEmpty) continue;
            int n = Mathf.Min(max, amount);
            slots[i] = new ItemStack { item = item, count = (ushort)n };
            amount -= n; put += n;
            OnSlotChanged?.Invoke(i);
        }

        return put;
    }

    // 清空指定槽
    public void RemoveAt(int index)
    {
        if ((uint)index >= slots.Length || slots[index].IsEmpty) return;
        slots[index] = default;
        OnSlotChanged?.Invoke(index);
    }

    // 选中槽扣 1，扣尽变空（放置消耗预留）
    public bool ConsumeSelected()
    {
        if ((uint)Selected >= slots.Length || slots[Selected].IsEmpty) return false;

        ItemStack s = slots[Selected];
        s.count = (ushort)(s.count - 1);
        if (s.count == 0) s = default;
        slots[Selected] = s;
        OnSlotChanged?.Invoke(Selected);
        return true;
    }

    // 切换选中槽，无效索引忽略
    public void Select(int index)
    {
        if ((uint)index >= slots.Length || index == Selected) return;
        Selected = index;
        OnSelectedChanged?.Invoke();
        
        Debug.Log($"当前选中道具{index}");
    }

    public ItemStack GetSlot(int index) =>
        (uint)index < slots.Length ? slots[index] : default;

    // 选中槽为空时返回 false
    public bool TryGetSelected(out ItemStack stack)
    {
        stack = GetSlot(Selected);
        return !stack.IsEmpty;
    }

    // 清空全部槽，选中保持不变
    public void Clear()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty) continue;
            slots[i] = default;
            OnSlotChanged?.Invoke(i);
        }
    }
}
