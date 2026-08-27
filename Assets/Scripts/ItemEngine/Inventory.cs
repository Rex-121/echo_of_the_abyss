using System;
using UnityEngine;

// 纯数据层：定长槽容器，UI 订阅事件刷新，不持有任何渲染对象
public class Inventory
{
    readonly ItemStack[] slots;
    readonly ItemTypeTable table;

    public int SlotCount => slots.Length;
    public int Selected { get; private set; }

    // 单槽堆变化（含变空）
    public event Action<int> OnSlotChanged;
    // 选中槽变化（同索引不触发）
    public event Action OnSelectedChanged;

    public Inventory(ItemTypeTable table, int slotCount)
    {
        this.table = table;
        slots = new ItemStack[slotCount];
    }

    // 加道具，返回实际放入数：先并同 id 未满堆，再开空槽；maxStack≤1 视为不可堆叠
    public int Add(ItemId id, int amount)
    {
        if (id == ItemId.None || amount <= 0) return 0;

        int max = MaxStack(id);
        int put = 0;

        if (max > 1)
            for (int i = 0; i < slots.Length && amount > 0; i++)
            {
                if (slots[i].id != id || slots[i].count >= max) continue;
                int n = Mathf.Min(max - slots[i].count, amount);
                slots[i].count += (ushort)n;
                amount -= n; put += n;
                OnSlotChanged?.Invoke(i);
            }

        for (int i = 0; i < slots.Length && amount > 0; i++)
        {
            if (!slots[i].IsEmpty) continue;
            int n = Mathf.Min(max, amount);
            slots[i] = new ItemStack { id = id, count = (ushort)n };
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

    int MaxStack(ItemId id)
    {
        ItemEntry e = table.Get(id);
        return e == null || e.maxStack < 1 ? 1 : e.maxStack;
    }
}
