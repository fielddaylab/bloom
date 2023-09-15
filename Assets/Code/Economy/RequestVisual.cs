using BeauUtil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.Economy
{
    public class RequestVisual : MonoBehaviour
    {
        [HideInInspector] public ResourceBlock Resources;
        public Image BG;
        public Image ResourceImage;

        public Sprite ManureSprite, MFertilizerSprite, DFertilizerSprite, GrainSprite, MilkSprite;
    }
}