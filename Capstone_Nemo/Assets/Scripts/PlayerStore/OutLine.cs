using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class OutLine : MonoBehaviour
{
    public Color outLineColor = Color.white;
    [Range(1, 3)]
    public float thickness = 1;

    SpriteRenderer spriteRenderer;
    SpriteRenderer[] clones;
    static readonly Vector2[] DIRS = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
    float ppu;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ppu = spriteRenderer.sprite != null ? spriteRenderer.sprite.pixelsPerUnit : 16;
        clones = new SpriteRenderer[4];
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("OutLine_" + i);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spriteRenderer.sprite;
            sr.color = outLineColor;
            sr.sortingLayerID = spriteRenderer.sortingLayerID;
            sr.sortingOrder = spriteRenderer.sortingOrder - 1;
            sr.enabled = false;
            clones[i] = sr;
        }
    }
    void ApplyPositions()
    {
        if (spriteRenderer.sprite == null) return;
        var offset = thickness / (float)ppu;
        clones[0].transform.localPosition = Vector2.up * offset;
        clones[1].transform.localPosition = Vector2.down * offset;
        clones[2].transform.localPosition = Vector2.left * offset;
        clones[3].transform.localPosition = Vector2.right * offset;
    }

    public void SetOutLine(bool on)
    {
        if (clones == null)
            return;

        if (on)
            ApplyPositions();

        for(int i = 0; i <4; i++)
        {
            if (clones[i] == null) continue;
            clones[i].sprite = spriteRenderer.sprite;
            clones[i].color = outLineColor;
            clones[i].sortingLayerID = spriteRenderer.sortingLayerID;
            clones[i].sortingOrder = spriteRenderer.sortingOrder - 1;
            clones[i].enabled = on;
        }
    }
}
