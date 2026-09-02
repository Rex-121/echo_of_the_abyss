using UnityEngine;

// 建造：方块放置的规则判断与执行
public class BuildManager : MonoBehaviour
{
    public static BuildManager main;

    void Awake()
    {
        if (main == null)
        {
            main = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 放置入口：全部规则通过才写入；user 为使用者（距离与占位判定）
    public bool TryPlace(Vector3 worldPos, BlockId block, Transform user, float maxReach)
    {
        World world = GameManager.main.gameWorld.World;
        var cell = new Vector2Int(Mathf.FloorToInt(worldPos.x + 0.5f), Mathf.FloorToInt(worldPos.y + 0.5f));

        if (!world.IsCellLoaded(cell.x, cell.y)) return false;
        if (!InRange(world, cell, user.position, maxReach)) return false;
        if (CellOverlapsPlayer(world, cell, user)) return false;

        // 墙占据地面格（Dirt→Wall，该格不可走）；其他方块填 Air
        BlockId cur = world.GetBlock(cell.x, cell.y).id;
        if (block == BlockId.Wall ? cur != BlockId.Dirt : cur != BlockId.Air) return false;

        return world.SetBlock(cell.x, cell.y, block);
    }

    bool InRange(World world, Vector2Int cell, Vector3 userPos, float maxReach) =>
        Vector3.Distance(world.CellCenter(cell.x, cell.y), userPos) <= maxReach;

    // 格子矩形与使用者 AABB 相交（跨格时也能拦住）
    bool CellOverlapsPlayer(World world, Vector2Int cell, Transform user)
    {
        Vector3 c = world.CellCenter(cell.x, cell.y);
        Vector3 p = user.position;
        float r = PlayerMovement.HalfSize;
        return c.x + 0.5f > p.x - r && c.x - 0.5f < p.x + r
            && c.y + 0.5f > p.y - r && c.y - 0.5f < p.y + r;
    }
}
