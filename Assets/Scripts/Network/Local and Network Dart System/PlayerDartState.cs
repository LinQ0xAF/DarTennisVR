using Unity.Netcode;
using UnityEngine;

public class PlayerDartState : NetworkBehaviour
{
    public NetworkVariable<int> OffHandDartCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsHoldingDart = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Dummy Visuals")]
    public GameObject[] OffHandDartMeshes;
    public GameObject HandDartMesh;

    public override void OnNetworkSpawn()
    {
        OffHandDartCount.OnValueChanged += (p, c) => UpdateVisuals();
        IsHoldingDart.OnValueChanged += (p, c) => UpdateVisuals();
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (IsOwner) // 나는 로컬 XRI를 보니까 껍데기는 끔
        {
            foreach (var m in OffHandDartMeshes) m.SetActive(false);
            if (HandDartMesh) HandDartMesh.SetActive(false);
            return;
        }

        // 상대방 처리
        for (int i = 0; i < OffHandDartMeshes.Length; i++) 
            OffHandDartMeshes[i].SetActive(i < OffHandDartCount.Value);
            
        if (HandDartMesh) HandDartMesh.SetActive(IsHoldingDart.Value);
    }
}