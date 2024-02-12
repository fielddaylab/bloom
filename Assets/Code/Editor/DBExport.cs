using BeauData;
using BeauUtil.Debugger;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.Editor {
    static public class DBExport {
        public const string ExportPath = "DBExport.json";

        private struct ActiveRange : ISerializedObject {
            public long Added;
            public long Updated;
            public long Deprecated;

            public void Serialize(Serializer ioSerializer) {
                ioSerializer.Serialize("added", ref Added);
                ioSerializer.Serialize("updated", ref Updated);
                ioSerializer.Serialize("deprecated", ref Deprecated);
            }

            private class Data : ISerializedObject {
                public List<RegionData> Regions = new List<RegionData>(RegionInfo.MaxRegions);
                // TODO: policy values? market/budget config?
                public void Serialize(Serializer ioSerializer) {
                    ioSerializer.ObjectArray("regions", ref Regions);
                }
            }
            private class RegionData : ISerializedObject {
                public string Id;
                public ActiveRange Date;

                public void Serialize(Serializer ioSerializer) {
                    ioSerializer.Serialize("id", ref Id);
                    ioSerializer.Object("date", ref Date);
                }
            }

            [MenuItem("Zavala/Export DB")]
            static public void Export() {

                Log.Error("[DBExport] ExportDB Unimplemented!");
                return;

                Data db;
                if (File.Exists(ExportPath)) {
                    db = Serializer.ReadFile<Data>(ExportPath);
                    Log.Msg("[DBExport] Read old database export from '{0}'", ExportPath);
                } else {
                    db = new Data();
                    Log.Msg("[DBExport] No database export found at '{0}': creating new", ExportPath);
                }

                long nowTS = DateTime.UtcNow.ToFileTimeUtc();
                
                // TODO: gather region data

                // TODO: set ActiveRange (added, updated, deprecated)
            }
        }
    }
}