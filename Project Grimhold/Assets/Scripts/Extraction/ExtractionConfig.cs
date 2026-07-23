using UnityEngine;

[CreateAssetMenu(
    fileName = "ExtractionConfig",
    menuName = "Game/Extraction/Extraction Config")]
public sealed class ExtractionConfig : ScriptableObject
{
    [Min(0.1f)]
    public float ExtractionDuration = 10f;
}