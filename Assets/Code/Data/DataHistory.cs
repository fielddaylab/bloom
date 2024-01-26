using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Zavala.Data
{
    public class DataHistory
    {
        public DataHistory(int historyDepth) {
            MaxHistory = historyDepth;
            Net = new RingBuffer<int>(MaxHistory, RingBufferMode.Overwrite);
        }

        public int MaxHistory = 20; // how far back history is stored

        public int Pending;
        public int PrevPending;
        public RingBuffer<int> Net; // store show much P was produced/removed over the previous ticks

        public void AddPending(int pendingDelta) {
            PrevPending = Pending;
            Pending += pendingDelta;
            // Log.Msg("[DataHistory] AddPending {0}, PrevPending {1}, New Pending {2}", pendingDelta, PrevPending, Pending);
        }

        public int LastChange() {
            // Log.Msg("[DataHistory] LastChange {0} - {1} = {2}", Pending, PrevPending, Pending - PrevPending);
            return Pending - PrevPending;
        }

        public void ConvertPending() {
            // add any phosphorus changes that have been recorded
            Net.PushFront(Pending);
            // Log.Msg("[DataHistory] Converted to net: {0}", Pending);
            // reset phosphorus changes for next tick
            Pending = 0;
            PrevPending = 0;
        }

        /// <summary>
        /// Outputs average phosphorus produced/removed over "depth" # of ticks
        /// </summary>
        /// <param name="depth">number of ticks to analyze</param>
        /// <param name="avg"></param>
        /// <returns></returns>
        public bool TryGetAvg(int depth, out float avg) {
            if (depth > MaxHistory || depth > Net.Count) {
                avg = float.MaxValue;
                return false;
            }

            int sum = 0;
            int index = 0;
            foreach (int i in Net) {
                if (index >= depth) {
                    break;
                }
                sum += i;
                index++;
            }

            avg = (float)sum / Net.Count;
            return true;
        }

        /// <summary>
        /// Outputs total phosphorus produced/removed over "depth" # of ticks
        /// </summary>
        /// <param name="depth">number of ticks to analyze</param>
        /// <param name="avg"></param>
        /// <returns></returns>
        public bool TryGetTotal(int depth, out int total) {
            if (depth > MaxHistory || depth > Net.Count) {
                total = 0;
                return false;
            }

            int sum = 0;
            int index = 0;
            foreach (int i in Net) {
                if (index >= depth) {
                    break;
                }
                sum += i;
                index++;
            }

            total = sum;
            return true;
        }

    }

    public static class DataHistoryUtil { 
        static public void InitializeDataHistory(ref DataHistory[] histories, int size, int depth) {
            histories = new DataHistory[size];
            for (int i = 0; i < histories.Length; i++) {
                histories[i] = new DataHistory(depth);
            }
        }

        static public void Write(DataHistory[] histories, ref ByteWriter writer) {
            writer.Write((byte) histories.Length);

            for(int i = 0; i < histories.Length; i++) {
                DataHistory history = histories[i];
                writer.Write((byte) history.MaxHistory);

                writer.Write(history.Pending);
                writer.Write(history.PrevPending);

                writer.Write((byte) history.Net.Count);
                for(int j = 0; j < history.Net.Count; j++) {
                    writer.Write(history.Net[j]);
                }
            }
        }

        static public void Read(ref DataHistory[] histories, ref ByteReader reader) {
            int size = reader.Read<byte>();
            Array.Resize(ref histories, size);

            for(int i = 0; i < histories.Length; i++) {
                DataHistory history = histories[i];
                int depth = reader.Read<byte>();

                if (history == null) {
                    histories[i] = history = new DataHistory(depth);
                } else {
                    history.MaxHistory = depth;
                    history.Net.Clear();
                    history.Net.SetCapacity(depth);
                }

                history.Pending = reader.Read<int>();
                history.PrevPending = reader.Read<int>();

                int count = reader.Read<byte>();
                while(count-- > 0) {
                    history.Net.PushBack(reader.Read<int>());
                }
            }
        }
    }

}
