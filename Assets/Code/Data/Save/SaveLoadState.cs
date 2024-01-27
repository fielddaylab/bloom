using System;
using System.Collections;
using System.IO;
using BeauData;
using BeauRoutine;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.SharedState;

namespace Zavala.Data {
    public class SaveLoadState : SharedStateComponent {
        public Routine Operation;
    }

    static public class SaveUtility {
        static public void Save() {
            var save = Game.SharedState.Get<SaveLoadState>();
            if (save.Operation) {
                Log.Error("[SaveUtility] Save/load operation is ongoing");
                return;
            }

            save.Operation = Routine.Start(save, SaveRoutine());
        }

        static public void Reload() {
            var save = Game.SharedState.Get<SaveLoadState>();
            if (save.Operation) {
                Log.Error("[SaveUtility] Save/load operation is ongoing");
                return;
            }

            save.Operation = Routine.Start(save, ReloadRoutine(true));
        }

        static public void LoadFromServer(string inUserId) {
            var save = Game.SharedState.Get<SaveLoadState>();
            if (save.Operation) {
                Log.Error("[SaveUtility] Save/load operation is ongoing");
                return;
            }

            save.Operation = Routine.Start(save, LoadFromServerRoutine(inUserId));
        }

        static private IEnumerator SaveRoutine() {
            yield return null;
            yield return null;
            ZavalaGame.SaveBuffer.Write();
            ZavalaGame.SaveBuffer.EncodeToBase64();

#if UNITY_EDITOR
            WriteToFileSystem();
#endif // UNITY_EDITOR
        }

#if UNITY_EDITOR
        static unsafe private void WriteToFileSystem() {
            var chars = ZavalaGame.SaveBuffer.GetCurrentBase64();
            Directory.CreateDirectory("Saves");
            using (var str = File.Open("Saves/" + DateTime.Now.ToFileTime().ToString() + ".bin", FileMode.Create)) {
                using (var stream = new StreamWriter(str)) {
                    var charsAsSys = new ReadOnlySpan<char>(chars.Ptr, chars.Length);
                    stream.Write(charsAsSys);
                }
            }
        }
#endif // UNITY_EDITOR

        static private void WriteToRemoteSave() {
            // get save data
            var chars = ZavalaGame.SaveBuffer.GetCurrentBase64();

            // write save data to string

            // try to send save data to server - just copied from aqualab

            /*
            int attempts = (int)(m_SaveRetryCount + 1);
            int retryCount = 0;
            while (attempts > 0) {
                using (var future = Future.Create())
                using (var saveRequest = OGD.GameState.PushState(m_ProfileName, saveData, future.Complete, (r) => future.Fail(r), retryCount)) {
                    yield return future;

                    if (future.IsComplete()) {
                        // DebugService.Log(LogMask.DataService, "[DataService] Saved to server!");
                        break;
                    } else {
                        attempts--;
                        Log.Warn("[DataService] Failed to save to server: {0}", future.GetFailure().Object);
                        if (attempts > 0) {
                            Log.Warn("[DataService] Retrying server save...", attempts);
                            // Services.Events.Dispatch(GameEvents.ProfileSaveError);
                            yield return m_SaveRetryDelay;
                            ++retryCount;
                        } else {
                            Log.Error("[DataService] Server save failed after {0} attempts", m_SaveRetryCount + 1);
                        }
                    }
                }
            }
            */

        }

        static private IEnumerator ReloadRoutine(bool waitForCutsceneClose) {
            if (waitForCutsceneClose) {
                var scriptRuntime = ScriptUtility.Runtime;
                while(scriptRuntime.Cutscene.IsRunning()) {
                    yield return null;
                }
            }
            yield return null;
            if (ZavalaGame.SaveBuffer.HasSave) {
                ZavalaGame.SaveBuffer.Read();
            }
            Game.Scenes.ReloadMainScene();
        }

        static private IEnumerator LoadFromServerRoutine(string inUserCode) {

            using (var future = Future.Create<string>())
            using (var request = OGD.GameState.RequestLatestState(inUserCode, future.Complete, (r) => future.Fail(r), 0)) {
                // stub
                yield return null;
                /*
                yield return future;

                if (future.IsComplete()) {
                    // DebugService.Log(LogMask.DataService, "[DataService] Save with profile name {0} found on server!", inUserCode);
                    // save data here in terms of an ISerialiedObject
                    SaveData serverData = null;
                    bool bSuccess;
                    using (Profiling.Time("reading save data from server")) {
                        bSuccess = Serializer.Read(ref serverData, future.Get());
                    }

                    if (!bSuccess) {
                        UnityEngine.Debug.LogErrorFormat("[DataService] Server profile '{0}' could not be read...", inUserCode);
                        ioFuture.Fail(DeserializeError);
                    } else {
                        ioFuture.Complete(serverData);
                    }
                } else {
                    UnityEngine.Debug.LogErrorFormat("[DataService] Failed to find profile on server: {0}", future.GetFailure());
                    ioFuture.Fail(future.GetFailure());
                }
                */
            }
        }

        [DebugMenuFactory]
        static private DMInfo SaveDebugMenu() {
            DMInfo info = new DMInfo("Save", 4);
            info.AddButton("Write Current to Memory", () => SaveUtility.Save());
            info.AddButton("Read Current from Memory", () => {
                SaveUtility.Reload();
            }, () => ZavalaGame.SaveBuffer.HasSave);
            return info;
        }
    }
}