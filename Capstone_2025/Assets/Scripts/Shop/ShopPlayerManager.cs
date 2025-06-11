using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopPlayerManager : MonoBehaviour
{
    public float moveSpeed = 10f;
    Rigidbody2D rb;
    Animator animator;

    public NpcManager NpcManager;

    private Vector2 movement;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (NpcManager != null && NpcManager.IsShopOpen())
        {
            movement = Vector2.zero;
            
        }
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
    }
}
