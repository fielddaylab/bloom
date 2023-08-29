using System.Collections;
using BeauRoutine;
using BeauUtil;
using FieldDay.Systems;
using Leaf;

namespace FieldDay.Scripting {
    [SysUpdate(GameLoopPhase.PreUpdate, 100)]
    public sealed class ScriptDatabaseLoadSystem : SharedStateSystemBehaviour<ScriptDatabase> {
        public override void ProcessWork(float deltaTime) {
            if (HandleCurrentLoad(m_State)) {
                return;
            }

            if (StartLoadingOne(m_State)) {
                return;
            }

            UnloadScripts(m_State);
        }

        static private bool HandleCurrentLoad(ScriptDatabase db) {
            if (db.CurrentLoadAsset == null) {
                return false;
            }

            if (db.CurrentLoadHandle.IsRunning()) {
                return true;
            }

            ScriptDatabaseUtility.RegisterPackage(db, db.CurrentLoadPackage, db.CurrentLoadAsset);

            db.CurrentLoadAsset = null;
            db.CurrentLoadHandle = default;
            db.CurrentLoadPackage = null;
            return true;
        }

        static private bool StartLoadingOne(ScriptDatabase db) {
            if (db.LoadQueue.TryPopFront(out LeafAsset asset)) {
                db.CurrentLoadAsset = asset;
                db.CurrentLoadPackage = LeafAsset.CompileAsync(asset, ScriptNodePackage.Parser.Instance, out IEnumerator loader);
                db.CurrentLoadHandle = Async.Schedule(loader, AsyncFlags.HighPriority);
                return true;
            }

            return false;
        }

        static private void UnloadScripts(ScriptDatabase db) {
            for(int i = db.UnloadQueue.Count - 1; i >= 0; i--) {
                LeafAsset asset = db.UnloadQueue[i];
                if (!db.LoadedSourceAssetMap.TryGetValue(asset, out ScriptNodePackage package)) {
                    db.UnloadQueue.FastRemoveAt(i);
                    continue;
                }

                if (package.IsReferenced()) {
                    continue;
                }

                ScriptDatabaseUtility.DeregisterPackage(db, package);
                package.Clear();
                db.UnloadQueue.FastRemoveAt(i);
            }
        }
    }
}