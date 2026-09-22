using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/SceneLoadEventSO")]
public class SceneLoadEventSO : ScriptableObject
{
    public UnityAction<GameSceneSO, Vector3, bool> LoadRequestEvent;

    public void RaiseLoadRequestEvent(GameSceneSO sceneToGo, Vector3 posToGO, bool fadeSceen)
    {
        LoadRequestEvent?.Invoke(sceneToGo, posToGO, fadeSceen);
    }
}