using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Wyzard : Character
{
    [SerializeField] private Transform              arm;
    [SerializeField] private NetworkVariable<float> cooldown = new NetworkVariable<float>(0.25f);
    [SerializeField] private NetworkVariable<float> damage = new NetworkVariable<float>(10.0f);
    [SerializeField] private Projectile             shotPrefab;
    [SerializeField] private Projectile             shotPrefabNetwork;
    [SerializeField] private Transform              shootPoint;
    [SerializeField] private ParticleSystem         levelUpPS;

    float                   cooldownTimer;
    NetworkVariable<int>    _level = new NetworkVariable<int>(1);
    NetworkVariable<int>    _xp = new NetworkVariable<int>(0);
    NetworkVariable<int>    _maxXP = new NetworkVariable<int>(15);

    public int level => _level.Value;
    public int xp => _xp.Value;
    public int maxXP => _maxXP.Value;

    protected override void Start()
    {
        base.Start();

        cooldownTimer = cooldown.Value;
    }

    void Update()
    {
        if (isDead)
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }
            return;
        }

        if (networkObject.IsLocalPlayer)
        {
            Vector3 moveDir = Vector3.zero;
            moveDir.x = speed * Input.GetAxis("Horizontal");
            moveDir.y = speed * Input.GetAxis("Vertical");

            moveDir *= Time.deltaTime;

            transform.Translate(moveDir, Space.World);
        }

        var enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Enemy closestEnemy = null;
        float minDist = float.MaxValue;

        // Find closest
        foreach (var enemy in enemies)
        {
            if (enemy.isDead) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closestEnemy = enemy;
            }
        }

        Vector3 targetVector = Vector3.down;
        if (closestEnemy != null)
        {
            targetVector = (closestEnemy.transform.position + (Vector3.up * 8) - arm.transform.position).normalized;
        }
        
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, targetVector);

        arm.transform.rotation = Quaternion.RotateTowards(arm.transform.rotation, targetRotation, Time.deltaTime * 360.0f);

        if ((networkObject.IsLocalPlayer) && (closestEnemy != null))
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0.0f)
            {
                Shoot(shootPoint.position, shootPoint.rotation);

                cooldownTimer = cooldown.Value;
            }
        }

        UpdateAnimation();
    }

    protected void Shoot(Vector3 pos, Quaternion rotation)
    {
        var spawnedObject = Instantiate(shotPrefab, pos, rotation);
        spawnedObject.projectileId = projectileId;
        spawnedObject.playerId = OwnerClientId;
        spawnedObject.origin = pos;
        spawnedObject.shotTime = NetworkManager.ServerTime.TimeAsFloat;
        spawnedObject.damage = damage.Value;

        RequestShootServerRpc(pos, rotation, projectileId, NetworkManager.ServerTime.TimeAsFloat, OwnerClientId);

        projectileId++;
    }

    [ServerRpc]
    void RequestShootServerRpc(Vector3 position, Quaternion rotation, int projectileId, float shotTime, ulong playerId)
    {
        var spawnedObject = Instantiate(shotPrefabNetwork, position, rotation);
        spawnedObject.origin = position;
        spawnedObject.shotTime = shotTime;
        spawnedObject.remoteProjectileId.Value = projectileId;
        spawnedObject.remotePlayerId.Value = playerId;
        spawnedObject.damage = damage.Value;
        var prefabNetworkObject = spawnedObject.GetComponent<NetworkObject>();

        prefabNetworkObject.Spawn(true);
    }

    public static Wyzard GetLocalPlayer()
    {
        var players = FindObjectsOfType<Wyzard>();
        foreach (var player in players)
        {
            if (player.IsLocalPlayer)
            {
                return player;
            }
        }

        return null;
    }

    public static Wyzard GetFirstPlayer()
    {
        var players = FindObjectsOfType<Wyzard>();
        foreach (var player in players)
        {
            if (!player.isDead)
            {
                return player;
            }
        }

        return null;
    }

    public void AddXP(int ammount)
    {
        if (IsServer)
        {
            _xp.Value += ammount;

            if (_xp.Value >= _maxXP.Value)
            {
                _xp.Value -= _maxXP.Value;
                _maxXP.Value = (int)(_maxXP.Value * 1.5f);
                _level.Value++;

                LevelUpClientRpc();

                var clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { OwnerClientId }
                    }
                };

                SelectPowerupClientRpc(clientRpcParams);
            }
        }
    }

    [ClientRpc]
    void LevelUpClientRpc()
    {
        levelUpPS.Play();
    }

    [ClientRpc]
    void SelectPowerupClientRpc(ClientRpcParams clientRpcParams = default)
    {
        var powerupSelector = FindObjectOfType<PowerupSelector>(true);
        powerupSelector.gameObject.SetActive(true);
    }

    public void Upgrade(string upgradeName)
    {
        UpgradeClientServerRpc(upgradeName);
    }

    [ServerRpc]
    public void UpgradeClientServerRpc(string upgradeName)
    {
        switch (upgradeName)
        {
            case "LessCooldown":
                cooldown.Value *= 0.9f;
                break;
            case "MoreDamage":
                damage.Value *= 1.25f;
                break;
            default:
                break;
        }
    }
}
