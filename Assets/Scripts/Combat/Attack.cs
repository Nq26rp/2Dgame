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

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Attack trigger enter: " + other.name);
        other.GetComponent<Character>()?.TakeDamage(this);
    }
}
