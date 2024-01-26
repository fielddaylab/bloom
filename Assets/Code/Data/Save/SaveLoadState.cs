using System;
using System.Collections;
using System.IO;
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