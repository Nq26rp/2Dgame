using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "GameScene/SceneLoadEventSO")]
public class GameSceneSO : ScriptableObject
{
    public AssetReference sceneReference;
    public SceneType sceneType;
}