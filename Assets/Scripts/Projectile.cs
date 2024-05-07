using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    public Faction  faction;
    public float    speed = 200.0f;
    public float    damage = 100.0f;
    public float    duration = 5.0f;
    public int      projectileId = -1;
    public ulong    playerId = 0;

    public NetworkVariable<ulong>   remotePlayerId = new NetworkVariable<ulong>(0);
    public NetworkVariable<int>     remoteProjectileId = new NetworkVariable<int>(-1);

    public float   shotTime = 0;
    public Vector3 origin;
    
    private Vector3     prevPos;
    private Projectile  linkedShot = null;

    private void Start()
    {
        prevPos = origin;
    }

    void Update()
    {
        transform.position = origin + transform.up * (NetworkManager.ServerTime.TimeAsFloat - shotTime) * speed;

        if (NetworkManager.IsServer)
        {
            duration -= Time.deltaTime;
            if (duration <= 0.0f)
            {
                Destroy(gameObject);
            }
            else
            {
                // Improved collision detection, works better on server side
                var hits = Physics2D.LinecastAll(prevPos, transform.position);
                foreach (var hit in hits)
                {
                    // Check if collision was with something with a health system that's still alive
                    var character = hit.collider.GetComponent<Character>();
                    if ((character != null) && (!character.isDead) && (character.faction != faction))
                    {
                        // Check factions
                        switch (faction)
                        {
                            case Faction.Player:
                                if (hit.collider.GetComponent<Enemy>() == null) return;
                                break;
                            case Faction.Enemy:
                                if (hit.collider.GetComponent<Wyzard>() == null) return;
                                break;
                        }

                        // Run damage
                        character.DealDamage(damage);

                        Destroy(gameObject);
                    }
                }
                prevPos = transform.position;
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsClient && !IsServer) // Client and not the host
        {
            DestroyLocalProjectile(remoteProjectileId.Value, remotePlayerId.Value);
        }
    }

    private void DestroyLocalProjectile(int projectileId, ulong playerId)
    {
        Projectile[] projectiles = FindObjectsOfType<Projectile>();
        foreach (var proj in projectiles)
        {
            // Use the new value of ProjectileId to find the predicted projectile
            if ((proj.playerId == playerId) && (proj.projectileId == projectileId) && (proj != this))
            {
                // Disable this renderer, because we already have one
                GetComponent<SpriteRenderer>().enabled = false;
                linkedShot = proj;
                break;
            }
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (linkedShot != null)
        {
            Destroy(linkedShot.gameObject);
        }
    }

/*    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Only server detects collisions
        if (IsServer)
        {
            // Check if collision was with something with a health system that's still alive
            var healthSystem = collision.GetComponent<HealthSystem>();
            if ((healthSystem != null) && (!healthSystem.isDead))
            {
                // Check factions
                switch (faction)
                {
                    case Faction.Player:
                        if (collision.GetComponent<Enemy>() == null) return;
                        break;
                    case Faction.Enemy:
                        if (collision.GetComponent<Wyzard>() == null) return;
                        break;
                }

                // Run damage
                healthSystem.DealDamage(damage);

                Destroy(gameObject);
            }
        }
    }*/
}
