using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private int   minPlayers = 2;
    [SerializeField] private float timeForFirstSpawn = 2;
    [SerializeField] private float spawnInterval = 10;
    [SerializeField] private int   spawnCount = 5;
    [SerializeField] private Enemy enemyPrefab;

    private NetworkManager networkManager;
    private float          spawnTimer;

    void Start()
    {
        networkManager = GetComponent<NetworkManager>();
        spawnTimer = timeForFirstSpawn;
    }

    // Update is called once per frame
    void Update()
    {
        if (networkManager.IsServer)
        {
            if (networkManager.ConnectedClients.Count >= minPlayers)
            {
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0.0f)
                {
                    Spawn();
                    spawnTimer = spawnInterval;
                }
            }
            else if (networkManager.ConnectedClients.Count == 0)
            {
                var enemies = FindObjectsOfType<Enemy>();
                foreach (var enemy in enemies)
                {
                    Destroy(enemy.gameObject);
                }
                spawnTimer = timeForFirstSpawn;
            }
        }
    }

    void Spawn()
    {
        // Get all players
        var wyzards = FindObjectsOfType<Wyzard>();
        if (wyzards.Length == 0) return;

        float xMin = wyzards[0].transform.position.x;
        float yMin = wyzards[0].transform.position.y;
        float xMax = xMin;
        float yMax = yMin;

        foreach (var wyzard in wyzards)
        {
            xMin = Mathf.Min(xMin, wyzard.transform.position.x);
            xMax = Mathf.Max(xMax, wyzard.transform.position.x);
            yMin = Mathf.Min(yMin, wyzard.transform.position.y);
            yMax = Mathf.Max(yMax, wyzard.transform.position.y);
        }

        for (int i = 0; i < spawnCount; i++)
        {
            float x = Random.Range(xMin - 20, xMax + 20);
            float y = Random.Range(yMin - 20, yMax + 20);

            var spawnedObject = Instantiate(enemyPrefab, new Vector3(x, y, 0), Quaternion.identity);
            var prefabNetworkObject = spawnedObject.GetComponent<NetworkObject>();

            prefabNetworkObject.Spawn(true);
        }
    }
}
