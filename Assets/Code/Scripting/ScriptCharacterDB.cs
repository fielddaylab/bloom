using System;
using System.Collections.Generic;
//using System.Linq;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.Scripting
{
    public class ScriptCharacterDB : SharedStateComponent
    {
        #region Inspector

        public ScriptCharacterDef[] CharDefs = null;
        public ScriptCharacterRemap[] CharRemaps = null;
        [SerializeField, Required] private ScriptCharacterDef m_NullActorDefinition = null;
        [SerializeField, Required] private Sprite m_ErrorPortrait = null;

        [HideInInspector] public bool Constructed = false;
        public Dictionary<StringHash32, ScriptCharacterDef> IdMap;
        public Dictionary<StringHash32, ScriptCharacterRemap> LocalIdRemap;

        #endregion // Inspector

        public ScriptCharacterDef Default() { return m_NullActorDefinition; }
        public Sprite ErrorPortrait() { return m_ErrorPortrait; }
        public ScriptCharacterDef NullValue() { return m_NullActorDefinition; }

    }

    static public class ScriptCharacterDBUtility
    {
        #region Lookup

        static public ScriptCharacterDef Get(ScriptCharacterDB db, StringHash32 inId) {
            EnsureCreated(db);

            if (inId.IsEmpty) {
                return NullValue();
            }

            ScriptCharacterDef def;
            db.IdMap.TryGetValue(inId, out def);
            if (object.ReferenceEquals(def, null)) {
                Assert.NotNull(def, "Could not find {0} with id '{1}'", typeof(ScriptCharacterDef).Name, inId);
            }
            return def;
        }

        static public ScriptCharacterDef GetRemapped(ScriptCharacterDB db, StringHash32 inId) {
            EnsureCreated(db);

            if (inId.IsEmpty) {
                return NullValue();
            }

            // TODO: is it possible to check if there is a "local:alertRegion" var in the leaf script and use that instead?
            int regionKey = (int)Game.SharedState.Get<SimGridState>().CurrRegionIndex;
            
            ScriptCharacterRemap remap;
            db.LocalIdRemap.TryGetValue(inId, out remap);
            if (object.ReferenceEquals(remap, null)) {
                Assert.NotNull(remap, "Could not find {0} with id '{1}'", typeof(ScriptCharacterRemap).Name, inId);
            }

            StringHash32 remappedId = null;
            for (int i = 0; i < remap.RemapTo.Length; i++) {
                if (remap.RemapTo[i].Region == (RegionId)regionKey) {
                    remappedId = (remap.RemapTo[i].CharDef).name;
                }
            }

            return Get(db, remappedId);
        }

        static public IEnumerable<ScriptCharacterDef> Filter(ScriptCharacterDB db, Predicate<ScriptCharacterDef> inPredicate) {
            if (inPredicate == null) {
                throw new ArgumentNullException("inPredicate");
            }

            for (int i = 0; i < db.CharDefs.Length; ++i) {
                ScriptCharacterDef obj = db.CharDefs[i];
                if (inPredicate(obj)) {
                    yield return obj;
                }
            }
        }

        static public ScriptCharacterDef NullValue() { return null; }

        #endregion // Lookup

        #region Internal

        static public void Initialize(ScriptCharacterDB db) {
            EnsureCreated(db);
        }

        static public void EnsureCreated(ScriptCharacterDB db) {
            if (!db.Constructed) {
                db.Constructed = true;
                ConstructLookups(db);
            }
        }

        static public void ConstructLookups(ScriptCharacterDB db) {
            PreLookupConstruct(db);

            for (int i = 0; i < db.CharDefs.Length; ++i) {
                ScriptCharacterDef def = db.CharDefs[i];
                ConstructLookupForItem(db, def, i);
            }

            for (int i = 0; i < db.CharRemaps.Length; ++i) {
                ScriptCharacterRemap remap = db.CharRemaps[i];
                ConstructLookupForRemapItem(db, remap, i);
            }
        }

        static public void PreLookupConstruct(ScriptCharacterDB db) {
            db.IdMap = new Dictionary<StringHash32, ScriptCharacterDef>(db.CharDefs.Length);
            db.LocalIdRemap = new Dictionary<StringHash32, ScriptCharacterRemap>(db.CharRemaps.Length);
        }

        static public void ConstructLookupForItem(ScriptCharacterDB db, ScriptCharacterDef def, int i) {
            StringHash32 id = def.name;
            Assert.False(db.IdMap.ContainsKey(id), "Duplicate {0} entry '{1}'", typeof(ScriptCharacterDef).Name, id);
            db.IdMap.Add(id, def);
        }

        static public void ConstructLookupForRemapItem(ScriptCharacterDB db, ScriptCharacterRemap remap, int i) {
            StringHash32 id = remap.name;
            Assert.False(db.LocalIdRemap.ContainsKey(id), "Duplicate {0} entry '{1}'", typeof(ScriptCharacterRemap).Name, id);
            db.LocalIdRemap.Add(id, remap);
        }

        #endregion // Internal
    }
}