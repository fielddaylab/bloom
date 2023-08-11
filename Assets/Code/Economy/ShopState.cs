using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SharedStateInitOrder(10)]
public sealed class ShopState : SharedStateComponent
{
    public UIShop ShopUI;
}
