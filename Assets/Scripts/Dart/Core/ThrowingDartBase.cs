using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// 잡고 던질 수 있는 다트의 공통 기능을 제공하는 추상 클래스
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public abstract class ThrowingDartBase : MonoBehaviour
{
    [Header("Base Settings")]
    [Tooltip("다트가 던져진 후 풀에 자동으로 반환될 때까지의 최대 시간 (초)")]
    [SerializeField] protected float maxLifetime = 10.0f;

    protected Rigidbody rb;
    protected XRGrabInteractable grabInteractable;
    protected bool hasCollided = false;
    protected Coroutine returnCoroutine;
    protected Coroutine maxLifetimeCoroutine;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    protected virtual void OnEnable()
    {
        // 재사용 시 상태 초기화
        hasCollided = false;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        grabInteractable.throwOnDetach = true;

        // 이벤트 등록
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    protected virtual void OnDisable()
    {
        // 코루틴 정리
        StopAllReturnCoroutines();

        // 이벤트 해제
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    protected virtual void OnGrabbed(SelectEnterEventArgs args)
    {
        // 잡으면 모든 반환 타이머 취소
        StopAllReturnCoroutines();
    }

    protected virtual void OnReleased(SelectExitEventArgs args)
    {
        // 놓으면 최대 수명 타이머 시작
        StartMaxLifetimeCoroutine();
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (hasCollided) return;
        if (grabInteractable.isSelected) return;

        if (collision.gameObject.CompareTag("Environment"))
        {
            hasCollided = true;
            OnHitEnvironment(collision);
        }
    }

    /// <summary>
    /// 환경(벽/바닥)에 닿았을 때의 행동 (자식에서 구현)
    /// </summary>
    protected abstract void OnHitEnvironment(Collision collision);

    /// <summary>
    /// 오브젝트 풀로 반환하는 로직 (자식에서 구현)
    /// </summary>
    public abstract void ReturnToPool();

    #region Coroutine Utilities
    protected void StartReturnCoroutine(float delay)
    {
        StopReturnCoroutine();
        returnCoroutine = StartCoroutine(ReturnAfterDelay(delay));
    }

    protected void StartMaxLifetimeCoroutine()
    {
        StopMaxLifetimeCoroutine();
        maxLifetimeCoroutine = StartCoroutine(ReturnAfterMaxLifetime());
    }

    protected void StopReturnCoroutine()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }

    protected void StopMaxLifetimeCoroutine()
    {
        if (maxLifetimeCoroutine != null)
        {
            StopCoroutine(maxLifetimeCoroutine);
            maxLifetimeCoroutine = null;
        }
    }

    protected void StopAllReturnCoroutines()
    {
        StopReturnCoroutine();
        StopMaxLifetimeCoroutine();
    }

    private IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool();
    }

    private IEnumerator ReturnAfterMaxLifetime()
    {
        yield return new WaitForSeconds(maxLifetime);
        ReturnToPool();
    }
    #endregion
}
