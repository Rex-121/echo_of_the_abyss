using Sirenix.OdinInspector;
using UnityEngine;

public partial class ItemDefinitionSO : IInventoryItem
{
    [ShowInInspector]
    public ushort itemID { get; set; }
    [ShowInInspector]
    public string displayName { get; set; }
    
    [ShowInInspector]
    public Sprite icon { get; set; }
    
    [ShowInInspector]
    public uint stackMax { get; private set; }
}