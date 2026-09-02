using UnityEngine;

// 玩家鼠标指示器：高亮选中格，超距时钳到 maxReach 范围内离鼠标最近的格（其他系统经 TryGetSelectedCell 取同一结果）
namespace Player
{
    public class PlayerCursor : MonoBehaviour
    {
        public float maxReach = 1f; // 选中格距玩家上限（unit）
        public float snapSpeed = 15f; // 指示器趋近目标格的速度
        public float flashSpeed = 10f; // 换格颜色恢复速度

        Vector2Int lastCell;
        float flash; // 换格瞬间 1，指数衰减到 0
        // public Color color = new Color(1f, 1f, 1f, 0.8f);

        Camera cam;
        
        public SpriteRenderer indicator;

        public Vector3 current;

        void Awake()
        {
            cam = Camera.main;

            indicator.transform.parent = null;
        }

        void Update()
        {
            bool has = TryGetSelectedCell(out Vector2Int cell);
            if (!has) return;
            current = new Vector3(cell.x, cell.y, 0f);
            // 平滑趋近目标格（指数衰减，帧率无关）
            float t = 1f - Mathf.Exp(-snapSpeed * Time.deltaTime);
            indicator.transform.position = Vector3.Lerp(indicator.transform.position, current, t);

            // 换格闪变：alpha 先压低再恢复
            if (cell != lastCell) { lastCell = cell; flash = 1f; }
            flash *= Mathf.Exp(-flashSpeed * Time.deltaTime);

            // 玩家所在格不可选中（红示）；后续建筑占格同样处理
            Vector2 p = transform.position;
            var playerCell = new Vector2Int(Mathf.FloorToInt(p.x + 0.5f), Mathf.FloorToInt(p.y + 0.5f));
            Color c = cell == playerCell ? new Color(1f, 0f, 0f, 1f) : Color.white;
            c.a = Mathf.Lerp(c.a, 0.15f, flash);
            indicator.color = c;
        }

        // 选中格：maxReach 范围内格心里离鼠标最近的；鼠标超距时自然钳到边界可达格（人在(0,0)指(1,5)→(1,1)）
        public bool TryGetSelectedCell(out Vector2Int cell)
        {
            Vector3 mouse = Input.mousePosition;
            mouse.z = Mathf.Abs(cam.transform.position.z);
            Vector3 world = cam.ScreenToWorldPoint(mouse);

            Vector2 p = transform.position;
            int r = Mathf.CeilToInt(maxReach);
            cell = default;
            float best = float.MaxValue;
            for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
            {
                var c = new Vector2Int(Mathf.RoundToInt(p.x) + x, Mathf.RoundToInt(p.y) + y);
                if (Vector2.Distance(c, p) > maxReach) continue; // 格心超玩家范围的剔除
                float d = Vector2.Distance(c, world);
                if (d < best) { best = d; cell = c; }
            }
            return true;
        }
    }
}
