using Meta.XR.Editor.Tags;
using UnityEngine;

[CreateAssetMenu(fileName = "DartSettings", menuName = "Dart Game/Dart Settings")]
public class DartSettingsSO : ScriptableObject
{
    [Header("Physics")]
    public float ThrowPowerMultiplier = 1.5f;
    public float NetworkLifeTime = 10.0f; // 서버 다트 수명
    public float LocalLifeTime = 3.0f;   // 로컬 잔해 수명
}