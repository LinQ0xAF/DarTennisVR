using UnityEngine;
using System.Collections.Generic;

public class RangeTargetRespawn : MonoBehaviour
{
    [SerializeField]
    private GameObject _TargetDummyPrefab;
    [SerializeField]
    private GameObject _FirstTargetDummy;

    [SerializeField]
    private Transform _RangeTargetSpawnPoint;

    private List<GameObject> _ActiveTargetDummies = new List<GameObject>();

    void Start()
    {
        _ActiveTargetDummies.Add(_FirstTargetDummy);
    }

    public void ResetTargets()
    {
        foreach (var targetDummy in _ActiveTargetDummies)
        {
            Destroy(targetDummy);
        }
        _ActiveTargetDummies.Clear();

        GameObject newTargetDummy = Instantiate(_TargetDummyPrefab, _RangeTargetSpawnPoint.position, _RangeTargetSpawnPoint.rotation, _RangeTargetSpawnPoint);
        _ActiveTargetDummies.Add(newTargetDummy);
    }
}
