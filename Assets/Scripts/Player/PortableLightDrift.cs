using UnityEngine;
using UnityEngine.Rendering.Universal;

// 便携光：强度呼吸（正弦+Perlin抖动）+ 光心在小圆内来回漂移
[RequireComponent(typeof(Light2D))]
public class PortableLightDrift : MonoBehaviour
{
    public float baseIntensity = 1.2f;
    public float breatheAmplitude = 0.25f;
    public float breatheSpeed = 2f;
    public float flickerRandomness = 0.3f; // Perlin 抖动占比
    public float driftRadius = 0.3f;       // 光心漂移半径（格）
    public float driftSpeed = 1.5f;

    Light2D light2d;
    Vector3 origin;
    float seed;

    void Awake()
    {
        light2d = GetComponent<Light2D>();
        origin = transform.localPosition;
        seed = Random.value * 100f;
    }

    void Update()
    {
        float t = Time.time;

        // 呼吸：正弦为主 + Perlin 抖动
        float breath = Mathf.Sin(t * breatheSpeed) * (1f - flickerRandomness)
                     + (Mathf.PerlinNoise(seed, t * breatheSpeed) * 2f - 1f) * flickerRandomness;
        light2d.intensity = baseIntensity + breath * breatheAmplitude;

        // 光心漂移：双频正弦走不重复轨迹，在小圆内来回游走
        float a = t * driftSpeed + seed;
        transform.localPosition = origin + new Vector3(
            Mathf.Sin(a) * driftRadius,
            Mathf.Sin(a * 1.618f) * driftRadius * 0.6f, 0f);
    }
}
