using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 墙格挡光：4 连通墙合并成单个 ShadowCaster2D（整块外轮廓多边形，无逐格接缝）
// 依赖：点光开启 Shadow Intensity；被照物用 Sprite-Lit 材质
public class WallShadows : MonoBehaviour
{
    class Segment
    {
        public readonly HashSet<Vector3Int> cells = new HashSet<Vector3Int>();
        public ShadowCaster2D caster;
    }

    static readonly FieldInfo fiShapePath = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo fiShapeHash = typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly Vector3Int[] Dirs = { new Vector3Int(0, 1, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(-1, 0, 0) };
    static int shapeCounter; // 每写一次递增，保证 hash 变化触发 mesh 重建

    public float edgePad = 0f; // 预留：轮廓外扩（格）。背光侧外扩会把阴影起点推远，默认关闭

    readonly Dictionary<Vector3Int, Segment> cellSeg = new Dictionary<Vector3Int, Segment>(); // 格 → 所属块
    readonly List<Segment> segments = new List<Segment>();
    readonly Stack<ShadowCaster2D> casterPool = new Stack<ShadowCaster2D>();
    readonly Dictionary<Vector2Int, List<Vector2Int>> edges = new Dictionary<Vector2Int, List<Vector2Int>>(); // 轮廓边：起点 → 终点表
    readonly List<List<Vector2Int>> loops = new List<List<Vector2Int>>();
    readonly Queue<Vector3Int> bfs = new Queue<Vector3Int>();
    readonly HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
    Transform root; // caster 池父物体

    public void SetWall(int x, int y, bool wall)
    {
        var c = new Vector3Int(x, y, 0);
        if (wall) AddCell(c);
        else RemoveCell(c);
    }

    // 清一个 chunk 范围的投影（渲染卸载）：批量删格，受影响块各重算一次
    public void ClearArea(int baseX, int baseY, int size)
    {
        List<Segment> touched = null;
        for (int x = baseX; x < baseX + size; x++)
            for (int y = baseY; y < baseY + size; y++)
            {
                var c = new Vector3Int(x, y, 0);
                if (!cellSeg.TryGetValue(c, out Segment s)) continue;
                s.cells.Remove(c);
                cellSeg.Remove(c);
                (touched ??= new List<Segment>()).Add(s);
            }
        if (touched == null) return;
        foreach (Segment s in touched)
        {
            if (s.cells.Count == 0) Destroy(s);
            else Split(s);
        }
    }

    // 新格并入邻居块（多个邻居块则吞并成一个），无邻居新建块
    void AddCell(Vector3Int c)
    {
        if (cellSeg.ContainsKey(c)) return;

        Segment target = null;
        for (int d = 0; d < 4; d++)
        {
            if (!cellSeg.TryGetValue(c + Dirs[d], out Segment s) || s == target) continue;
            if (target == null) { target = s; continue; }
            foreach (Vector3Int cell in s.cells) { target.cells.Add(cell); cellSeg[cell] = target; }
            Destroy(s); // 吞并：其 caster 回池，轮廓由 target 重算
        }
        if (target == null) { target = new Segment(); segments.Add(target); }
        target.cells.Add(c);
        cellSeg[c] = target;
        Rebuild(target);
    }

    void RemoveCell(Vector3Int c)
    {
        if (!cellSeg.TryGetValue(c, out Segment s)) return;
        s.cells.Remove(c);
        cellSeg.Remove(c);
        if (s.cells.Count == 0) Destroy(s);
        else Split(s); // 可能分裂或只是形状变化
    }

    // 重算连通性：首分量留原块，其余分裂成新块，全部重写轮廓
    void Split(Segment s)
    {
        visited.Clear();
        bfs.Clear();
        List<List<Vector3Int>> comps = null;
        foreach (Vector3Int start in s.cells)
        {
            if (visited.Contains(start)) continue;
            var comp = new List<Vector3Int>();
            bfs.Enqueue(start); visited.Add(start);
            while (bfs.Count > 0)
            {
                Vector3Int cur = bfs.Dequeue();
                comp.Add(cur);
                for (int d = 0; d < 4; d++)
                {
                    Vector3Int n = cur + Dirs[d];
                    if (s.cells.Contains(n) && !visited.Contains(n)) { visited.Add(n); bfs.Enqueue(n); }
                }
            }
            (comps ??= new List<List<Vector3Int>>()).Add(comp);
        }

        s.cells.Clear();
        s.cells.UnionWith(comps[0]);
        foreach (Vector3Int c in comps[0]) cellSeg[c] = s;
        List<Segment> extra = null;
        for (int i = 1; i < comps.Count; i++)
        {
            var ns = new Segment();
            segments.Add(ns);
            foreach (Vector3Int c in comps[i]) { ns.cells.Add(c); cellSeg[c] = ns; }
            (extra ??= new List<Segment>()).Add(ns);
        }

        Rebuild(s);
        if (extra != null) foreach (Segment ns in extra) Rebuild(ns);
    }

    // 外轮廓 → 写入 caster。shapePath 无公开 setter，反射写 + hash 递增，
    // 组件下一帧自动重建 mesh。洞（环形墙）与对角触点取最大外环近似
    void Rebuild(Segment s)
    {
        // 收集边界有向边（顺时针绕行：上→ 右↓ 下← 左↑）
        edges.Clear();
        foreach (Vector3Int c in s.cells)
        {
            if (!s.cells.Contains(new Vector3Int(c.x, c.y + 1, 0))) AddEdge(c.x, c.y + 1, c.x + 1, c.y + 1);
            if (!s.cells.Contains(new Vector3Int(c.x + 1, c.y, 0))) AddEdge(c.x + 1, c.y + 1, c.x + 1, c.y);
            if (!s.cells.Contains(new Vector3Int(c.x, c.y - 1, 0))) AddEdge(c.x + 1, c.y, c.x, c.y);
            if (!s.cells.Contains(new Vector3Int(c.x - 1, c.y, 0))) AddEdge(c.x, c.y, c.x, c.y + 1);
        }

        // 拼环：分叉点（对角触点）选相对来向最顺时针的出边，保持环独立
        loops.Clear();
        while (true)
        {
            Vector2Int start = default;
            bool found = false;
            foreach (var kv in edges)
                if (kv.Value.Count > 0) { start = kv.Key; found = true; break; }
            if (!found) break;

            var loop = new List<Vector2Int>();
            Vector2Int cur = start, dirIn = Vector2Int.zero;
            while (true)
            {
                loop.Add(cur);
                List<Vector2Int> outs = edges[cur];
                int pick = 0;
                if (outs.Count > 1)
                    for (int i = 1; i < outs.Count; i++)
                        if (TurnScore(dirIn, outs[i] - cur) < TurnScore(dirIn, outs[pick] - cur)) pick = i;
                Vector2Int next = outs[pick];
                outs.RemoveAt(pick);
                dirIn = next - cur;
                cur = next;
                if (cur == start) break;
            }
            loops.Add(loop);
        }

        // 顺时针环（shoelace<0）为外轮廓，取最大；逆时针环为洞，丢弃
        List<Vector2Int> best = null;
        long bestA2 = 0;
        foreach (List<Vector2Int> loop in loops)
        {
            long a2 = Shoelace(loop);
            if (a2 >= 0 || -a2 <= bestA2) continue;
            bestA2 = -a2;
            best = loop;
        }
        if (best == null) return;

        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (Vector3Int c in s.cells) { if (c.x < minX) minX = c.x; if (c.y < minY) minY = c.y; }

        // 共线点合并，转块左下角局部坐标，反转成逆时针（与引擎默认一致）
        int n = best.Count;
        var pts = new List<Vector3>(n);
        for (int i = 0; i < n; i++)
        {
            Vector2Int prev = best[(i + n - 1) % n], curV = best[i], next = best[(i + 1) % n];
            int dx = curV.x - prev.x, dy = curV.y - prev.y;
            if (dx * (next.y - curV.y) - dy * (next.x - curV.x) == 0 &&
                dx * (next.x - curV.x) + dy * (next.y - curV.y) > 0) continue;
            // 外法线 (-dy,dx)（顺时针环）；顶点偏移 = 入边法线 + 出边法线，直角处为对角偏移
            int ex = next.x - curV.x, ey = next.y - curV.y;
            float ox = (-dy - ey) * edgePad;
            float oy = (dx + ex) * edgePad;
            pts.Add(new Vector3(curV.x - minX + ox, curV.y - minY + oy, 0f));
        }
        pts.Reverse();

        if (s.caster == null) s.caster = TakeCaster();
        s.caster.transform.position = new Vector3(minX, minY, 0f);
        fiShapePath.SetValue(s.caster, pts.ToArray());
        fiShapeHash.SetValue(s.caster, ++shapeCounter);
    }

    static long Shoelace(List<Vector2Int> loop)
    {
        long a2 = 0;
        for (int i = 0; i < loop.Count; i++)
        {
            Vector2Int p = loop[i], q = loop[(i + 1) % loop.Count];
            a2 += (long)p.x * q.y - (long)q.x * p.y;
        }
        return a2;
    }

    // 顺时针优先序：右转 0 < 直行 1 < 左转 2 < 折返 3
    static int TurnScore(Vector2Int din, Vector2Int dout)
    {
        int cross = din.x * dout.y - din.y * dout.x;
        if (cross < 0) return 0;
        if (din.x * dout.x + din.y * dout.y > 0) return 1;
        if (cross > 0) return 2;
        return 3;
    }

    void AddEdge(int fx, int fy, int tx, int ty)
    {
        var f = new Vector2Int(fx, fy);
        if (!edges.TryGetValue(f, out List<Vector2Int> list)) edges[f] = list = new List<Vector2Int>(2);
        list.Add(new Vector2Int(tx, ty));
    }

    // 块销毁：caster 回池
    void Destroy(Segment s)
    {
        segments.Remove(s);
        if (s.caster != null)
        {
            s.caster.gameObject.SetActive(false);
            casterPool.Push(s.caster);
            s.caster = null;
        }
    }

    ShadowCaster2D TakeCaster()
    {
        if (casterPool.Count > 0)
        {
            ShadowCaster2D c = casterPool.Pop();
            c.gameObject.SetActive(true);
            return c;
        }

        if (root == null)
        {
            root = new GameObject("WallShadowCasters").transform;
            root.SetParent(transform, false);
        }
        var obj = new GameObject("caster");
        obj.transform.SetParent(root, false);
        return obj.AddComponent<ShadowCaster2D>();
    }
}
