var NativeWebAudio = {
    NativeWebAudio_WakeUp: function() {
        if (WEBAudio.audioContext.state === "suspended") {
            WEBAudio.audioContext.resume();
            return true;
        } else {
            return false;
        }
    }
};

mergeInto(LibraryManager.library, NativeWebAudio);