using System;
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
public class Character : MonoBehaviour
{
    [Header("属性")]
    public float maxHealth;
    public float currentHealth;

    [Header("无敌时间")]
    public float invulnerableDuration;
    public bool invulnerable;
    public float invulnerableCounter;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update() {
        if (invulnerable)
        {
            invulnerableCounter -= Time.deltaTime;
            if (invulnerableCounter <= 0)
            {
                invulnerable = false;
            }
        }
    }

    public void TakeDamage(Attack attacker)
    {
        Debug.Log("attacker damage: " + attacker.damage);
        if (invulnerable) return;

        if (currentHealth - attacker.damage > 0)
        {
            currentHealth -= attacker.damage;
            TriggerInvulnerable();
            
        }
        else
        {
            currentHealth = 0;
            Debug.Log("Character is dead");
        }

    }

    public void TriggerInvulnerable()
    {
        if (!invulnerable)
        {
            invulnerable = true;
            invulnerableCounter = invulnerableDuration;
        }
    }
}
