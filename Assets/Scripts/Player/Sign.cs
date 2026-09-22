using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Sign : MonoBehaviour
{
    private Animator anim;
    public Transform playerTrans;
    public GameObject signSprite;
    private bool canPress;
    public @_2Dgame playerInput;
    private IInteractable targetItem;

    private void Awake()
    { 
        anim =  signSprite.GetComponent<Animator>();
        playerInput = new @_2Dgame();
        playerInput.Enable();
    }

    private void OnEnable()
    {
        playerInput.Player.Confirm.started += OnConfirm;
    }

    private void Update()
    {
        signSprite.SetActive(canPress);
        signSprite.transform.localScale = playerTrans.localScale;
        
    }

    private void OnDisable()
    {
        canPress = false;
    }

    public void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Interactable"))
        {
            canPress = true;
            anim.Play("Sign_E");
            targetItem = other.GetComponent<IInteractable>();
        }
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        canPress = false;
    }
    
    private void OnConfirm(InputAction.CallbackContext obj)
    {
        if (canPress)
        {
            targetItem.TriggerAction();
            GetComponent<AudioDefination>()?.PlayAudioClip();
        }
    }
}
