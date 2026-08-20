using UnityEngine;

// 鼠标挖掘/放置：规则判断走数据层
public class PlayerInteraction : MonoBehaviour
{
    public World world => GameManager.main.gameWorld.World;
    public BlockId placeBlock = BlockId.Dirt; // 放地面
    public float actionInterval = 0.15f;       // 长按 tick 间隔
    public byte digDamage = 1;
    public float maxReach = 5f;                // 可操作距离（格）

    Camera cam;
    float timer;

    void Awake() => cam = Camera.main;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        Vector2Int cell = MouseToCell();

        if (Input.GetMouseButton(0)) // 挖
        {
            if (TryDig(cell)) timer = actionInterval;
        }
        else if (Input.GetMouseButton(1)) // 放
        {
            if (TryPlace(cell)) timer = actionInterval;
        }
    }

    // 鼠标屏幕坐标 → 格子坐标（floor，cellSize=1 时成立）
    Vector2Int MouseToCell()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = Mathf.Abs(cam.transform.position.z);
        Vector3 world = cam.ScreenToWorldPoint(mouse);
        return new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
    }

    // 禁挖玩家自身占据的格子（拆自己脚下的地板）
    bool TryDig(Vector2Int cell)
    {
        if (!world.IsCellLoaded(cell.x, cell.y)) return false;
        if (world.GetBlock(cell.x, cell.y).IsAir) return false;
        if (!InRange(cell)) return false;
        if (CellOverlapsPlayer(cell)) return false;

        world.DamageBlock(cell.x, cell.y, digDamage);
        return true;
    }

    bool TryPlace(Vector2Int cell)
    {
        if (!world.IsCellLoaded(cell.x, cell.y)) return false;
        if (!InRange(cell)) return false;
        if (CellOverlapsPlayer(cell)) return false;

        // 墙占据地面格（Dirt→Wall，该格不可走）；其他方块填 Air
        BlockId cur = world.GetBlock(cell.x, cell.y).id;
        if (placeBlock == BlockId.Wall ? cur != BlockId.Dirt : cur != BlockId.Air) return false;

        world.SetBlock(cell.x, cell.y, placeBlock);
        return true;
    }

    bool InRange(Vector2Int cell) =>
        Vector2.Distance(world.CellCenter(cell.x, cell.y), transform.position) <= maxReach;

    // 格子矩形与玩家 AABB 相交（玩家跨格时也能拦住）
    bool CellOverlapsPlayer(Vector2Int cell)
    {
        Vector3 c = world.CellCenter(cell.x, cell.y);
        Vector3 p = transform.position;
        float r = PlayerMovement.HalfSize;
        return c.x + 0.5f > p.x - r && c.x - 0.5f < p.x + r
            && c.y + 0.5f > p.y - r && c.y - 0.5f < p.y + r;
    }
}
