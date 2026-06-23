using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    Animator animator;
    Rigidbody2D rb;
    GroundCheck groundCheck;

    static readonly int VelocityX = Animator.StringToHash("velocityX");
    static readonly int VelocityY = Animator.StringToHash("velocityY");
    static readonly int IsGrounded = Animator.StringToHash("isGround");

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        groundCheck = GetComponentInChildren<GroundCheck>();
    }

    void Update()
    {
        animator.SetFloat(VelocityX, Mathf.Abs(rb.velocity.x));
        animator.SetFloat(VelocityY, rb.velocity.y);
        animator.SetBool(IsGrounded, groundCheck.isGrounded);
    }
}
