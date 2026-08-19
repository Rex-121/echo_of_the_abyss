using UnityEngine;
using UnityEngine.Rendering.Universal;

// 光呼吸：强度随时间平滑起伏，可混入噪声做出火把闪烁感
[RequireComponent(typeof(Light2D))]
public class LightBreath : MonoBehaviour
{
    public float baseIntensity = 1.2f; // 基准强度
    public float amplitude = 0.25f;    // 起伏幅度
    public float speed = 2f;           // 呼吸速度
    [Range(0f, 1f)]
    public float randomness = 0.3f;    // 噪声占比，0=纯正弦呼吸

    Light2D light2d;
    float seed;

    void Awake()
    {
        light2d = GetComponent<Light2D>();
        seed = Random.value * 100f;
    }

    void Update()
    {
        float t = Time.time * speed + seed;
        float wave = Mathf.Sin(t);
        if (randomness > 0f)
            wave += (Mathf.PerlinNoise(t * 1.7f, seed) * 2f - 1f) * randomness;
        light2d.intensity = Mathf.Max(0f, baseIntensity + wave * amplitude);
    }
}
