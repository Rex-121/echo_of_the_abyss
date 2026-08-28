using UnityEngine;

// 一次道具使用的上下文：只描述"谁在哪用"，不携带游戏类型
public sealed class ItemUseContext
{
    public GameObject user;   // 使用者（玩家）
    public Vector3 worldPos;  // 指向的世界位置
}
