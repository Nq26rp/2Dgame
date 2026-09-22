using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public Transform playerTrans;
    public Vector3 firstPos;
    
    public SceneLoadEventSO loadEventSO;

    public GameSceneSO firstLoadScene;
    public GameSceneSO currentLoadedScene;

    public VoidEventSO afterSceneLoadedEvent;
    
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
        NewGame();
    }

    private void OnEnable()
    {
        loadEventSO.LoadRequestEvent += OnLoadRequestEvent;
    }

    private void OnDisable()
    {
        loadEventSO.LoadRequestEvent -= OnLoadRequestEvent;
    }


    private void NewGame()
    {
        sceneToLoad = firstLoadScene;
        OnLoadRequestEvent(firstLoadScene, firstPos, true);
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
        }

        yield return new WaitForSeconds(fadeDuration);
        yield return currentLoadedScene.sceneReference.UnLoadScene();
        playerTrans.gameObject.SetActive(false);
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
        }
        isLoading = false;
        afterSceneLoadedEvent.RaiseEvent();
    }
}