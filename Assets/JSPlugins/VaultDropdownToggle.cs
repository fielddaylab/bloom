
using System;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.SceneManagement;

using Zavala;

/// <summary>
/// Note: This class is designed to be used with the `vault-floating-dropdown` jslib pluggin. 
/// This script will remove the dropdown elment from the DOM after the target scene has been unloaded.
/// </summary>
public class VaultDropdownToggle : MonoBehaviour {
    public string sceneToDisplay = "TitleScreen";

    [DllImport("__Internal")]
    private static extern void DisableVaultButton();

    private void OnEnable() {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene current) {
        if(current.name != sceneToDisplay) return;
        Debug.Log("[VaultDropdownToggle] remove vault button");
        DisableVaultButton();
    }
    
    private void OnDisable() {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
}