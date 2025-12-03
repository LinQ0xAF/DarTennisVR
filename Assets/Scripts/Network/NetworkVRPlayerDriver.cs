using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;

public class NetworkVRPlayerDriver : NetworkBehaviour
{
    [Header("Network IK Target Transforms")]
    public Transform NetHeadIKTarget;
    public Transform NetLeftHandIKTarget;
    public Transform NetRightHandIKTarget;
    // public Transform NetMainCameraTransform;

    [Header("Network Avatar Meshes")]
    [SerializeField] public SkinnedMeshRenderer[] NetworkAvatarMeshes;

    // cached XR Origin reference for local player
    private XROrigin _LocalXROrigin;
    private LocalVRRigRefs _LocalRigRefs;

    [Header("Local Source(XR Origin) IK Target Transforms")]
    // will Automatically assigned
    private Transform _LocalHeadIKTarget;
    private Transform _LocalLeftHandIKTarget;
    private Transform _LocalRightHandIKTarget;
    // private Transform _LocalMainCameraTransform;

    public override void OnNetworkSpawn()
    {
        // connect to local XR Rig targets for own avatar
        if (IsOwner)
        {
            // Find XR Origin in the scene
            _LocalXROrigin = FindFirstObjectByType<XROrigin>();
            if (_LocalXROrigin != null)
            {
                _LocalRigRefs = _LocalXROrigin.GetComponent<LocalVRRigRefs>();
                if (_LocalRigRefs != null)
                {
                    _LocalHeadIKTarget = _LocalRigRefs.HeadIKTarget;
                    _LocalLeftHandIKTarget = _LocalRigRefs.LeftHandIKTarget;
                    _LocalRightHandIKTarget = _LocalRigRefs.RightHandIKTarget;
                    // _LocalMainCameraTransform = _LocalRigRefs.MainCameraTransform;
                }
            }
            
            // set own network avatar meshes invisible
            SetMeshesVisible(false); 
        }
        else
        {
            // set other players' network avatar meshes visible
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
            // if (_LocalMainCameraTransform)
            // {
            //     // Calculate relative position based on XR Origin (root)
            //     NetMainCameraTransform.localPosition = _LocalXROrigin.transform.InverseTransformPoint(_LocalMainCameraTransform.position);
            //     NetMainCameraTransform.localRotation = Quaternion.Inverse(_LocalXROrigin.transform.rotation) * _LocalMainCameraTransform.rotation;
            // }

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

    void SetMeshesVisible(bool isVisible)
    {
        foreach (var renderer in NetworkAvatarMeshes)
        {
            renderer.enabled = isVisible;
        }
    }
}