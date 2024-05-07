using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
public class Character : NetworkBehaviour
{
    [SerializeField] public     Faction faction;
    [SerializeField] protected  float speed = 50.0f;

    protected NetworkObject networkObject;
    protected Animator      animator;
    protected Vector3       prevPos;
    protected HealthSystem  healthSystem;
    protected int           projectileId = 0;

    public bool isDead => healthSystem.isDead;

    protected virtual void Start()
    {
        networkObject = GetComponent<NetworkObject>();
        animator = GetComponent<Animator>();
        healthSystem = GetComponent<HealthSystem>();
    }

    protected void UpdateAnimation()
    {
        Vector3 velocity = (transform.position - prevPos) / Time.deltaTime;

        animator.SetFloat("Speed", velocity.magnitude);

        if ((velocity.x < 0) && (transform.right.x > 0)) transform.rotation = Quaternion.Euler(0, 180, 0);
        else if ((velocity.x > 0) && (transform.right.x < 0)) transform.rotation = Quaternion.identity;

        prevPos = transform.position;
    }

    public void DealDamage(float damage)
    {
        healthSystem.DealDamage(damage);
    }
}
