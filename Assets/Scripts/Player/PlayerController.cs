using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
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

    private void Awake()
    {
        inputController = new @_2Dgame();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (groundCheck == null)
        {
            groundCheck = GetComponentInChildren<GroundCheck>();
        }


    }
    private void OnEnable()
    {
        inputController.Enable();
    }
    private void OnDisable()
    {
        inputController.Disable();
    }

    private void Update()
    {
        inputDirection = inputController.Player.Move.ReadValue<Vector2>();
        Filp();

        if (inputController.Player.Jump.WasPressedThisFrame() && groundCheck.isGrounded)
        {
            Jump();
        }

        if (inputController.Player.Attack.WasPressedThisFrame())
        {
            Debug.Log("Attack");
        }
    }
    private void FixedUpdate()
    {
        if (!isHurt)
        {
            Move();
        }
    }

    private void Move()
    {
        rb.velocity = new Vector2(inputDirection.x * Speed, rb.velocity.y);
    }

    private void Filp()
    {
        if (inputDirection.x > 0)
            sr.flipX = false;
        else if (inputDirection.x < 0)
            sr.flipX = true;
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

}
