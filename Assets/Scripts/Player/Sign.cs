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
        EnsurePlayerInput();
    }

    private void OnEnable()
    {
        EnsurePlayerInput();
        playerInput.Player.Confirm.started -= OnConfirm;
        playerInput.Player.Confirm.started += OnConfirm;
        playerInput.Enable();
    }

    private void Update()
    {
        signSprite.SetActive(canPress);
        signSprite.transform.localScale = playerTrans.localScale;
        
    }

    private void OnDisable()
    {
        if (playerInput != null)
        {
            playerInput.Player.Confirm.started -= OnConfirm;
            playerInput.Disable();
        }

        canPress = false;
        targetItem = null;
    }

    private void EnsurePlayerInput()
    {
        if (playerInput == null)
        {
            playerInput = new @_2Dgame();
        }
    }

    public void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Interactable"))
        {
            targetItem = other.GetComponent<IInteractable>();
            canPress = targetItem != null;

            if (canPress)
            {
                anim.Play("Sign_E");
            }
        }
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<IInteractable>() == targetItem)
        {
            canPress = false;
            targetItem = null;
        }
    }
    
    private void OnConfirm(InputAction.CallbackContext obj)
    {
        if (canPress && targetItem != null)
        {
            targetItem.TriggerAction();
            GetComponent<AudioDefination>()?.PlayAudioClip();
        }
    }
}
