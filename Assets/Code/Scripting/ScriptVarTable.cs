using System;
using BeauPools;
using BeauUtil;
using BeauUtil.Variants;

namespace FieldDay.Scripting {
    /// <summary>
    /// Temporary variable table.
    /// </summary>
    public struct TempVarTable : IDisposable {
        private readonly TempAlloc<VariantTable> m_Table;
        
        internal TempVarTable(TempAlloc<VariantTable> tempTable) {
            m_Table = tempTable;
        }

        public void Set(StringHash32 id, Variant value) {
            m_Table.Object?.Set(id, value);
        }

        public void Clear() {
            m_Table.Object?.Clear();
        }

        public void Dispose() {
            m_Table.Dispose();
        }

        static public implicit operator VariantTable(TempVarTable temp) {
            return temp.m_Table.Object;
        }

        static public TempVarTable Alloc() {
            ScriptRuntimeState state = Game.SharedState.Get<ScriptRuntimeState>();
            var temp = state.TablePool.TempAlloc();
            temp.Object.Name = "temp";
            return new TempVarTable(temp);
        }
    }
}