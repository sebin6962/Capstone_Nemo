using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CropTile
{
    public Vector3Int tilePosition;
    public CropData cropData;
    public int currentStage = 0;
    public float timer = 0f;
    public bool isWatered = false;
    public GameObject cropOverlayObject; // 타일 위 스프라이트용 오브젝트

    public CropTile(Vector3Int pos, CropData data, GameObject overlay)
    {
        tilePosition = pos;
        cropData = data;
        cropOverlayObject = overlay;
    }
}

