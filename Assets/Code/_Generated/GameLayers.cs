using System;


static public class LayerMasks {
    
    // Layer 0: Default
    public const int Default_Index = 0;
    public const int Default_Mask = 1;
    // Layer 1: TransparentFX
    public const int TransparentFX_Index = 1;
    public const int TransparentFX_Mask = 2;
    // Layer 2: Ignore Raycast
    public const int IgnoreRaycast_Index = 2;
    public const int IgnoreRaycast_Mask = 4;
    // Layer 4: Water
    public const int Water_Index = 4;
    public const int Water_Mask = 16;
    // Layer 5: UI
    public const int UI_Index = 5;
    public const int UI_Mask = 32;
    // Layer 6: HexTile
    public const int HexTile_Index = 6;
    public const int HexTile_Mask = 64;
    // Layer 7: Building
    public const int Building_Index = 7;
    public const int Building_Mask = 128;
    // Layer 8: Road
    public const int Road_Index = 8;
    public const int Road_Mask = 256;
    // Layer 9: Phosphorus
    public const int Phosphorus_Index = 9;
    public const int Phosphorus_Mask = 512;
    // Layer 10: Blueprints
    public const int Blueprints_Index = 10;
    public const int Blueprints_Mask = 1024;
    // Layer 31: WorldUI
    public const int WorldUI_Index = 31;
    public const int WorldUI_Mask = -2147483648;
}
static public class SortingLayers {
    
    // Layer Default
    public const int Default = 0;
    // Layer Overlay
    public const int Overlay = -2046748057;
}
static public class UnityTags {
    
    // Tag Untagged
    public const string Untagged = "Untagged";
    // Tag Respawn
    public const string Respawn = "Respawn";
    // Tag Finish
    public const string Finish = "Finish";
    // Tag EditorOnly
    public const string EditorOnly = "EditorOnly";
    // Tag MainCamera
    public const string MainCamera = "MainCamera";
    // Tag Player
    public const string Player = "Player";
    // Tag GameController
    public const string GameController = "GameController";
    // Tag PlayerPlaced
    public const string PlayerPlaced = "PlayerPlaced";
    // Tag UICamera
    public const string UICamera = "UICamera";
}