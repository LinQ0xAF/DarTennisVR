using Unity.Netcode;
using UnityEngine;

public class PlayerDartState : NetworkBehaviour
{
    public NetworkVariable<int> OffHandDartCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsHoldingDart = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Dummy Visuals")]
    public GameObject[] OffHandDartMeshes;
    public GameObject HandDartMesh;

    [SerializeField] private Animator NetworkAvatarAnimator;

    // Parameter hashes (performance optimization)
    private int _GripHash;
    private int _IsHoldingHash;

    public override void OnNetworkSpawn()
    {
        OffHandDartCount.OnValueChanged += (p, c) => UpdateVisuals();
        IsHoldingDart.OnValueChanged += (p, c) => UpdateVisuals();
        UpdateVisuals();

        // cache parameter hashes for performance
        _GripHash = Animator.StringToHash("Grip_L");            // Off hand fixed to Left side
        _IsHoldingHash = Animator.StringToHash("IsHolding_R");  // Main hand fixed to Right side
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
        if (NetworkAvatarAnimator != null)
        {
            NetworkAvatarAnimator.SetFloat(_GripHash, OffHandDartCount.Value > 0 ? 1f : 0f);        // Off hand grip (simplized)
            NetworkAvatarAnimator.SetBool(_IsHoldingHash, IsHoldingDart.Value);                    // Main hand holding state
        }
        for (int i = 0; i < OffHandDartMeshes.Length; i++) 
            OffHandDartMeshes[i].SetActive(i < OffHandDartCount.Value);
            
        if (HandDartMesh) HandDartMesh.SetActive(IsHoldingDart.Value);
    }
}