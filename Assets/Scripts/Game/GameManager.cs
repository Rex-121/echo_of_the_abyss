using System.Collections;
using UnityEngine;

// 生成顺序编排：等世界初始生成完毕，再生成分辨角色
public class GameManager : MonoBehaviour
{
    public GameWorld gameWorld;

    IEnumerator Start()
    {
        if (gameWorld == null) gameWorld = FindObjectOfType<GameWorld>();
        while (gameWorld == null || !gameWorld.IsReady) yield return null;

        SpawnPlayer();
    }

    // 出生角色并交给世界跟踪（流式加载随之启动）
    void SpawnPlayer()
    {
        var go = new GameObject("Player");
        go.transform.position = gameWorld.GetSpawnPoint();
        Transform player = go.transform;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSquareSprite();
        sr.color = new Color(0.2f, 0.9f, 1f); // 青色，与泥土区分
        sr.sortingOrder = 10;

        var movement = go.AddComponent<PlayerMovement>();
        movement.world = gameWorld.World;

        var inter = go.AddComponent<PlayerInteraction>();
        inter.world = gameWorld.World;

        var cam = Camera.main;
        cam.orthographicSize = 8f;
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = player;

        gameWorld.TrackPlayer(player);
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
