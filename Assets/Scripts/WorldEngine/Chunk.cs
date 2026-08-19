using UnityEngine;

// 32x32 数据块；Size 取 2 的幂，坐标换算可用位运算
public class Chunk
{
    public const int Size = 32;
    public const int Shift = 5; // log2(Size)
    public const int Mask = Size - 1;

    public readonly Vector2Int coord; // chunk 坐标
    public readonly BlockData[] blocks = new BlockData[Size * Size];
    public bool dirty; // 改动标记，存档用
    public bool painted; // 渲染是否激活（远处卸载后 false，数据保留）

    public Chunk(Vector2Int coord) => this.coord = coord;

    // 局部坐标 → 一维索引
    public static int Index(int lx, int ly) => (ly << Shift) | lx;

    public BlockData Get(int lx, int ly) => blocks[Index(lx, ly)];

    public void Set(int lx, int ly, BlockData b) => blocks[Index(lx, ly)] = b;
}
