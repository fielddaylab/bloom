using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Data
{
    public class DataHistory
    {
        public DataHistory(int historyDepth) {
            MaxHistory = historyDepth;
            Net = new LinkedList<int>();
        }

        public int MaxHistory = 20; // how far back history is stored

        public int Pending;
        public LinkedList<int> Net = new LinkedList<int>(); // store show much P was produced/removed over the previous ticks

        public void AddPending(int pendingDelta) {
            Pending += pendingDelta;
        }

        public void ConvertPending() {
            // add any phosphorus changes that have been recorded
            Net.AddFirst(Pending);

            // reset phosphorus changes for next tick
            Pending = 0;

            // Remove any history that is older than we care about keeping track of
            if (Net.Count > MaxHistory) {
                Net.RemoveLast();
            }
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
    }

}
