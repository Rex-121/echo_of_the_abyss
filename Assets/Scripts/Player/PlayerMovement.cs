using UnityEngine;

// 俯视角移动：可走判定走数据层（Dirt 可走，Air/界外不可走），不依赖物理
public class PlayerMovement : MonoBehaviour
{
    public const float HalfSize = 0.4f; // 玩家 AABB 半宽（0.8 格，能钻 1 格宽通道）

    public float moveSpeed = 5f;
    public World world => GameManager.main.gameWorld.World;

    const float Step = 0.05f; // 碰撞步进精度

    void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        Vector2 delta = input * (moveSpeed * Time.deltaTime);

        TryMove(new Vector2(delta.x, 0f)); // 轴分离，贴墙可滑
        TryMove(new Vector2(0f, delta.y));
    }

    // 分小步移动，步进失败即停（自然贴墙）
    void TryMove(Vector2 delta)
    {
        int n = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / Step));
        Vector2 d = delta / n;
        Vector2 p = transform.position;

        for (int i = 0; i < n; i++)
        {
            Vector2 next = p + d;
            if (!CanStand(next)) break;
            p = next;
        }
        transform.position = p;
    }

    // 玩家 AABB 覆盖的格子是否全部为 Dirt
    bool CanStand(Vector2 c)
    {
        const float e = 0.0001f; // 收缩边界，避免恰在格线上多算一格
        int x0 = Mathf.FloorToInt(c.x - HalfSize + e);
        int x1 = Mathf.FloorToInt(c.x + HalfSize - e);
        int y0 = Mathf.FloorToInt(c.y - HalfSize + e);
        int y1 = Mathf.FloorToInt(c.y + HalfSize - e);

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (world.GetBlock(x, y).id != BlockId.Dirt) return false;
        return true;
    }
}
