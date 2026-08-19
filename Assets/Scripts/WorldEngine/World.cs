using System;
using System.Collections.Generic;
using UnityEngine;

// 纯数据层：chunk 按需生成，所有方块读写必须经过这里，不持有任何渲染对象
// 坐标不变量：全局方块坐标 == Tilemap cell 坐标（Grid 位于原点、cellSize=1）
// 负数寻址依赖算术移位语义：-33 >> 5 = -2, -33 & 31 = 31
public class World
{
    readonly Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
    readonly BlockTypeTable table;

    // 单格数据变化（含变 Air），渲染层订阅此事件同步 Tilemap
    public event Action<int, int, BlockId> OnBlockChanged;

    public World(BlockTypeTable table) => this.table = table;

    public Chunk GetChunk(Vector2Int coord) =>
        chunks.TryGetValue(coord, out Chunk c) ? c : null;

    public bool HasChunk(Vector2Int coord) => chunks.ContainsKey(coord);

    // 所有已生成 chunk（卸载扫描用）
    public IEnumerable<Chunk> LoadedChunks => chunks.Values;

    // 该格所在 chunk 是否已生成（未生成区不可走也不可交互）
    public bool IsCellLoaded(int x, int y) =>
        chunks.ContainsKey(new Vector2Int(x >> Chunk.Shift, y >> Chunk.Shift));

    // 未生成区返回 Air（移动层据此挡住玩家）
    public BlockData GetBlock(int x, int y) =>
        chunks.TryGetValue(new Vector2Int(x >> Chunk.Shift, y >> Chunk.Shift), out Chunk c)
            ? c.Get(x & Chunk.Mask, y & Chunk.Mask)
            : default;

    // 修改方块的唯一入口：写数据 + 标记 dirty + 抛事件；未生成区返回 false
    public bool SetBlock(int x, int y, BlockId id)
    {
        if (!chunks.TryGetValue(new Vector2Int(x >> Chunk.Shift, y >> Chunk.Shift), out Chunk c)) return false;

        BlockEntry e = table.Get(id);
        c.Set(x & Chunk.Mask, y & Chunk.Mask, new BlockData { id = id, hp = e != null ? e.maxHp : (byte)0 });
        c.dirty = true;

        OnBlockChanged?.Invoke(x, y, id);
        return true;
    }

    // 扣血，血尽自动变 Air；返回是否破坏
    public bool DamageBlock(int x, int y, byte dmg)
    {
        if (!chunks.TryGetValue(new Vector2Int(x >> Chunk.Shift, y >> Chunk.Shift), out Chunk c)) return false;

        int lx = x & Chunk.Mask, ly = y & Chunk.Mask;
        BlockData b = c.Get(lx, ly);
        if (b.IsAir) return false;

        if (b.hp > dmg)
        {
            b.hp -= dmg;
            c.Set(lx, ly, b);
            c.dirty = true;
            return false;
        }

        SetBlock(x, y, BlockId.Air);
        return true;
    }

    // 生成流程填充完数据后注册
    public void AddChunk(Chunk c) => chunks[c.coord] = c;

    // 世界坐标 → chunk 坐标
    public Vector2Int ChunkCoordOf(Vector3 worldPos) => new Vector2Int(
        Mathf.FloorToInt(worldPos.x) >> Chunk.Shift,
        Mathf.FloorToInt(worldPos.y) >> Chunk.Shift);

    // 格子中心的世界坐标（Grid 原点、cellSize=1，纯数学换算）
    public Vector3 CellCenter(int x, int y) => new Vector3(x + 0.5f, y + 0.5f, 0f);
}
