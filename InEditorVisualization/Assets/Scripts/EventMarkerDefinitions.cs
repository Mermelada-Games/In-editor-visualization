using UnityEngine;

[System.Flags]
public enum EventCategory
{
    None                = 0,
    PlayerPosition      = 1 << 0,
    PlayerDeath         = 1 << 1,
    EnemyDefeated       = 1 << 2,
    ItemPickup          = 1 << 3,
    ItemInteract        = 1 << 4,
    ItemHeal            = 1 << 5
}

public enum MarkerShape
{
    Sphere, Cube, WireSphere, WireCube
}

[System.Serializable]
public class CategorySettings
{
    public EventCategory category;
    public bool isVisible = true;
    public bool showPath = false;
    public Color color = Color.white;
    public MarkerShape shape = MarkerShape.Sphere;
    public bool useSizeOverride = false;
    public float size = 1.0f;
}

[System.Serializable]
public struct WorldEvent
{
    public string id;
    public Vector3 position;
    public EventCategory category;
    public string message;
    public float timestamp;
}
