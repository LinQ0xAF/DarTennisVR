using Unity.Netcode;
using UnityEngine;

public class PlayerDartState : NetworkBehaviour
{
    // 손에 다트 들고 있는지
    public NetworkVariable<bool> IsHoldingDart = new NetworkVariable<bool>(false, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Dummy Visuals")]
    public GameObject HandDartMesh; // 껍데기 모델

    public override void OnNetworkSpawn()
    {
        IsHoldingDart.OnValueChanged += (prev, curr) => UpdateVisuals();
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // 나는 내 XRI 다트를 보니까 껍데기는 무조건 숨김
        if (IsOwner)
        {
            if(HandDartMesh) HandDartMesh.SetActive(false);
            return;
        }

        // 남들은 변수 값에 따라 껍데기 On/Off
        if (HandDartMesh) HandDartMesh.SetActive(IsHoldingDart.Value);
    }
}