using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoarChaseState : BaseState
{
    public override void OnEnter(Enemy enemy)
    {
        currentEnemy = enemy;
        currentEnemy.currentSpeed = currentEnemy.chaseSpeed;
        currentEnemy.lostTimeCounter = currentEnemy.lostTime;
        currentEnemy.animator.SetBool("isRun", true);
    }

    public override void LogicUpdate()
    {
        if (currentEnemy.lostTimeCounter <= 0)
        {
            currentEnemy.ChangeState(EnemyState.Patrol);
        }
        if (currentEnemy.groundCheck.isGrounded &&
     ((currentEnemy.groundCheck.touchLeftWall && currentEnemy.faceDir.x < 0) ||
     (currentEnemy.faceDir.x > 0 && currentEnemy.groundCheck.touchRightWall)))
        {
            currentEnemy.transform.localScale = new Vector3(currentEnemy.faceDir.x, 1, 1);
        }
    }
    public override void PhysicsUpdate()
    {
        
    }
    public override void OnExit()
    {
        currentEnemy.animator.SetBool("isRun", false);
        currentEnemy.lostTimeCounter = 0;
    }
}
