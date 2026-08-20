using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 世界入口：流式无限世界——跟随玩家按需生成/渲染 chunk，远处卸载渲染（数据保留）
// 无物理：可走/碰撞全部由数据层判定（Dirt 可走，Air/未生成区不可走）
public class GameWorld : MonoBehaviour
{
    
    public BlockTypeTable blockTable;
    public int seed = 20260818;
    public int activeRadius = 2; // 玩家周围保持生成+渲染的圈数
    public int unloadRadius = 4; // 超出则卸载渲染（数据保留，回来重渲染）

    const int ChunksPerTick = 1; // 每帧生成上限

    public World World { get; private set; }
    public bool IsReady { get; private set; }

    public Tilemap terrain;
    Transform player; // 由 GameManager 注入
    readonly Queue<Vector2Int> pending = new Queue<Vector2Int>();
    Vector2Int lastPlayerChunk = new Vector2Int(int.MaxValue, int.MaxValue);

    void Awake()
    {
        if (blockTable == null || terrain == null)
        {
            Debug.LogError("GameWorld: blockTable / terrain 未赋值");
            enabled = false;
            return;
        }

        // 材质（Sprite-Lit-Default）已在场景 TilemapRenderer 上配置，代码不再干预

        World = new World(blockTable);
        World.OnBlockChanged += OnBlockChanged;
    }

    void Start() => StartCoroutine(StreamRoutine());

    // 流式加载主循环
    IEnumerator StreamRoutine()
    {
        // 初始：围绕原点生成一圈再出生
        int count = 0;
        foreach (Vector2Int c in CoordsInRadius(Vector2Int.zero, activeRadius))
        {
            GenerateAndPaint(c);
            if (++count % 2 == 0) yield return null;
        }

        IsReady = true; // 初始地形完毕，GameManager 检测到此标志后出生角色

        while (true)
        {
            if (player != null)
            {
                Vector2Int pc = World.ChunkCoordOf(player.position);
                if (pc != lastPlayerChunk)
                {
                    lastPlayerChunk = pc;
                    RefreshPending(pc);
                    UnloadFar(pc);
                }

                // 每帧最多生成 N 个，跨 chunk 时玩家早有整圈缓冲
                for (int i = 0; i < ChunksPerTick && pending.Count > 0; i++)
                    GenerateAndPaint(pending.Dequeue());
            }
            yield return null;
        }
    }

    // 重算待加载队列：半径内缺失的、或已卸载渲染的
    void RefreshPending(Vector2Int pc)
    {
        pending.Clear();
        foreach (Vector2Int c in CoordsInRadius(pc, activeRadius))
        {
            Chunk ch = World.GetChunk(c);
            if (ch == null || !ch.painted) pending.Enqueue(c);
        }
    }

    // 卸载远处渲染，数据保留
    void UnloadFar(Vector2Int pc)
    {
        foreach (Chunk c in World.LoadedChunks)
        {
            if (!c.painted) continue;
            if (Mathf.Abs(c.coord.x - pc.x) <= unloadRadius && Mathf.Abs(c.coord.y - pc.y) <= unloadRadius) continue;
            ClearPaint(c);
        }
    }

    // 无数据则生成，然后渲染
    void GenerateAndPaint(Vector2Int coord)
    {
        Chunk chunk = World.GetChunk(coord);
        if (chunk == null)
        {
            chunk = new Chunk(coord);
            WorldEngine.FillChunk(chunk, seed, blockTable);
            World.AddChunk(chunk);
        }
        PaintChunk(chunk);
    }

    // 数据层单格变化 → 同步渲染（Air 清 tile）
    void OnBlockChanged(int x, int y, BlockId id)
    {
        BlockEntry e = blockTable.Get(id);
        terrain.SetTile(new Vector3Int(x, y, 0), e != null ? e.tile : null);
    }

    // 把 chunk 数据刷到渲染层（运行时改动走 World.SetBlock）
    void PaintChunk(Chunk c)
    {
        int baseX = c.coord.x << Chunk.Shift;
        int baseY = c.coord.y << Chunk.Shift;
        var pos = new Vector3Int();

        c.painted = true;

        for (int ly = 0; ly < Chunk.Size; ly++)
        {
            for (int lx = 0; lx < Chunk.Size; lx++)
            {
                BlockData b = c.blocks[Chunk.Index(lx, ly)];
                pos.Set(baseX + lx, baseY + ly, 0);

                BlockEntry e = blockTable.Get(b.id);
                if (!b.IsAir && e != null && e.tile != null) terrain.SetTile(pos, e.tile);
            }
        }
    }

    // 清掉 chunk 渲染（数据不动）
    void ClearPaint(Chunk c)
    {
        int baseX = c.coord.x << Chunk.Shift;
        int baseY = c.coord.y << Chunk.Shift;
        var pos = new Vector3Int();

        for (int ly = 0; ly < Chunk.Size; ly++)
        {
            for (int lx = 0; lx < Chunk.Size; lx++)
            {
                pos.Set(baseX + lx, baseY + ly, 0);
                if (!c.blocks[Chunk.Index(lx, ly)].IsAir) terrain.SetTile(pos, null);
            }
        }
        c.painted = false;
    }

    // 半径内所有 chunk 坐标，近的排前面
    List<Vector2Int> CoordsInRadius(Vector2Int center, int r)
    {
        var list = new List<Vector2Int>((2 * r + 1) * (2 * r + 1));
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
                list.Add(new Vector2Int(center.x + dx, center.y + dy));

        list.Sort((a, b) =>
            (Mathf.Abs(a.x - center.x) + Mathf.Abs(a.y - center.y))
          - (Mathf.Abs(b.x - center.x) + Mathf.Abs(b.y - center.y)));
        return list;
    }

    // GameManager 出生角色后注入，流式加载开始跟随
    public void TrackPlayer(Transform t) => player = t;

    // 出生点：初始区内 x=0 列从上往下第一个 Dirt 格
    public Vector3 GetSpawnPoint()
    {
        int top = (activeRadius + 1) * Chunk.Size;
        for (int y = top - 1; y >= -top; y--)
            if (World.GetBlock(0, y).id == BlockId.Dirt) return World.CellCenter(0, y);
        return World.CellCenter(0, -top); // 兜底：整列皆空
    }
}
