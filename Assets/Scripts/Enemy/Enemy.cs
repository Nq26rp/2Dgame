using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    Rigidbody2D rb;
    protected Animator animator;
    GroundCheck groundCheck;
    public Transform attacker;

    [Header("基本参数")]
    public float normalSpeed;
    public float chaseSpeed;
    [SerializeField] float currentSpeed;
    [SerializeField] Vector2 faceDir;
    public float hurtForce;

    [Header("计时器")]
    public float waitTime;
    float waitTimeCounter;
    [SerializeField] bool isWaiting;

    [Header("状态")]
    public bool isHurt;
    public bool isDead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        groundCheck = GetComponentInChildren<GroundCheck>();
        currentSpeed = normalSpeed;
        waitTimeCounter = waitTime;
    }

    private void Update()
    {
        faceDir = new Vector2(-transform.localScale.x, 0);
        if (groundCheck.touchLeftWall && faceDir.x < 0)
        {
            isWaiting = true;
        }
        if (faceDir.x > 0 && groundCheck.touchRightWall)
        {
            isWaiting = true;
        }
        WaitCounter();
    }

    private void FixedUpdate()
    {
        if (!isHurt)
        {
            Move();
        }
    }
    public virtual void Move()
    {
        rb.velocity = new Vector2(faceDir.x * currentSpeed, rb.velocity.y);
    }

    public void Flip()
    {
        transform.localScale = new Vector3(faceDir.x, 1, 1);
    }

    public void WaitCounter()
    {
        if (isWaiting)
        {
            waitTimeCounter -= Time.deltaTime;
            if (waitTimeCounter <= 0)
            {
                isWaiting = false;
                waitTimeCounter = waitTime;
                Flip();
            }
        }
    }

    public void GetHurt(Transform attackerTrans)
    {
        attacker = attackerTrans;
        rb.velocity = Vector2.zero;
        if (attacker.position.x - transform.position.x >= 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        if (attacker.position.x - transform.position.x < 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        isHurt = true;
        Vector2 dir = new Vector2(transform.position.x - attacker.position.x, 0).normalized;
        animator.SetTrigger("hurt");
        StartCoroutine(OnHurt(dir));
    }

    public IEnumerator OnHurt(Vector2 dir)
    {
        rb.AddForce(dir * hurtForce, ForceMode2D.Impulse);
        yield return new WaitForSeconds(0.8f);
        isHurt = false;
    }

    public void OnDie()
    {
        gameObject.layer = 2;
        rb.velocity = Vector2.zero;
        isDead = true;
        animator.SetBool("isDead", true);
    }

    public void DestroyAfterAnimation()
    {
        Destroy(this.gameObject);
    }
}