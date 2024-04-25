using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala;
using Zavala.Audio;
using Zavala.Data;
using Zavala.Sim;

public class UIPauseMenu : MonoBehaviour, IScenePreload
{

    [SerializeField] private Button m_QuitButton;
    [SerializeField] private SceneReference m_TitleScene;
    [SerializeField] private TMP_Text m_PlayerCode;
    [SerializeField] private Slider m_VolumeSlider;
    [SerializeField] private Button m_HelpToggle;

    IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
        UserSettings settings = Game.SharedState.Get<UserSettings>();
        m_PlayerCode.SetText(settings.PlayerCode);

        m_QuitButton.onClick.AddListener(HandleQuitButton);
        m_VolumeSlider.onValueChanged.AddListener(HandleSliderChanged);
        m_VolumeSlider.SetValueWithoutNotify(settings.MusicVolume);
        m_HelpToggle.onClick.AddListener(HandleHelpToggle);

        return null;
    }

    private void HandleQuitButton() {
        ZavalaGame.Scenes.LoadMainScene(m_TitleScene);
    }
    private void HandleSliderChanged(float val) {
        float oldVal = Game.SharedState.Get<UserSettings>().MusicVolume;
        float newVal = val / 10f;
        Game.SharedState.Get<UserSettings>().MusicVolume = newVal;
        ZavalaGame.Events.Dispatch(GameEvents.VolumeChanged, new ZoomVolData(oldVal, newVal, false));

    }

    private void HandleHelpToggle() {
        SimTimeState time = Find.State<SimTimeState>();
        if ((time.Paused & SimPauseFlags.Help) != 0) {
            SimTimeUtility.Resume(SimPauseFlags.Help, time);
        } else {
            SimTimeUtility.Pause(SimPauseFlags.Help, time);
        }
    }
}
