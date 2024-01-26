using System;
using System.Collections;
using System.IO;
using BeauRoutine;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;

namespace Zavala.Data {
    public class SaveLoadState : SharedStateComponent {
        public Routine Operation;
    }

    static public class SaveUtility {
        static public void Save() {
            var save = Game.SharedState.Get<SaveLoadState>();
            if (save.Operation) {
                Log.Error("[SaveUtility] Save operation is ongoing");
                return;
            }

            save.Operation = Routine.Start(save, SaveRoutine());
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
    }
}