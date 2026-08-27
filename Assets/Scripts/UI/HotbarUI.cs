using UnityEngine;
using UnityEngine.UI;

// 道具栏 UI：订阅 Inventory 事件刷新，数字键 1-8 / 点击格子选中
// G/C 为临时 GM 键，接掉落闭环时删
public class HotbarUI : MonoBehaviour
{
    public ItemTypeTable itemTable;
    public Color selectedColor = new Color(1f, 0.85f, 0.2f);

    Inventory inv;
    Image[] borders;
    Image[] icons;
    Text[] counts;

    void Awake()
    {
        if (itemTable == null)
        {
            Debug.LogError("[HotbarUI] 缺少 itemTable", this);
            enabled = false;
            return;
        }

        inv = new Inventory(itemTable, transform.childCount);
        borders = new Image[inv.SlotCount];
        icons = new Image[inv.SlotCount];
        counts = new Text[inv.SlotCount];

        for (int i = 0; i < inv.SlotCount; i++) BuildSlot(i);

        inv.OnSlotChanged += RefreshSlot;
        inv.OnSelectedChanged += RefreshSelection;

        RefreshAll();
    }

    void Update()
    {
        for (int i = 0; i < 8; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                inv.Select(i % inv.SlotCount);

        // GM：加一组测试道具 / 清空
        if (Input.GetKeyDown(KeyCode.G))
        {
            inv.Add(ItemId.DirtChunk, 5);
            inv.Add(ItemId.WallBrick, 5);
            inv.Add(ItemId.AlchemistTable, 1);
        }
        if (Input.GetKeyDown(KeyCode.C)) inv.Clear();
    }

    // 收集场景预设的格子边框，动态建 icon / 数量 / 点击响应
    // 子物体须在收集完 borders 后再建（GetChild 按层级索引取格子）
    void BuildSlot(int i)
    {
        Transform slot = transform.GetChild(i);
        borders[i] = slot.GetComponent<Image>();

        var iconObj = new GameObject("icon");
        iconObj.transform.SetParent(slot, false);
        icons[i] = iconObj.AddComponent<Image>();
        icons[i].raycastTarget = false;
        icons[i].rectTransform.anchorMin = Vector2.one * 0.1f; // 拉伸留 10% 边距
        icons[i].rectTransform.anchorMax = Vector2.one * 0.9f;
        icons[i].rectTransform.offsetMin = icons[i].rectTransform.offsetMax = Vector2.zero;

        var countObj = new GameObject("count");
        countObj.transform.SetParent(slot, false);
        counts[i] = countObj.AddComponent<Text>();
        counts[i].font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        counts[i].fontSize = 28;
        counts[i].alignment = TextAnchor.LowerRight;
        counts[i].raycastTarget = false;
        RectTransform rt = counts[i].rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-10f, 4f);
        rt.offsetMax = new Vector2(-6f, -6f);
        countObj.AddComponent<Shadow>();

        // 手动 tint 边框做选中态，transition 必须关掉避免覆盖
        var btn = slot.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        int idx = i;
        btn.onClick.AddListener(() => inv.Select(idx));
    }

    void RefreshAll()
    {
        for (int i = 0; i < inv.SlotCount; i++) RefreshSlot(i);
        RefreshSelection();
    }

    void RefreshSlot(int i)
    {
        ItemStack s = inv.GetSlot(i);
        ItemEntry e = s.IsEmpty ? null : itemTable.Get(s.id);
        icons[i].sprite = e != null ? e.icon : null;
        counts[i].text = !s.IsEmpty && s.count > 1 ? s.count.ToString() : "";
    }

    void RefreshSelection()
    {
        for (int i = 0; i < borders.Length; i++)
            borders[i].color = i == inv.Selected ? selectedColor : Color.white;
    }
}
