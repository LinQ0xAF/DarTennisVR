using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.UIElements;

public class BalloonManager : MonoBehaviour
{

    [Range(1, 5)]
    [SerializeField]
    private int balloonNumber = 1;
    [SerializeField]
    private int balloonCurrentNumber = 1;
    [SerializeField]
    public List<Balloons> BalloonList = new List<Balloons>();



    public int BalloonNumber
    {
        get => balloonNumber;
        set => balloonNumber = Mathf.Clamp(value, 1, 5);
    }
    public int BalloonCurrentNumber
    {
        get => balloonCurrentNumber;
        set => balloonCurrentNumber = Mathf.Clamp(value, 1, 5);
    }
    void Start()
    {       
        BalloonCurrentNumber = BalloonNumber;

        for (int i = 0; i < BalloonCurrentNumber; i++)
        {
            BalloonList[i].gameObject.SetActive(true);
            BalloonList[i].OnHit += HandleBalloonHit;
        }

        for (int i = BalloonList.Count-1; i >= BalloonCurrentNumber; i--)
        {
            BalloonList[i].gameObject.SetActive(false);
            BalloonList.Remove(BalloonList[BalloonCurrentNumber]);
        }

    }
    private void HandleBalloonHit(Balloons b)
    {
        b.OnHit -= HandleBalloonHit;
        b.gameObject.SetActive(false);
        BalloonList.Remove(b);
        Debug.Log($"[BalloonManager] Remaining:{BalloonList.Count}");
    }

}



// using System.Collections.Generic;
// using System.Runtime.ExceptionServices;
// using UnityEngine;
// using UnityEngine.UIElements;

// public class BalloonManager : MonoBehaviour
// {

//     [Range(1, 5)]
//     [SerializeField]
//     private int balloonNumber = 1;
//     [SerializeField]
//     private int balloonCurrentNumber = 1;
//     [SerializeField]
//     public List<Balloons> BalloonList = new List<Balloons>();

//     /// <summary>
//     /// 풍선이 맞았다는 신호를 서버/게임 매니저에 전달하기 위한 이벤트(네트워크 전송은 별도 스크립트가 구독).
//     /// 인자: 풍선 인덱스(초기 리스트 기준)
//     /// </summary>
//     public event System.Action<int> OnBalloonPopRequest;

//     /// <summary>
//     /// 모든 풍선이 제거됐을 때 알림. 세트 종료 판정 등에 사용.
//     /// </summary>
//     public event System.Action OnAllBalloonsCleared;


//     public int BalloonNumber
//     {
//         get => balloonNumber;
//         set => balloonNumber = Mathf.Clamp(value, 1, 5);
//     }
//     public int BalloonCurrentNumber
//     {
//         get => balloonCurrentNumber;
//         set => balloonCurrentNumber = Mathf.Clamp(value, 1, 5);
//     }
//     void Start()
//     {
//         BalloonCurrentNumber = BalloonNumber;

//         for (int i = 0; i < BalloonCurrentNumber; i++)
//         {
//             BalloonList[i].gameObject.SetActive(true);
//             BalloonList[i].OnHit += HandleBalloonHit;
//         }

//         for (int i = BalloonList.Count-1; i >= BalloonCurrentNumber; i--)
//         {
//             BalloonList[i].gameObject.SetActive(false);
//             BalloonList.Remove(BalloonList[BalloonCurrentNumber]);
//         }

//     }
//     private void HandleBalloonHit(Balloons b)
//     {
//         int balloonIndex = BalloonList.IndexOf(b); // 서버 보고용 인덱스 확보

//         b.OnHit -= HandleBalloonHit;
//         b.gameObject.SetActive(false);
//         BalloonList.Remove(b);
//         Debug.Log($"[BalloonManager] Remaining:{BalloonList.Count}");

//         // 네트워크/게임 매니저가 구독해 서버 보고/점수 처리
//         if (balloonIndex >= 0)
//             OnBalloonPopRequest?.Invoke(balloonIndex);

//         // 모든 풍선이 제거되었으면 세트/게임 종료 알림
//         if (BalloonList.Count == 0)
//             OnAllBalloonsCleared?.Invoke();
//     }

// }
