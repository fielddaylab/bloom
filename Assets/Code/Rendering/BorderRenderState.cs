using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Rendering;
using Zavala.Sim;

namespace Zavala.World
{
    public class BorderRenderState : SharedStateComponent
    {
        [NonSerialized] public MeshData16<TileVertexFormat> MeshGeneratorA = new MeshData16<TileVertexFormat>(256);
        [NonSerialized] public MeshData16<TileVertexFormat> MeshGeneratorB = new MeshData16<TileVertexFormat>(256);
        [NonSerialized] public RingBuffer<ushort> RegionQueue = new RingBuffer<ushort>(2);

        [Header("Outline Parameters")]
        public float OutlineThickness = 0.2f;
        public float ThickOutlineThickness = 5;

        [Header("Outline Rendering")]
        public MeshFilter OutlineFilter;
        public MeshRenderer OutlineRenderer;

        [Header("Blueprint Outline Rendering")]
        public MeshFilter LockedOutlineFilter;
        public MeshRenderer LockedOutlineRenderer;
        public Material LockedOutlineMaterial;
    }
}