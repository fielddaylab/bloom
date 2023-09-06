using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Cards {
    public class CardUI : MonoBehaviour
    {
        [HideInInspector] public int PolicyIndex; // Which severity index this card corresponds to (also index from left to right)

        /*
             if (Game.SharedState.Get<PolicyState>().SetPolicyByIndex(ButtonPolicy, policyIndex, regionIndex)) { }
          * 
          */
    }
}
