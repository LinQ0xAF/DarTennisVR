using UnityEngine;
using System.Collections;

public class AutoDeactivate : MonoBehaviour
{
    [SerializeField] private float lifetime = 2.0f;
    private WaitForSeconds _wait;

    private void Awake()
    {
        // 최적화를 위해 WaitForSeconds를 캐싱합니다.
        _wait = new WaitForSeconds(lifetime);
    }

    private void OnEnable()
    {
        StartCoroutine(DeactivateRoutine());
    }

    private IEnumerator DeactivateRoutine()
    {
        yield return _wait;
        gameObject.SetActive(false);
    }
}
