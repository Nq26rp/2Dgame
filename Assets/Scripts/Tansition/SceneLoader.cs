using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour, ISaveable
{
    public Transform playerTrans;
    public Vector3 firstPos;
    public Vector3 menuPos;

    public SceneLoadEventSO loadEventSO;

    public GameSceneSO firstLoadScene;
    public GameSceneSO currentLoadedScene;
    public GameSceneSO menuScene;

    public VoidEventSO afterSceneLoadedEvent;
    public FadeEventSO fadeEvent;
    public VoidEventSO newGameEvent;
    public SceneLoadEventSO unloadSceneEvent;
    public VoidEventSO backToMenuEvent;

    private GameSceneSO sceneToLoad;
    private Vector3 posToGo;
    private bool fadeSceen;
    private bool isLoading;

    public float fadeDuration;

    private void Awake()
    {
        //Addressables.LoadSceneAsync(firstLoadScene.sceneReference, LoadSceneMode.Additive);
        //currentLoadedScene = firstLoadScene;
        //currentLoadedScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive);
    }

    private void Start()
    {
        loadEventSO.RaiseLoadRequestEvent(menuScene, menuPos, true);
        //NewGame();
    }

    private void OnEnable()
    {
        loadEventSO.LoadRequestEvent += OnLoadRequestEvent;
        newGameEvent.OnEventRaised += NewGame;
        backToMenuEvent.OnEventRaised += OnBackToMenu;
        ISaveable saveable = this;
        saveable.RegisterSaveData();
    }

    private void OnDisable()
    {
        loadEventSO.LoadRequestEvent -= OnLoadRequestEvent;
        newGameEvent.OnEventRaised -= NewGame;
        backToMenuEvent.OnEventRaised -= OnBackToMenu;
        ISaveable saveable = this;
        saveable.UnregisterSaveData();
    }


    private void NewGame()
    {
        sceneToLoad = firstLoadScene;
        loadEventSO.RaiseLoadRequestEvent(sceneToLoad, firstPos, true);
    }

    private void OnLoadRequestEvent(GameSceneSO sceneToGo, Vector3 posToGo, bool fadeSceen)
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;
        sceneToLoad = sceneToGo;
        this.posToGo = posToGo;
        this.fadeSceen = fadeSceen;

        // 场景切换期间停止角色与旧场景的碰撞，避免刚恢复的生命值再次被扣除。
        playerTrans.gameObject.SetActive(false);

        if (currentLoadedScene != null)
        {
            StartCoroutine(UnLoadPreviousScene());
        }
        else
        {
            LoadNewScene();
        }
    }

    private IEnumerator UnLoadPreviousScene()
    {
        if (fadeSceen)
        {
            fadeEvent.FadeIn(fadeDuration);
        }

        yield return new WaitForSeconds(fadeDuration);
        unloadSceneEvent.LoadRequestEvent(sceneToLoad, posToGo, true);
        yield return currentLoadedScene.sceneReference.UnLoadScene();
        LoadNewScene();
    }

    private void LoadNewScene()
    {
        var loadingOption = sceneToLoad.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);
        loadingOption.Completed += OnLoadCompleted;
    }

    private void OnLoadCompleted(AsyncOperationHandle<SceneInstance> obj)
    {
        currentLoadedScene = sceneToLoad;
        playerTrans.position = posToGo;
        playerTrans.gameObject.SetActive(true);
        if (fadeSceen)
        {
            fadeEvent.FadeOut(fadeDuration);
        }

        isLoading = false;

        if (currentLoadedScene.sceneType == SceneType.Location)
        {
            afterSceneLoadedEvent.RaiseEvent();
        }
    }

    public DataDefinition GetDataID()
    {
        return GetComponent<DataDefinition>();
    }

    public void GetSaveData(Data data)
    {
        data.SaveGameScene(currentLoadedScene);
    }

    public void LoadData(Data data)
    {
        var playerID = playerTrans.GetComponent<DataDefinition>().ID;
        if (data.characterPosDict.ContainsKey(playerID))
        {
            posToGo = data.characterPosDict[playerID];
            sceneToLoad = data.GetSavedScene();
            OnLoadRequestEvent(sceneToLoad, posToGo, true);
        }
    }

    private void OnBackToMenu()
    {
        sceneToLoad = menuScene;
        loadEventSO.RaiseLoadRequestEvent(sceneToLoad, menuPos, true);
    }
}
