using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class ArrowTrap : NetworkBehaviour
{
    [SerializeField] private float _cooldown;
    [SerializeField] private float _arrowAmount;
    [SerializeField] private List<Transform> _refPoints;
    [SerializeField] private NetworkPrefabRef _arrowPrefab;

    private TickTimer cooldownTimer;

    public override void FixedUpdateNetwork()
    {

    }


    public void OnTriggerEnter2D(Collider2D collision)
    {
        if(CheckCD())
        {
            Activate();
        }
    }

    private bool CheckCD()
    {
        return true;
    }

    [ContextMenu("Test")]
    public void Activate()
    {
        for (int i = 0; i < _arrowAmount; i++)
        {
            Transform refPoint = SelectRandomRefPoint();
            if(!HasStateAuthority)
                return;
            Runner.Spawn(_arrowPrefab, refPoint.position, refPoint.rotation, Object.InputAuthority);
        }
    }

    private Transform SelectRandomRefPoint()
    {
        return _refPoints[Random.Range(0, _refPoints.Count)];
    }
}
