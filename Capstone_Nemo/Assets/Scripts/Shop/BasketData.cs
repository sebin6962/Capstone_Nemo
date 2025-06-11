using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopBasketData
{
    public ShopData item;
    public int quantity;
    public int TotalPrice => item.price * quantity;
    
}
   