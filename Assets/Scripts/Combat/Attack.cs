using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
• 创建 Boar 野猪木桩
• 碰撞体和触发器的使用
• 创建 Character 代码
• 创建 Attack 代码
• 实现伤害减少
*/
public class Attack : MonoBehaviour
{
    public int damage = 10;
    public float attackRange;
    public float attackRate;

    void OnTriggerEnter2D(Collider2D other)
    {
        Character target = other.GetComponent<Character>();
        if (target == null) return;
        target.TakeDamage(this);
    }
}
