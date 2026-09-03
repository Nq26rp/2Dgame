using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
public class CameraController : MonoBehaviour
{
    private CinemachineConfiner2D confiner2D;
    public CinemachineImpulseSource impulseSource;
    public VoidEventSO cameraShakeEvent;

    private void Awake()
    {
        confiner2D = GetComponent<CinemachineConfiner2D>();
    }

    private void OnEnable()
    {
        cameraShakeEvent.OnEventRaised += OnCameraShakeEvent;
    }

    private void Start()
    {
        GetNewCameraBounds();
    }

    private void OnDisable()
    {
        cameraShakeEvent.OnEventRaised -= OnCameraShakeEvent;
    }
    
    private void GetNewCameraBounds()
    {
        var obj = GameObject.FindWithTag("Bounds");
        if (obj != null)
            return;
        confiner2D.m_BoundingShape2D = obj.GetComponent<BoxCollider2D>();
        confiner2D.InvalidateCache();
    }

    private void OnCameraShakeEvent()
    {
        impulseSource.GenerateImpulse();
    }
}
