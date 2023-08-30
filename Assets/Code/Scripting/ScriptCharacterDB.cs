using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using FieldDay.SharedState;
using UnityEngine;

namespace Zavala.Scripting
{
    public class ScriptCharacterDB : SharedStateComponent
    {
        #region Inspector

        public ScriptCharacterDef[] CharDefs = null;
        [SerializeField, Required] private ScriptCharacterDef m_NullActorDefinition = null;
        [SerializeField, Required] private Sprite m_ErrorPortrait = null;

        [HideInInspector] public bool Constructed = false;
        public Dictionary<StringHash32, ScriptCharacterDef> IdMap;


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
        }

        static public void PreLookupConstruct(ScriptCharacterDB db) {
            db.IdMap = new Dictionary<StringHash32, ScriptCharacterDef>(db.CharDefs.Length);
        }

        static public void ConstructLookupForItem(ScriptCharacterDB db, ScriptCharacterDef def, int i) {
            StringHash32 id = def.name;
            Assert.False(db.IdMap.ContainsKey(id), "Duplicate {0} entry '{1}'", typeof(ScriptCharacterDef).Name, id);
            db.IdMap.Add(id, def);
        }

        #endregion // Internal
    }
}