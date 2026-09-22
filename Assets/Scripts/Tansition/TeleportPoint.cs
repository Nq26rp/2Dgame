using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportPoint : MonoBehaviour,IInteractable
{
    public SceneLoadEventSO loadEventSO;
    public Vector3 posToGo;
    public GameSceneSO sceneToGo;
    public void TriggerAction()
    {
        loadEventSO.RaiseLoadRequestEvent(sceneToGo, posToGo,true);
    }
}
