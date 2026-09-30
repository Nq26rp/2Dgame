using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;

    public VoidEventSO saveDataEvent;
    public VoidEventSO loadDataEvent;
    public VoidEventSO newGameEvent;
    
    private List<ISaveable> saveableList = new List<ISaveable>();
    private Data saveData;

    public bool HasSaveData =>
        saveData != null &&
        !string.IsNullOrEmpty(saveData.sceneToSave) &&
        saveData.characterPosDict.Count > 0;
    
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(this.gameObject);
        }
        
        saveData = new Data();
    }

    private void OnEnable()
    {
        saveDataEvent.OnEventRaised += Save;
        loadDataEvent.OnEventRaised += Load;
    }

    private void OnDisable()
    {
        saveDataEvent.OnEventRaised -= Save;
        loadDataEvent.OnEventRaised -= Load;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            Load();
        }
    }

    public void RegisterSaveData(ISaveable saveable)
    {
        if (!saveableList.Contains(saveable))
        {
            saveableList.Add(saveable);
        }
    }

    public void UnRegisterSaveData(ISaveable saveable)
    {
        saveableList.Remove(saveable);
    }

    public void Save()
    {
        foreach (ISaveable saveable in saveableList)
        {
            saveable.GetSaveData(saveData);
        }

        foreach (var item in saveData.characterPosDict)
        {
            Debug.Log(item.Key + ":" + item.Value);
        }
    }

    public void Load()
    {
        if (!HasSaveData)
        {
            Debug.LogWarning("没有可读取的存档，将从新游戏开始。");
            newGameEvent.RaiseEvent();
            return;
        }

        foreach (ISaveable saveable in saveableList.ToArray())
        {
            saveable.LoadData(saveData);
        }
    }
}
