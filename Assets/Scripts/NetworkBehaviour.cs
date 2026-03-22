using System;
using PurrNet;
using UnityEngine;

public class TestingNetwork : NetworkBehaviour
{
    [SerializeField] private NetworkIdentity _networkIdentity;

    private void Awake()
    {
        
    }

    private void Start()
    {
        
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        
    }
}
