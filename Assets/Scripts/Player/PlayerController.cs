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

    private void Awake()
    {
        inputController = new @_2Dgame();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if(groundCheck == null){
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
    }
    private void FixedUpdate()
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

}
