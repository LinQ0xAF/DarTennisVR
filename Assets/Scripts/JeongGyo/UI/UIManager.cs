using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UIElements;


public class UIManager : MonoBehaviour
{

    [SerializeField]
    public List<UIPopUp> PopUpList = new List<UIPopUp>(); // 서로 세트가 되는 팝업UI들을 관리하는 리스트

    void Start()
    {
        // 리스트의 다른 ui를 비활성화 하는 이벤트 핸들러 등록, 리스트가 없다면 패스
        if (PopUpList.Count != 0 && PopUpList != null)
            for (int i = 0; i < PopUpList.Count; i++)
            {
                PopUpList[i].OnHit += HandleUIHit;
            }
        
       
    }
    private void HandleUIHit(UIPopUp hitUI)
    {
    
        SetChildrenActive(hitUI.transform, true); // 해당 UI의 자식들만 활성화

        for (int i = 0; i < PopUpList.Count; i++) // 나머지 UI들은 비활성화
        {
            if (PopUpList[i] == hitUI)
                continue;
                
            SetChildrenActive(PopUpList[i].transform, false);
        }

    }

    private static void SetChildrenActive(Transform parent, bool state)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            parent.GetChild(i).gameObject.SetActive(state);
        }
    }

}
