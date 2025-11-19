using UnityEngine;

public class LocalVRRigRefs : MonoBehaviour
{
    [Header("Local XR Origin IK Target References")]
    public Transform HeadIKTarget;      // Main Camera
    public Transform LeftHandIKTarget;  // Left Controller
    public Transform RightHandIKTarget; // Right Controller
}