using BeauUtil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.Economy
{
    public class RequestSpriteVisual : MonoBehaviour
    {
        [HideInInspector] public ResourceBlock Resources;
        public SpriteRenderer BG;
        public SpriteRenderer ResourceImage;

        public Sprite ManureSprite, MFertilizerSprite, DFertilizerSprite, GrainSprite, MilkSprite;
    }
}