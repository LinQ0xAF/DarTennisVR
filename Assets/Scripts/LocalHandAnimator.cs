using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;

public class LocalHandAnimator : MonoBehaviour
{
    [Header("Hand Role Setting")]
    public bool IsRightHandComponent;

    [Header("References")]
    [SerializeField] private Animator _AvatarAnimator;
    [SerializeField] private InputActionReference _GripAction;
    [SerializeField] private NearFarInteractor _HandInteractor; // to check if holding dart
    
    [Header("Data Source")]
    [SerializeField] private GamePersonalDataManager _GameSettings;

    [Header("Hand IK Target Reference")]
    [SerializeField] private Transform LocalHandIKTarget;

    [Header("Dart Held Comfort Settings")]
    [SerializeField] private Transform HeldDartRotationReference;
    [Tooltip("Delay before resetting hand IK target rotation on dart release (seconds)")]
    [SerializeField] private float ReleaseRotationDelay = 0.1f;

    // Parameter hashes (performance optimization)
    private int _GripHash;
    private int _IsMainHash;
    private int _IsHoldingHash;

    // internal state
    private float _CurrentGripValue = 0f;
    private bool _IsMainHand = false;
    private bool _IsHoldingDart = false;

    // inital Local Rotation cache
    private Quaternion _InitialLocalRotation;

    private void Awake()
    {
        // cache parameter hashes for performance
        string side = IsRightHandComponent ? "_R" : "_L";
        _GripHash = Animator.StringToHash("Grip" + side);
        _IsMainHash = Animator.StringToHash("IsMain" + side);
        _IsHoldingHash = Animator.StringToHash("IsHolding" + side);

    }

    void Start()
    {
        if (_GameSettings != null)
        {
            UpdateHandRole(_GameSettings.mainHand);
            _GameSettings.OnMainHandChanged += UpdateHandRole;
        }

        if( LocalHandIKTarget != null)
        {
            _InitialLocalRotation = LocalHandIKTarget.localRotation;
        }
    }

private void OnDestroy()
    {
        if (_GameSettings != null)
        {
            _GameSettings.OnMainHandChanged -= UpdateHandRole;
        }
    }

    private void OnEnable()
    {
        // subscribe to input and interaction events
        if (_GripAction != null)
        {
            _GripAction.action.performed += OnGripInput;
            _GripAction.action.canceled += OnGripInput;
        }
        if (_HandInteractor != null)
        {
            _HandInteractor.selectEntered.AddListener(OnGrab);
            _HandInteractor.selectExited.AddListener(OnRelease);
        }
    }

    private void OnDisable()
    {
        if (_GripAction != null)
        {
            _GripAction.action.performed -= OnGripInput;
            _GripAction.action.canceled -= OnGripInput;
        }
        if (_HandInteractor != null)
        {
            _HandInteractor.selectEntered.RemoveListener(OnGrab);
            _HandInteractor.selectExited.RemoveListener(OnRelease);
        }
    }

    private void UpdateHandRole(Hand mainHand)
    {
        _IsMainHand =  mainHand == (IsRightHandComponent ? Hand.Right : Hand.Left) ;
        RefreshAnimatorState();
    }

    private void OnGripInput(InputAction.CallbackContext context)
    {
        _CurrentGripValue = context.ReadValue<float>();
        RefreshAnimatorState();
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (args.interactableObject.transform.GetComponent<ThrowingDartBase>() != null)
        {
            _IsHoldingDart = true;
            RefreshAnimatorState();
            
            if (LocalHandIKTarget != null && HeldDartRotationReference != null)
            {
                LocalHandIKTarget.localRotation = HeldDartRotationReference.localRotation;
            }
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _IsHoldingDart = false;
        RefreshAnimatorState();
        
        if (LocalHandIKTarget != null)
        {
            StartCoroutine(DelayedRotationReset());
        }
    }

    private IEnumerator DelayedRotationReset()
    {
        yield return new WaitForSeconds(ReleaseRotationDelay);
        LocalHandIKTarget.localRotation = _InitialLocalRotation;
    }

    // reflect in Animator
    private void RefreshAnimatorState()
    {
        if (_AvatarAnimator == null) return;

        _AvatarAnimator.SetBool(_IsMainHash, _IsMainHand);

        if (_IsMainHand)
        {   // check holding state on main hand, ignore grip value
            _AvatarAnimator.SetFloat(_GripHash, 0f);
            _AvatarAnimator.SetBool(_IsHoldingHash, _IsHoldingDart);
        }
        else
        {   // non-main hand reflects grip value directly, ignore holding state
            _AvatarAnimator.SetFloat(_GripHash, _CurrentGripValue);
            _AvatarAnimator.SetBool(_IsHoldingHash, false);
        }
    }
}