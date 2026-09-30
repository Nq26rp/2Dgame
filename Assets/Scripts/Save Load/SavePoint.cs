using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SqlServer.Server;
using UnityEngine;

public class SavePoint : MonoBehaviour
{
    public VoidEventSO saveGameDataEvent;
    public SpriteRenderer spriteRenderer;
    public Sprite darkSprite;
    public Sprite lightSprite;
    public bool isDone;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        spriteRenderer.sprite = isDone ? lightSprite : darkSprite;
    }

    public void TriggerAction()
    {
        if (!isDone)
        {
            isDone = true;
            spriteRenderer.sprite = lightSprite;
            
            saveGameDataEvent.RaiseEvent();
            
            this.gameObject.tag = "Untagged";
        }
    }
}
