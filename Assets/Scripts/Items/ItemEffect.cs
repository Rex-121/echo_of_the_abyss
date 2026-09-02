// 道具使用效果：类对应机制（放置/治疗/传送...），目标与数值全是子类字段
// 顺序执行，任一失败即中断且不消耗道具
public abstract class ItemEffect
{
    public abstract bool Apply(ItemUseContext ctx);
}
