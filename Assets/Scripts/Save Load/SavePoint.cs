using UnityEngine;

public class SavePoint : MonoBehaviour, IInteractable
{
    public VoidEventSO saveGameDataEvent;
    public SpriteRenderer spriteRenderer;
    public Sprite darkSprite;
    public Sprite lightSprite;
    public bool isDone;

    private void Awake()
    {
        // The assigned renderer belongs to the M child. Do not replace the rock's sprite.
        if (spriteRenderer == null)
        {
            spriteRenderer = transform.Find("M")?.GetComponent<SpriteRenderer>();
        }
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
