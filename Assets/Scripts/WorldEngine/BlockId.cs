// 方块类型，Air 必须为 0（数组清零即空气）
// Dirt = 地面，玩家可走；Wall = 墙体，不可走；Air = 虚空，不可走
public enum BlockId : ushort
{
    Air = 0,
    Dirt = 1,
    Wall = 2,
}
