using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoarPatrolState : BaseState
{
    public override void OnEnter(Enemy enemy)
    {
        currentEnemy = enemy;
    }

    public override void LogicUpdate()
    {
        if (!currentEnemy.groundCheck.isGrounded &&
        ((currentEnemy.groundCheck.touchLeftWall && currentEnemy.faceDir.x < 0) ||
        (currentEnemy.faceDir.x > 0 && currentEnemy.groundCheck.touchRightWall)))
        {
            currentEnemy.isWaiting = true;
            currentEnemy.animator.SetBool("isWalk", false);
        }
        else
        {
            currentEnemy.animator.SetBool("isWalk", true);
        }
    }
    
    public override void PhysicsUpdate()
    {
        throw new System.NotImplementedException();
    }

    public override void OnExit()
    {
        currentEnemy.animator.SetBool("isWalk", false);
    }
}
