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
