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
            healthDelayImage.fillAmount = Mathf.MoveTowards(
                healthDelayImage.fillAmount,
                healthImage.fillAmount,
                Time.deltaTime);
        }
    }

    public void OnHealthChange(float percentage)
    {
        percentage = Mathf.Clamp01(percentage);
        healthImage.fillAmount = percentage;

        // 受到伤害时保留延迟扣血效果；回血或重新开始时立即同步延迟血条。
        if (healthDelayImage.fillAmount < percentage)
        {
            healthDelayImage.fillAmount = percentage;
        }
    }

    public void SyncHealthImmediately()
    {
        healthDelayImage.fillAmount = healthImage.fillAmount;
    }
}
