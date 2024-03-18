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

public class UIPauseMenu : MonoBehaviour, IScenePreload
{

    [SerializeField] private Button m_QuitButton;
    [SerializeField] private SceneReference m_TitleScene;
    [SerializeField] private TMP_Text m_PlayerCode;
    [SerializeField] private Slider m_VolumeSlider;

    IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
        UserSettings settings = Game.SharedState.Get<UserSettings>();
        m_PlayerCode.SetText(settings.PlayerCode);

        m_QuitButton.onClick.AddListener(HandleQuitButton);
        m_VolumeSlider.onValueChanged.AddListener(HandleSliderChanged);
        m_VolumeSlider.SetValueWithoutNotify(settings.MusicVolume);

        return null;
    }

    private void HandleQuitButton() {
        ZavalaGame.Scenes.LoadMainScene(m_TitleScene);
    }
    private void HandleSliderChanged(float val) {
        Game.SharedState.Get<UserSettings>().MusicVolume = val / 10f;
        MusicUtility.SetVolume(val/10f);
    }
}
