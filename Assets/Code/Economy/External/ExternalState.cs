using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Economy;

namespace Zavala.Economy {
    public class ExternalState : SharedStateComponent
    {
        [NonSerialized] public ResourceSupplier ExternalSupplier; // The supplier selling commercial fertilizer
        [NonSerialized] public ResourceSupplierProxy ExternalDepot; // The depot connecting external imports to individual purchasers
    }
}