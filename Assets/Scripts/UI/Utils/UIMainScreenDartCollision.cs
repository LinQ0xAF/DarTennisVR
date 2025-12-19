using UnityEngine;

public class UIMainScreenDartCollision : MonoBehaviour
{
    [SerializeField]
    private GameObject SingleUIPanelObj;

    [SerializeField]
    private GameObject MultiUIPanelObj;
    
    [SerializeField]
    private GameObject _MainUIPanelObj;

    [SerializeField]
    private bool isSingleUI = true;

    private int _DartsLayerIndex;

    void Awake()
    {
        _DartsLayerIndex = LayerMask.NameToLayer("Darts");
    }


    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == _DartsLayerIndex)
        {
            Debug.Log("Dart hit UI Control Panel");

            if (isSingleUI)
            {
                SingleUIPanelObj.SetActive(true);
                MultiUIPanelObj.SetActive(false);
                _MainUIPanelObj.SetActive(false);
            }
            else
            {
                SingleUIPanelObj.SetActive(false);
                MultiUIPanelObj.SetActive(true);
                _MainUIPanelObj.SetActive(false);
            }
            
        }
    }


}
