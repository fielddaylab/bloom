using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Sim
{
    public class PhosphorusSkimmer : BatchedComponent
    {
        public float AlgaeSkimAmt;
        public int PhosSkimAmt;
        public SkimmerType Type;
    }

    public enum SkimmerType {
        Algae,
        Dredge
    }
}