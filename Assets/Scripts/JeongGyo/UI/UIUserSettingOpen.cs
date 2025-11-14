using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
public class UIUserSettingOpen : MonoBehaviour
{

    [SerializeField]
    bool UserSettingUIActive = false;
    private ControllerInputActionManager controllerInputActionManager;
    private InputActionReference m_OpenUserSettingUI;
    const float distance = 1.5f;
    const float verticalOffset = 1f;

    Camera cam;
    void Start()
    {   
        controllerInputActionManager = FindFirstObjectByType<ControllerInputActionManager>();
        m_OpenUserSettingUI = controllerInputActionManager.m_OpenUserSettingUI;
        cam = Camera.main;

        InputAction openUserSettingAction = m_OpenUserSettingUI.action;

        if (openUserSettingAction != null)
        {
            openUserSettingAction.performed += OnOpenUserSettingUI;
        }

    }
    void OnOpenUserSettingUI(InputAction.CallbackContext context)
    {
        
        transform.position = cam.transform.position + cam.transform.forward * distance - Vector3.up * verticalOffset;
        transform.LookAt(cam.transform.position, Vector3.up);
        
        bool shouldEnable = !UserSettingUIActive;
        foreach (Transform child in transform)
        {   
            child.localPosition = Vector3.zero;
            child.gameObject.SetActive(shouldEnable);
            
            foreach(Transform grandChild in child)
            {
                grandChild.gameObject.SetActive(shouldEnable);
            }
           
        }
        UserSettingUIActive = shouldEnable;
        
    }
    

}
