using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 给选中物体按 sprite 实际轮廓生成/刷新 ShadowCaster2D（贴形投影，非包围盒矩形）
// 轮廓取导入生成的物理形状（pivot 原点、世界单位）；换朝向 sprite 后重跑即可
// m_ShapePath 无公开 setter，反射写入（同 WallShadows）；hash 变化触发 ExecuteInEditMode
// 的 Update 自动重建 shadow mesh，无需手动生成
public static class SpriteShadowCasterTool
{
    const string MenuPath = "Tools/2D/按 Sprite 轮廓生成 ShadowCaster2D";
    const float SimplifyPixels = 0.5f; // 抽稀容差（像素），只删共线冗余点

    static readonly FieldInfo fiShapePath = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo fiShapeHash = typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);

    [MenuItem(MenuPath, true)]
    static bool Validate()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null) return true;
        }
        return false;
    }

    [MenuItem(MenuPath)]
    static void Apply()
    {
        foreach (GameObject go in Selection.gameObjects) ApplyOne(go);
    }

    static void ApplyOne(GameObject go)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            Debug.LogError($"[{go.name}] 缺少 SpriteRenderer 或 sprite，跳过", go);
            return;
        }

        Sprite sprite = sr.sprite;
        int shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount == 0)
        {
            EditorUtility.DisplayDialog("Sprite ShadowCaster2D",
                $"{go.name}：sprite 无物理形状，请在其图片导入设置开启 Generate FallBack Physics Shape", "确定");
            return;
        }

        // 多个形状（洞/分离部件）时取面积最大环，单路径不支持洞
        var raw = new List<Vector2>();
        var buf = new List<Vector2>();
        float bestA = 0;
        int dropped = 0;
        for (int i = 0; i < shapeCount; i++)
        {
            buf.Clear();
            sprite.GetPhysicsShape(i, buf);
            float a = Mathf.Abs(Shoelace(buf));
            if (a <= bestA) { dropped++; continue; }
            bestA = a;
            raw = new List<Vector2>(buf);
        }

        if (raw.Count < 3)
        {
            Debug.LogError($"[{go.name}] 物理形状退化（{raw.Count} 点），跳过", go);
            return;
        }

        // flip 镜像到物体局部（物理形状已是 pivot 原点坐标）
        if (sr.flipX) for (int i = 0; i < raw.Count; i++) raw[i] = new Vector2(-raw[i].x, raw[i].y);
        if (sr.flipY) for (int i = 0; i < raw.Count; i++) raw[i] = new Vector2(raw[i].x, -raw[i].y);

        // 统一逆时针（与引擎默认轮廓绕向一致，反了阴影会向光侧挤出）
        if (Shoelace(raw) < 0) raw.Reverse();

        int rawCount = raw.Count;
        List<Vector2> pts = SimplifyRing(raw, SimplifyPixels / sprite.pixelsPerUnit);

        var caster = go.GetComponent<ShadowCaster2D>();
        if (caster == null) caster = Undo.AddComponent<ShadowCaster2D>(go); // AddComponent 即完成初始化：全排序层 + 默认参数

        Undo.RecordObject(caster, "Sprite ShadowCaster2D");
        var path = new Vector3[pts.Count];
        for (int i = 0; i < pts.Count; i++) path[i] = pts[i];
        fiShapePath.SetValue(caster, path);
        fiShapeHash.SetValue(caster, (int)fiShapeHash.GetValue(caster) + 1);
        EditorUtility.SetDirty(caster);

        // 轮廓 AABB 与 sprite 渲染 bounds 粗校验，差异大说明坐标异常
        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector2 p in pts) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        Bounds sb = sprite.bounds;
        if (Mathf.Abs((max.x - min.x) - sb.size.x) > 0.1f * sb.size.x ||
            Mathf.Abs((max.y - min.y) - sb.size.y) > 0.1f * sb.size.y)
            Debug.LogWarning($"[{go.name}] 轮廓尺寸 ({max.x - min.x:F2}x{max.y - min.y:F2}) 与 sprite bounds ({sb.size.x:F2}x{sb.size.y:F2}) 差异超 10%", go);

        string extra = dropped > 0 ? "，丢弃 " + dropped + " 个次要形状" : "";
        Debug.Log($"[{go.name}] ShadowCaster2D 轮廓 {pts.Count} 点（原始 {rawCount}{extra}）", go);
    }

    static float Shoelace(List<Vector2> p)
    {
        float a = 0;
        for (int i = 0; i < p.Count; i++)
        {
            Vector2 q = p[(i + 1) % p.Count];
            a += p[i].x * q.y - q.x * p[i].y;
        }
        return a;
    }

    // 闭环 RDP：0 与 N/2 为锚点分两段，保闭环不散架
    static List<Vector2> SimplifyRing(List<Vector2> ring, float tol)
    {
        int n = ring.Count;
        var res = new List<Vector2> { ring[0] };
        if (n > 4)
        {
            int mid = n / 2;
            AppendRdp(ring, 0, mid, tol, res);
            res.Add(ring[mid]);
            AppendRdp(ring, mid, n, tol, res); // 末段终点绕回 ring[0]，不重复加入
        }
        else res.AddRange(ring.GetRange(1, n - 1));
        return res;
    }

    // 追加 (lo, hi) 区间内需保留的点，不含两端（res 末尾需已是 p[lo]）
    static void AppendRdp(List<Vector2> p, int lo, int hi, float tol, List<Vector2> res)
    {
        float maxD = 0;
        int idx = -1;
        Vector2 a = p[lo], b = p[hi % p.Count];
        for (int i = lo + 1; i < hi; i++)
        {
            float d = DistanceToSegment(p[i], a, b);
            if (d > maxD) { maxD = d; idx = i; }
        }
        if (idx < 0 || maxD <= tol) return;
        AppendRdp(p, lo, idx, tol, res);
        res.Add(p[idx]);
        AppendRdp(p, idx, hi, tol, res);
    }

    static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 1e-10f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
        return Vector2.Distance(p, a + ab * t);
    }
}
