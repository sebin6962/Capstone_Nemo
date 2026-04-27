//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//[RequireComponent(typeof(SpriteRenderer))]
//public class YSort : MonoBehaviour
//{
//    private SpriteRenderer sr;

//    void Awake()
//    {
//        sr = GetComponent<SpriteRenderer>();
//    }

//    void LateUpdate()
//    {
//        // Y값이 작을수록 Order가 높아짐 → 더 앞에 렌더링됨
//        sr.sortingOrder = -(int)(transform.position.y * 100);
//    }
//}

using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSort : MonoBehaviour
{
    private SpriteRenderer sr;

    private bool lockSorting = false;
    private int lockedSortingOrder = 0;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (lockSorting)
        {
            sr.sortingOrder = lockedSortingOrder;
            return;
        }

        sr.sortingOrder = -(int)(transform.position.y * 100);
    }

    public void SetSortingLock(bool isLocked, int sortingOrder = 0)
    {
        lockSorting = isLocked;
        lockedSortingOrder = sortingOrder;
    }
}