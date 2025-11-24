using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Events/Dart Spawn Channel")]
public class DartSpawnChannelSO : ScriptableObject
{
    // 요청자ID, 손의 위치, 회전, 어느 손인지(True=Right, False=Left)
    public event Action<ulong, Vector3, Quaternion, bool, int> OnSpawnRequested;

    public void RaiseEvent(ulong clientId, Vector3 pos, Quaternion rot, bool isRightHand, int socketIndex)
    {
        OnSpawnRequested?.Invoke(clientId, pos, rot, isRightHand, socketIndex);
    }
}