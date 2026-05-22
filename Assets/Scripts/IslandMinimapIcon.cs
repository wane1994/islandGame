using UnityEngine;

public enum IslandMinimapIconType
{
    Player,
    Crystal,
    Enemy,
    Beacon
}

public sealed class IslandMinimapIcon : MonoBehaviour
{
    [SerializeField] private IslandMinimapIconType iconType;

    public IslandMinimapIconType IconType => iconType;

    public void Configure(IslandMinimapIconType type)
    {
        iconType = type;
    }
}
