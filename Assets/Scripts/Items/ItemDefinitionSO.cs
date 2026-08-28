using Sirenix.OdinInspector;
using UnityEngine;

// 单种道具的静态定义，一个道具一个资产（Assets/Data/Items/）
// numericId 手工分配且全局唯一（0 禁用），存档用它紧凑序列化
[CreateAssetMenu(fileName = "Item_", menuName = "Echo/ItemDefinition")]
public partial class ItemDefinitionSO : SerializedScriptableObject
{

#if UNITY_EDITOR
    void OnValidate()
    {
        if (stackMax < 1) stackMax = 1;
        if (itemID == 0)
            Debug.LogError($"[Item] {name} numericId 不能为 0", this);
    }
#endif
    
}
