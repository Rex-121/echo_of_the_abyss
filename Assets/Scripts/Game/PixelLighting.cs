using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 光照像素化：算出光贴图实际分辨率并设全局 _LightTexSize，
// PixelLit2D 材质用它把光照 UV 量化到像素网格（点采样效果）
// scale 自动取自资产的 Light Render Texture Scale，改资产即生效
[ExecuteAlways]
public class PixelLighting : MonoBehaviour
{
    public Renderer2DData renderer2D; // 拖 Assets/Settings/Renderer2D.asset

    static readonly PropertyInfo piScale = typeof(Renderer2DData).GetProperty("lightRenderTextureScale", BindingFlags.NonPublic | BindingFlags.Instance);

    Camera cam;

    void Awake() => cam = GetComponent<Camera>();

    void Update()
    {
        if (cam == null || renderer2D == null) return;

        float scale = (float)piScale.GetValue(renderer2D);
        int w = Mathf.Max(1, (int)(cam.pixelWidth * scale));
        int h = Mathf.Max(1, (int)(cam.pixelHeight * scale));
        Shader.SetGlobalVector("_LightTexSize", new Vector4(w, h, 1f / w, 1f / h));
    }
}
