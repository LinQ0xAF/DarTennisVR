using UnityEngine;

/// <summary>
/// 플레이어 위치(1P/2P)에 따라 UI를 좌우로 뒤집어 준다.
/// SpawnManager에서 SetFlipped(true/false)로 호출.
/// </summary>
public class UIOrientationFlipper : MonoBehaviour
{
    [SerializeField] private RectTransform root; // 뒤집을 대상(비워두면 자기 자신)

    private void Awake()
    {
        if (root == null)
            root = transform as RectTransform;
    }

    /// <summary>
    /// flip=true면 X 스케일을 -1로 뒤집어서 2P에서도 정방향 UI를 보게 한다.
    /// </summary>
    public void SetFlipped(bool flip)
    {
        if (root == null)
            return;

        var scale = root.localScale;
        scale.x = Mathf.Abs(scale.x) * (flip ? -1f : 1f);
        root.localScale = scale;
    }
}
