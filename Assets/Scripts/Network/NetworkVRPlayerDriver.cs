using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
// XR 관련 네임스페이스 (프로젝트 설정에 따라 다를 수 있음. 보통 필요 없음)

public class NetworkVRPlayerDriver : NetworkBehaviour
{

    [Header("Network IK Target Transforms")]
    public Transform NetHeadIKTarget;
    public Transform NetLeftHandIKTarget;
    public Transform NetRightHandIKTarget;

    // cached XR Origin reference for local player
    private XROrigin _LocalXROrigin;

    [Header("Local Source(XR Origin) IK Target Transforms")]
    // will Automatically assigned
    private Transform _LocalHeadIKTarget;
    private Transform _LocalLeftHandIKTarget;
    private Transform _LocalRightHandIKTarget;

    public override void OnNetworkSpawn()
    {
        // connect to local XR Rig targets for own avatar
        if (IsOwner)
        {
            // Find XR Origin in the scene
            _LocalXROrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (_LocalXROrigin != null)
            {
                // find head, left and right hand IK Targets by name
                foreach (var t in _LocalXROrigin.GetComponentsInChildren<Transform>())
                {
                    if (t.name.Contains("Head") && t.name.Contains("Target")) _LocalHeadIKTarget = t;
                    if (t.name.Contains("LeftArm") && t.name.Contains("Target")) _LocalLeftHandIKTarget = t;
                    if (t.name.Contains("RightArm") && t.name.Contains("Target")) _LocalRightHandIKTarget = t;
                }
            }
            
            // set own avatar meshes invisible
            SetMeshesVisible(false); 
        }
        else
        {
            // set other players' avatar meshes visible
            SetMeshesVisible(true);
        }
    }

    void Update()
    {
        if (IsOwner && _LocalXROrigin != null)
        {
            // 1. Avatar Root synchronization directly by world transform
            transform.SetPositionAndRotation(_LocalXROrigin.transform.position, _LocalXROrigin.transform.rotation);

            // 2. Head synchronization (core modification!)
            if (_LocalHeadIKTarget)
            {
                // Calculate relative position based on XR Origin (root)
                NetHeadIKTarget.localPosition = _LocalXROrigin.transform.InverseTransformPoint(_LocalHeadIKTarget.position);
                NetHeadIKTarget.localRotation = Quaternion.Inverse(_LocalXROrigin.transform.rotation) * _LocalHeadIKTarget.rotation;
            }

            // 3. both Hands synchronization 
            if (_LocalLeftHandIKTarget)
            {   // Calculate relative position based on XR Origin (root)
                NetLeftHandIKTarget.localPosition = _LocalXROrigin.transform.InverseTransformPoint(_LocalLeftHandIKTarget.position);
                NetLeftHandIKTarget.localRotation = Quaternion.Inverse(_LocalXROrigin.transform.rotation) * _LocalLeftHandIKTarget.rotation;
            }
            if (_LocalRightHandIKTarget)
            {   // Calculate relative position based on XR Origin (root)
                NetRightHandIKTarget.localPosition = _LocalXROrigin.transform.InverseTransformPoint(_LocalRightHandIKTarget.position);
                NetRightHandIKTarget.localRotation = Quaternion.Inverse(_LocalXROrigin.transform.rotation) * _LocalRightHandIKTarget.rotation;
            }
        }
    }

    void SetMeshesVisible(bool visible)
    {
        foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
        {
            renderer.enabled = visible;
        }
    }
}