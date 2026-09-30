using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/FadeEventSO")]
public class FadeEventSO : ScriptableObject
{
    public UnityAction<Color, float, bool> OnEventRaised;
    
    public void FadeIn(float duration)
    {
        RaiseEventRaised(Color.black, duration, true);   
    }

    public void FadeOut(float duration)
    {
        RaiseEventRaised(Color.clear, duration, false);
    }

    public void RaiseEventRaised(Color targetColor, float duration, bool fadeIn)
    {
        OnEventRaised?.Invoke(targetColor, duration, true);
    }
}
