using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public SceneLoadEventSO sceneLoadEvent;
    public VoidEventSO afterSceneLoadEvent;
    
    public @_2Dgame inputController;
    public Vector2 inputDirection;
    public float Speed;
    public float JumpForce;
    public Rigidbody2D rb;
    public SpriteRenderer sr;
    public GroundCheck groundCheck;
    public bool isHurt;
    public float hurtForce;
    public bool isDead;
    public PlayerAnimation playerAnimation;
    public bool isAttack;
    public PhysicsMaterial2D normalMaterial;
    public PhysicsMaterial2D wallMaterial;
    public Transform attackRoot;

    private void Awake()
    {
        EnsureInputController();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        playerAnimation = GetComponent<PlayerAnimation>();
        if (groundCheck == null)
        {
            groundCheck = GetComponentInChildren<GroundCheck>();
        }
        attackRoot = transform.Find("Attack");
        inputController.Enable();
    }
    private void OnEnable()
    {
        EnsureInputController();
        sceneLoadEvent.LoadRequestEvent -= OnLoadEvent;
        sceneLoadEvent.LoadRequestEvent += OnLoadEvent;
        inputController.Player.Attack.started -= PlayerAttack;
        inputController.Player.Attack.started += PlayerAttack;
        afterSceneLoadEvent.OnEventRaised -= OnAfterSceneLoadedEvent;
        afterSceneLoadEvent.OnEventRaised += OnAfterSceneLoadedEvent;
    }
    private void OnDisable()
    {
        sceneLoadEvent.LoadRequestEvent -= OnLoadEvent;
        afterSceneLoadEvent.OnEventRaised -= OnAfterSceneLoadedEvent;

        if (inputController != null)
        {
            inputController.Player.Attack.started -= PlayerAttack;
            inputController.Disable();
        }
    }

    private void Update()
    {
        inputDirection = inputController.Player.Move.ReadValue<Vector2>();
        Filp();

        if (inputController.Player.Jump.WasPressedThisFrame() && groundCheck.isGrounded)
        {
            Jump();
        }
        CheckState();
    }
    private void FixedUpdate()
    {
        if (!isHurt && !isAttack)
        {
            Move();
        }
    }

    private void OnLoadEvent(GameSceneSO arg0, Vector3 arg1, bool arg2)
    {
        EnsureInputController();
        inputController.Player.Disable();
    }

    private void OnAfterSceneLoadedEvent()
    {
        EnsureInputController();
        isDead = false;
        isHurt = false;
        isAttack = false;
        rb.velocity = Vector2.zero;
        inputController.Player.Enable();
    }

    private void EnsureInputController()
    {
        if (inputController == null)
        {
            inputController = new @_2Dgame();
        }
    }

    private void Move()
    {
        rb.velocity = new Vector2(inputDirection.x * Speed, rb.velocity.y);
    }

    private void Filp()
    {
        if (inputDirection.x > 0)
        {
            sr.flipX = false;
            attackRoot.localScale = new Vector3(1, 1, 1);
        }

        else if (inputDirection.x < 0)
        {
            sr.flipX = true;
            attackRoot.localScale = new Vector3(-1, 1, 1);
        }
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, JumpForce);
    }

    public void GetHurt(Transform attacker)
    {
        isHurt = true;
        rb.velocity = Vector2.zero;
        Vector2 dir = new Vector2(transform.position.x - attacker.position.x, 0).normalized;
        rb.AddForce(dir * hurtForce, ForceMode2D.Impulse);
    }

    public void Death()
    {
        isDead = true;
        inputController.Player.Disable();
    }
    public void PlayerAttack(InputAction.CallbackContext context)
    {
        if(groundCheck.isGrounded)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
        playerAnimation.PlayAttack();
        isAttack = true;
    }

    private void CheckState()
    {
        rb.sharedMaterial = groundCheck.isGrounded ? normalMaterial : wallMaterial;
    }
}
