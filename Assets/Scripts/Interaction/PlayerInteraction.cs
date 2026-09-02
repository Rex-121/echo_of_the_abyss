using Player;
using UnityEngine;

// 鼠标挖掘/道具使用：规则判断走数据层（放置规则在 PlaceBlockEffect）
namespace Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        public World world => GameManager.main.gameWorld.World;
        public float actionInterval = 0.15f;       // 长按 tick 间隔
        public byte digDamage = 1;
        public float maxReach = 5f;                // 可挖掘距离（格）

        public PlayerInventory bag;
        // Camera cam;
        float timer;

        // void Awake()
        // { 
        //     cam = Camera.main;
        // }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            // if (Input.GetMouseButton(0)) // 挖
            // {
            //     if (TryDig(MouseToCell())) timer = actionInterval;
            // }
            // else 
            if (Input.GetMouseButton(1) && bag != null) // 用选中道具
            {
                bag.Inv.Use(PlayerCursor.main.current, PlayerCursor.main.world);
                timer = actionInterval;
            }
        }

        // 鼠标屏幕坐标 → 世界坐标
        // Vector3 MouseWorld()
        // {
        //     Vector3 mouse = Input.mousePosition;
        //     mouse.z = Mathf.Abs(cam.transform.position.z);
        //     return cam.ScreenToWorldPoint(mouse);
        // }
        //
        // // 鼠标世界坐标 → 格子（+0.5 取整：格心=整数）
        // Vector2Int MouseToCell() => new Vector2Int(
        //     Mathf.FloorToInt(MouseWorld().x + 0.5f), Mathf.FloorToInt(MouseWorld().y + 0.5f));
        //
        // // 禁挖玩家自身占据的格子（拆自己脚下的地板）
        // bool TryDig(Vector2Int cell)
        // {
        //     if (!world.IsCellLoaded(cell.x, cell.y)) return false;
        //     if (world.GetBlock(cell.x, cell.y).IsAir) return false;
        //     if (!InRange(cell)) return false;
        //     if (CellOverlapsPlayer(cell)) return false;
        //
        //     world.DamageBlock(cell.x, cell.y, digDamage);
        //     return true;
        // }
        //
        // bool InRange(Vector2Int cell) =>
        //     Vector2.Distance(world.CellCenter(cell.x, cell.y), transform.position) <= maxReach;
        //
        // // 格子矩形与玩家 AABB 相交（玩家跨格时也能拦住）
        // bool CellOverlapsPlayer(Vector2Int cell)
        // {
        //     Vector3 c = world.CellCenter(cell.x, cell.y);
        //     Vector3 p = transform.position;
        //     float r = PlayerMovement.HalfSize;
        //     return c.x + 0.5f > p.x - r && c.x - 0.5f < p.x + r
        //                                 && c.y + 0.5f > p.y - r && c.y - 0.5f < p.y + r;
        // }
    }
}
