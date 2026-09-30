using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Operations;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/FloatEventSO")]
public class FloatEventSO : ScriptableObject
{
    public UnityAction<float> OnEventRaised;

    public void RaiseEvent(float amount)
    {
        OnEventRaised?.Invoke(amount);
    }
    
}
