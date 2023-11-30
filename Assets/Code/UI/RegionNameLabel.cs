using FieldDay;
using TMPro;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.UI {
    public class RegionNameLabel : MonoBehaviour {
        public TMP_Text Text;

        private void Start() {
            Game.Events.Register(GameEvents.RegionSwitched, OnRegionSwitched)
                .Register(SimGridState.Event_RegionUpdated, OnRegionSwitched);
        }

        private void OnRegionSwitched() {
            Text.SetText(Loc.Find(RegionUtility.GetNameLong((ushort) ZavalaGame.SimGrid.CurrRegionIndex)));
        }
    }
}