using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    public Transform checkPoint;
    public float checkRadius;
    public Vector2 offset;
    public Vector2 leftOffset;
    public Vector2 rightOffset;
    public LayerMask groundLayer;

    public bool isGrounded;
    public bool touchLeftWall;
    public bool touchRightWall;

    private void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle((Vector2)checkPoint.position + offset, checkRadius, groundLayer);
        touchLeftWall = Physics2D.OverlapCircle((Vector2)checkPoint.position + leftOffset, checkRadius, groundLayer);
        touchRightWall = Physics2D.OverlapCircle((Vector2)checkPoint.position + rightOffset, checkRadius, groundLayer);
    }

    public void OnDrawGizmosSelected()
    {
        if (checkPoint == null) return;
        Gizmos.DrawWireSphere((Vector2)checkPoint.position + offset, checkRadius);
        Gizmos.DrawWireSphere((Vector2)checkPoint.position + leftOffset, checkRadius);
        Gizmos.DrawWireSphere((Vector2)checkPoint.position + rightOffset, checkRadius);
    }
}
