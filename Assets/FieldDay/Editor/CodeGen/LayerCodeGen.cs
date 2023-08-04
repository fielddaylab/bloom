using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FieldDay.Editor {
    [CreateAssetMenu(menuName = "Field Day/Code Gen/Layers")]
    public class LayerCodeGen : CodeGenBase {
        public override void Generate(IndentedTextWriter writer, string ns) {
            writer.WriteUsing("System");
            writer.WriteLine();

            writer.OpenNamespace(ns);
            {
                writer.OpenStaticClass("LayerMasks");
                {
                    for (int i = 0; i < 32; i++) {
                        string layerName = LayerMask.LayerToName(i);

                        if (string.IsNullOrEmpty(layerName)) {
                            continue;
                        }

                        int index = i;
                        int mask = 1 << i;

                        string safeName = CodeGen.NameToSymbol(layerName);

                        writer.WriteComment("Layer " + index + ": " + layerName);

                        writer.WriteLine();
                        writer.Write("public const int ");
                        writer.Write(safeName);
                        writer.Write("_Index = ");
                        writer.Write(index);
                        writer.Write(";");

                        writer.WriteLine();
                        writer.Write("public const int ");
                        writer.Write(safeName);
                        writer.Write("_Mask = ");
                        writer.Write(mask);
                        writer.Write(";");
                    }
                }
                writer.CloseScope();

                writer.OpenStaticClass("SortingLayers");
                {
                    SortingLayer[] allLayers = SortingLayer.layers;
                    Array.Sort(allLayers, (a, b) => a.value.CompareTo(b.value));

                    foreach (var layer in allLayers) {
                        string safeName = CodeGen.NameToSymbol(layer.name);

                        writer.WriteComment("Layer " + layer.name);

                        writer.WriteLine();
                        writer.Write("public const int ");
                        writer.Write(safeName);
                        writer.Write(" = ");
                        writer.Write(layer.id);
                        writer.Write(";");
                    }
                }
                writer.CloseScope();

                writer.OpenStaticClass("UnityTags");
                {
                    string[] allTags = InternalEditorUtility.tags;

                    foreach (var tag in allTags) {
                        string safeName = CodeGen.NameToSymbol(tag);

                        writer.WriteComment("Tag " + tag);

                        writer.WriteLine();
                        writer.Write("public const string ");
                        writer.Write(safeName);
                        writer.Write(" = \"");
                        writer.Write(tag);
                        writer.Write("\";");
                    }
                }
                writer.CloseScope();
            }
            writer.CloseScope(ns);
        }
    }
}