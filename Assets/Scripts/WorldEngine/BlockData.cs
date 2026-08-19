// 单格方块数据
public struct BlockData
{
    public BlockId id;
    public byte hp; // 剩余血量

    public bool IsAir => id == BlockId.Air;
}
