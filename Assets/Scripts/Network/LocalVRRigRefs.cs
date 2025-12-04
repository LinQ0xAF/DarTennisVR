using UnityEngine;

public class LocalVRRigRefs : MonoBehaviour
{
    [Header("Local XR Origin IK Target References")]
    public Transform HeadIKTarget;      // Local Head IK Target
    public Transform LeftHandIKTarget;  // Left Controller
    public Transform RightHandIKTarget; // Right Controller
    // public Transform MainCameraTransform; // Main Camera (HMD) Transform
}