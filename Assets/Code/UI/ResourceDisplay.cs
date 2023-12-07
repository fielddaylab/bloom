using BeauUtil;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Zavala;
using Zavala.Economy;
using Zavala.World;

public class ResourceDisplay : MonoBehaviour {
    public ResourceId ResourceType;
    // private int Count;

    public TMP_Text CountText;
    public SpriteRenderer Renderer;
    public ColorGroup ColorGroup;
    public MeshRenderer Mesh;

    public void Start() {
        Camera cam = ZavalaGame.SharedState.Get<SimWorldCamera>().Camera;
        Renderer.transform.rotation = cam.transform.rotation;
    }
    public void SetCount(int count) {
        CountText.text = count.ToStringLookup();
        ColorGroup.Color = count == 0 ? Color.clear : Color.white;
        if (Mesh != null) Mesh.enabled = (count != 0);
    }

}
