using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.UI;

namespace Zavala.Economy
{
    [SharedStateInitOrder(10)]
    public sealed class ShopState : SharedStateComponent
    {
        public UIShop ShopUI;
    }
}
