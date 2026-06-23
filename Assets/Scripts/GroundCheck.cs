using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    public Transform checkPoint;
    public float checkRadius;
    public Vector2 offset;
    public LayerMask groundLayer;

    public bool isGrounded;

    private void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle((Vector2)checkPoint.position + offset, checkRadius, groundLayer);
    }

    public void OnDrawGizmosSelected()
    {
        if (checkPoint == null) return;
        Gizmos.DrawWireSphere((Vector2)checkPoint.position + offset, checkRadius);
    }
}
