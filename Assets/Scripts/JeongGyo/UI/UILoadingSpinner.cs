using UnityEngine;
using UnityEngine.UI;

public class UILoadingSpinner : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private RectTransform _TargetRectTransform;
    [SerializeField] private Image _TargetImage;

    [Header("Settings")]
    [Tooltip("Rotation per sec(clockwise is negative)")]
    [SerializeField] private float _RotationSpeed = -240f;

    [Header("Stretch Settings")]
    private float _CycleDuration = 1.5f; // configured automatically by RotationSpeed
    [Range(0f, 1f)]
    [SerializeField] private float _MinFillAmount = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float _MaxStretchAmount = 0.2f;    // how much to add to MinFillAmount(multiplied by curve value)

    [Tooltip("시간에 따른 길이 변화 그래프 (X축: 0~1 시간, Y축: 0~1 적용 비율)")]
    [SerializeField] private AnimationCurve _StretchCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));

    [SerializeField] private bool _IsQueueing = false;

    private float timer;
    
    private void Start()
    {
        if (_TargetRectTransform == null) _TargetRectTransform = GetComponent<RectTransform>();
        if (_TargetImage == null) _TargetImage = GetComponent<Image>();

        // curve loop setting
        _StretchCurve.preWrapMode = WrapMode.Loop;
        _StretchCurve.postWrapMode = WrapMode.Loop;

        // auto-configure cycle duration based on rotation speed
        if (Mathf.Approximately(_RotationSpeed, 0f))
        {
            _CycleDuration = 1.5f; // default
        }
        else
        {
            float fullRotationTime = 360f / Mathf.Abs(_RotationSpeed); // time for a full rotation
            _CycleDuration = fullRotationTime;
        }
    }
    
    private void OnEnable()
    {
        // Reset state when panel is enabled
        timer = 0f;
        if (_TargetRectTransform != null)
        {
            _TargetRectTransform.localRotation = Quaternion.identity;
        }
        if (_TargetImage != null)
        {
            _TargetImage.fillAmount = _MinFillAmount;
        }
    }


    // API to start/stop the spinner
    public void MatchingStarted()
    {
        _IsQueueing = true;
    }

    public void MatchingStopped()
    {
        _IsQueueing = false;
    }

    void Update()
    {   
        // rotate only when queueing
        if(!_IsQueueing) return;

        // Z-axis rotation every second
        if (_TargetRectTransform != null)
        {
            _TargetRectTransform.Rotate(0f, 0f, _RotationSpeed * Time.deltaTime);
        }

        if (_TargetImage != null)
        {
            timer += Time.deltaTime;
            float cycleProgress = Mathf.Repeat(timer / _CycleDuration, 1f);
            float curveValue = _StretchCurve.Evaluate(cycleProgress);
            _TargetImage.fillAmount = _MinFillAmount + (curveValue * _MaxStretchAmount);
        }
    }
}