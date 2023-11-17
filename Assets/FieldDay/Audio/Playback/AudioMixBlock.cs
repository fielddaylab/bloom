using System;
using System.Runtime.InteropServices;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio mixer properties.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct AudioMixBlock {
        public float Factor;

        public AudioPropertyBlock Master;
        public AudioPropertyBlock Bus0;
        public AudioPropertyBlock Bus1;
        public AudioPropertyBlock Bus2;
        public AudioPropertyBlock Bus3;
        public AudioPropertyBlock Bus4;
        public AudioPropertyBlock Bus5;
        public AudioPropertyBlock Bus6;
        public AudioPropertyBlock Bus7;
        public AudioPropertyBlock Bus8;
        public AudioPropertyBlock Bus9;
        public AudioPropertyBlock Bus10;
        public AudioPropertyBlock Bus11;
        public AudioPropertyBlock Bus12;
        public AudioPropertyBlock Bus13;
        public AudioPropertyBlock Bus14;

        /// <summary>
        /// Gets/sets the properties for the given bus.
        /// </summary
        public AudioPropertyBlock this[int busId] {
            get {
                unsafe {
                    fixed(AudioPropertyBlock* ptr = &Master) {
                        return *(ptr + busId);
                    }
                }
            }
            set {
                unsafe {
                    fixed (AudioPropertyBlock* ptr = &Master) {
                        *(ptr + busId) = value;
                    }
                }
            }
        }
    }
}