using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Data;
using UnityEngine;
using Zavala.Sim;
using Zavala.World;

namespace Zavala {
    [Serializable]
    public struct SimpleMeshConfig {
        static private readonly Material[] s_MaterialWorkArray = new Material[1];

        public Mesh Mesh;
        public Material Material;

        public SimpleMeshConfig(MeshRenderer renderer, MeshFilter filter) {
            if (renderer) {
                Material = renderer.sharedMaterial;
            } else {
                Material = null;
            }

            if (filter) {
                Mesh = filter.sharedMesh;
            } else {
                Mesh = null;
            }
        }

        public void Apply(MeshRenderer renderer, MeshFilter filter) {
            if (renderer) {
                s_MaterialWorkArray[0] = Material;
                renderer.sharedMaterials = s_MaterialWorkArray;
                s_MaterialWorkArray[0] = null;
                //renderer.sharedMaterial = Material;
            }

            if (filter) {
                filter.sharedMesh = Mesh;
            }
        }
    }

    [Serializable]
    public struct MultiMaterialMeshConfig {
        public Mesh Mesh;
        public Material[] Material;

        public MultiMaterialMeshConfig(MeshRenderer renderer, MeshFilter filter) {
            if (renderer) {
                Material = renderer.sharedMaterials;
            } else {
                Material = null;
            }

            if (filter) {
                Mesh = filter.sharedMesh;
            } else {
                Mesh = null;
            }
        }

        public void Apply(MeshRenderer renderer, MeshFilter filter) {
            if (renderer) {
                renderer.sharedMaterials = Material;
            }

            if (filter) {
                filter.sharedMesh = Mesh;
            }
        }
    }
}