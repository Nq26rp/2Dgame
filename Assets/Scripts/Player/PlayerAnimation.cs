using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] GroundCheck groundCheck;
    [SerializeField] PlayerController playerController;
    static readonly int VelocityX = Animator.StringToHash("velocityX");
    static readonly int VelocityY = Animator.StringToHash("velocityY");
    static readonly int IsGrounded = Animator.StringToHash("isGround");

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        groundCheck = GetComponentInChildren<GroundCheck>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        animator.SetFloat(VelocityX, Mathf.Abs(rb.velocity.x));
        animator.SetFloat(VelocityY, rb.velocity.y);
        animator.SetBool(IsGrounded, groundCheck.isGrounded);
        animator.SetBool("isDead", playerController.isDead);
        animator.SetBool("isAttack", playerController.isAttack);
    }

    public void PlayHurt()
    {
        animator.SetTrigger("Hurt");
    }

    public void PlayAttack()
    {
        animator.SetTrigger("attack");
    }

}
