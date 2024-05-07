using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static HealthSystem;

public class HealthSystem : NetworkBehaviour
{
    [SerializeField] float          maxHealth = 100.0f;
    [SerializeField] GameObject     healthDisplay;
    [SerializeField] Image          fill;
    [SerializeField] GameObject[]   loot;
    [SerializeField] Color          flashColor = Color.white;


    private NetworkVariable<float>  health = new NetworkVariable<float>(100.0f);
    private Flasher                 flasher;

    public bool isDead => (health.Value <= 0.0f);

    public delegate void OnDeath();
    public event OnDeath onDeath;

    void Start()
    {
        flasher = GetComponent<Flasher>();

        health.Value = maxHealth;

        health.OnValueChanged += HealthChanged;
    }

    private void HealthChanged(float previousValue, float newValue)
    {
        if (NetworkManager.Singleton.IsClient)
        {
            flasher.Flash(flashColor, 0.2f);

            float p = Mathf.Clamp01(newValue / maxHealth);
            if (fill)
            {
                fill.transform.localScale = new Vector3(p, 1.0f, 1.0f);
            }

            if ((healthDisplay != null) && (p <= 0.0f))
            {
                healthDisplay.SetActive(false);
            }
        }
    }

    public void DealDamage(float damage)
    {
        if (NetworkManager.IsServer)
        {
            health.Value = Mathf.Clamp(health.Value - damage, 0, maxHealth);

            if (isDead)
            {
                if (loot.Length > 0)
                {
                    var drop = loot[UnityEngine.Random.Range(0, loot.Length)];

                    var spawnedObject = Instantiate(drop, transform.position, Quaternion.identity);
                    var prefabNetworkObject = spawnedObject.GetComponent<NetworkObject>();

                    prefabNetworkObject.Spawn(true);
                }

                if (onDeath != null) onDeath();
            }
        }
    }
}
