using UnityEngine;

// 放置方块：把 block 放进指向的格子，规则与执行走 BuildManager
public class PlaceBlockEffect : ItemEffect
{
    public BlockId block = BlockId.Wall;
    public float maxReach = 5f; // 可放置距离（格）

    public override bool Apply(ItemUseContext ctx) =>
        ctx.user != null
        && BuildManager.main != null
        && BuildManager.main.TryPlace(ctx.worldPos, block, ctx.user.transform, maxReach);
}
