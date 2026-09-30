using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [Header("事件监听")] public PlayAudioEventSO FXEvent;
    public PlayAudioEventSO BGMEvent;
    public FloatEventSO volumeEvent;
    public VoidEventSO pauseEvent;

    [Header("事件广播")]
    public FloatEventSO syncVolumeEvent;
    
    public AudioSource BGMSource;
    public AudioSource FXSource;
    public AudioMixer mixer;

    private void OnEnable()
    {
        FXEvent.OnEventRaised += OnFXEvent;
        BGMEvent.OnEventRaised += OnBGMEvent;
        volumeEvent.OnEventRaised += OnVolumeEvent;
        pauseEvent.OnEventRaised += OnPauseEvent;
    }

    private void OnDisable()
    {
        FXEvent.OnEventRaised -= OnFXEvent;
        BGMEvent.OnEventRaised -= OnBGMEvent;
        volumeEvent.OnEventRaised -= OnVolumeEvent;
        pauseEvent.OnEventRaised -= OnPauseEvent;
    }

    private void OnFXEvent(AudioClip audioClip)
    {
        FXSource.clip = audioClip;
        FXSource.Play();
    }

    private void OnBGMEvent(AudioClip audioClip)
    {
        BGMSource.clip = audioClip;
        BGMSource.Play();
    }

    private void OnVolumeEvent(float amount)
    {
        mixer.SetFloat("MasterVolume", amount * 100 - 80);
    }

    private void OnPauseEvent()
    {
        float amount;
        mixer.GetFloat("MasterVolume", out amount);
        syncVolumeEvent.RaiseEvent(amount);
    }
}