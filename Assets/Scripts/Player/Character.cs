using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Character : MonoBehaviour
{
    [Header("属性")]
    public float maxHealth;
    public float currentHealth;

    [Header("无敌时间")]
    public float invulnerableDuration;
    public bool invulnerable;
    public float invulnerableCounter;
    [Header("事件")]
    public UnityEvent<Transform> OnTakeDamage;
    public UnityEvent OnDeath;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
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
            OnTakeDamage?.Invoke(attacker.transform);
        }
        else
        {
            currentHealth = 0;
            OnDeath?.Invoke();
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
