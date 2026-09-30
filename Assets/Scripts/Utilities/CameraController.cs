using UnityEngine;
using Cinemachine;
public class CameraController : MonoBehaviour
{
    public VoidEventSO afterSceneLoadedEvent;
    
    private CinemachineConfiner2D confiner2D;
    public CinemachineImpulseSource impulseSource;
    public VoidEventSO cameraShakeEvent;

    private void Awake()
    {
        confiner2D = GetComponent<CinemachineConfiner2D>();
    }

    private void OnEnable()
    {
        if (confiner2D == null)
        {
            confiner2D = GetComponent<CinemachineConfiner2D>();
        }

        cameraShakeEvent.OnEventRaised -= OnCameraShakeEvent;
        cameraShakeEvent.OnEventRaised += OnCameraShakeEvent;
        afterSceneLoadedEvent.OnEventRaised -= OnAfterSceneLoadedEvent;
        afterSceneLoadedEvent.OnEventRaised += OnAfterSceneLoadedEvent;
    }

    private void Start()
    {
        //GetNewCameraBounds();
    }

    private void OnDisable()
    {
        cameraShakeEvent.OnEventRaised -= OnCameraShakeEvent;
        afterSceneLoadedEvent.OnEventRaised -= OnAfterSceneLoadedEvent;
    }
    
    private void GetNewCameraBounds()
    {
        var obj = GameObject.FindWithTag("Bounds");
        if (obj == null)
        {
            Debug.LogWarning("当前场景中没有找到 Bounds 标签对象，相机边界未更新。");
            return;
        }

        var bounds = obj.GetComponent<Collider2D>();
        if (bounds == null)
        {
            Debug.LogWarning("Bounds 对象缺少 Collider2D，相机边界未更新。", obj);
            return;
        }

        confiner2D.m_BoundingShape2D = bounds;
        confiner2D.InvalidateCache();
    }

    private void OnCameraShakeEvent()
    {
        impulseSource.GenerateImpulse();
    }
    
    private void OnAfterSceneLoadedEvent()
    {
        GetNewCameraBounds();
    }
}
