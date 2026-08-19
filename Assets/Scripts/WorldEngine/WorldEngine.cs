using UnityEngine;

// 地形生成引擎：纯函数，同 seed 同坐标结果一致
// 世界默认实心 Dirt，噪声挖出 Air 洞穴（不可走区域）
public static class WorldEngine
{
    // 单格判定：噪声值超过阈值为洞
    public static BlockId GenerateBlock(int x, int y, int seed)
    {
        float s = seed * 0.371f;
        float cave = Mathf.PerlinNoise(x * 0.09f + s, y * 0.09f + s);
        return cave > 0.62f ? BlockId.Air : BlockId.Dirt;
    }

    // 填充一个 chunk 的数据（不动渲染）
    public static void FillChunk(Chunk chunk, int seed, BlockTypeTable table)
    {
        BlockEntry dirt = table.Get(BlockId.Dirt);
        byte dirtHp = dirt != null ? dirt.maxHp : (byte)0;

        int baseX = chunk.coord.x << Chunk.Shift;
        int baseY = chunk.coord.y << Chunk.Shift;

        for (int ly = 0; ly < Chunk.Size; ly++)
        {
            for (int lx = 0; lx < Chunk.Size; lx++)
            {
                BlockId id = GenerateBlock(baseX + lx, baseY + ly, seed);
                chunk.blocks[Chunk.Index(lx, ly)] = new BlockData { id = id, hp = id == BlockId.Dirt ? dirtHp : (byte)0 };
            }
        }
    }
}
