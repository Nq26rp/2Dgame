using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    Rigidbody2D rb;
    public Animator animator;
    public GroundCheck groundCheck;
    public Transform attacker;

    [Header("基本参数")]
    public float normalSpeed;
    public float chaseSpeed;
    float currentSpeed;
    public Vector2 faceDir;
    public float hurtForce;

    [Header("计时器")]
    public float waitTime;
    float waitTimeCounter;
    public bool isWaiting;

    [Header("状态")]
    public bool isHurt;
    public bool isDead;

    protected BaseState currentState;
    protected BaseState patrolState;
    protected BaseState chaseState;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        groundCheck = GetComponentInChildren<GroundCheck>();
        currentSpeed = normalSpeed;
        waitTimeCounter = waitTime;
    }

    private void OnEnable()
    {
        currentState = patrolState;
        currentState.OnEnter(this);
    }

    private void Update()
    {
        faceDir = new Vector2(-transform.localScale.x, 0);
        currentState.LogicUpdate();
        WaitCounter();
    }

    private void FixedUpdate()
    {
        if (!isHurt && !isDead && !isWaiting)
        {
            Move();
        }
        currentState.PhysicsUpdate();
    }

    private void OnDisable()
    {
        currentState.OnExit();
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