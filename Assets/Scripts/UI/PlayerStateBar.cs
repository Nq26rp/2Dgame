using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStateBar : MonoBehaviour
{
    public Image healthImage;
    public Image healthDelayImage;


    private void Update()
    {
        if (healthDelayImage.fillAmount > healthImage.fillAmount)
        {
            healthDelayImage.fillAmount -= Time.deltaTime;
        }
    }

    public void OnHealthChange(float percentage)
    {
        healthImage.fillAmount = percentage;
    }
}
