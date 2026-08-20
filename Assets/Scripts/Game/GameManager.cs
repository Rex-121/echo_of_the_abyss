using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal; // Light2D 在此命名空间（源码 Light2D.cs 第 10 行）

// 生成顺序编排：等世界初始生成完毕，再生成分辨角色
public class GameManager : MonoBehaviour
{
    public GameWorld gameWorld;

    public static GameManager main;

    public GameObject player;
    private void Awake()
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

    IEnumerator Start()
    {

        while (gameWorld == null || !gameWorld.IsReady) yield return null;

        SpawnPlayer();
    }

    // 玩家为场景物品：这里只做定位、表现配置、注入运行时依赖（World 是运行时对象，Inspector 拖不了）
    void SpawnPlayer()
    {
        player.transform.position = gameWorld.GetSpawnPoint();

        // var sr = player.GetComponent<SpriteRenderer>();
        // sr.sprite = MakeSquareSprite();
        // sr.color = new Color(0.2f, 0.9f, 1f); // 青色，与泥土区分
        // sr.sortingOrder = 10;
        // 材质（Sprite-Lit-Default）已在场景 SpriteRenderer 上配置，代码不再覆盖

        // 注入 world（场景里无法序列化）
        // player.GetComponent<PlayerMovement>().world = gameWorld.World;
        // player.GetComponent<PlayerInteraction>().world = gameWorld.World;

        // 相机跟随挂相机上（挂玩家上会把玩家 z 拉进相机裁剪面）
        var cam = Camera.main;
        // cam.orthographicSize = 8f;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = player.transform;

        gameWorld.TrackPlayer(player.transform);
    }

    // 16x16 纯色方块 sprite，零美术依赖
    static Sprite MakeSquareSprite()
    {
        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        var cols = new Color32[16 * 16];
        for (int i = 0; i < cols.Length; i++) cols[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
    }
}
