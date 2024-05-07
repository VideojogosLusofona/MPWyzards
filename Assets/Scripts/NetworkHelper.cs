using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;
using System;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
#endif

public class NetworkHelper : MonoBehaviour
{
    [SerializeField] private bool            localTest;
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private List<Wyzard>    playerPrefabs;
    [SerializeField] private List<Transform> playerSpawnLocations;

    private ushort          port = 7777;
    private NetworkManager  networkManager;
    private int             playerPrefabIndex = 0;
    static public NetworkHelper   instance;

    IEnumerator Start()
    {
        instance = this;

#if !UNITY_EDITOR
        localTest = false;
#endif

        bool host = false;

        string[] args = System.Environment.GetCommandLineArgs();

        // Process each argument
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--host")
            {
                host = true;
            }
            else
            {
                if (ushort.TryParse(args[i], out ushort p))
                {
                    port = p;
                }
            }
        }

        networkManager = GetComponent<NetworkManager>();
        networkManager.enabled = true;
        var transport = GetComponent<UnityTransport>();
        transport.ConnectionData.Port = port;
        transport.enabled = true;

        yield return null;

        if (localTest)
        {
            if (networkManager.StartHost())
            {
                Log($"Hosting on port {transport.ConnectionData.Port}...");
            }
            else
            {
                Log($"Failed to host on port {transport.ConnectionData.Port}...");
            }
        }
        else if (host)
        {
            if (networkManager.StartServer())
            {
                Log($"Serving on port {transport.ConnectionData.Port}...");
            }
            else
            {
                Log($"Failed to serve on port {transport.ConnectionData.Port}...");
            }
        }
        else
        {
            if (networkManager.StartClient())
            {
                Log($"Connecting on port {transport.ConnectionData.Port}...");
            }
            else
            {
                Log($"Failed to connect on port {transport.ConnectionData.Port}...");
            }
        }

        if (networkManager.IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Log($"Player {clientId} connected, prefab index = {playerPrefabIndex}!");

        // Check a free spot for this player
        var spawnPos = Vector3.zero;
        var currentPlayers = FindObjectsOfType<Wyzard>();
        foreach (var playerSpawnLocation in playerSpawnLocations)
        {
            var closestDist = float.MaxValue;
            foreach (var player in currentPlayers)
            {
                float d = Vector3.Distance(player.transform.position, playerSpawnLocation.position);
                closestDist = Mathf.Min(closestDist, d);
            }
            if (closestDist > 20)
            {
                spawnPos = playerSpawnLocation.position;
                break;
            }
        }

        // Spawn player object
        var spawnedObject = Instantiate(playerPrefabs[playerPrefabIndex], spawnPos, Quaternion.identity);
        var prefabNetworkObject = spawnedObject.GetComponent<NetworkObject>();
        prefabNetworkObject.SpawnAsPlayerObject(clientId, true);
        prefabNetworkObject.ChangeOwnership(clientId);

        playerPrefabIndex = (playerPrefabIndex + 1) % playerPrefabs.Count;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Log($"Player {clientId} disconnected!");
    }

    public void Log(string txt)
    {
        if (logText)
        {
            logText.text += txt + "\n";
        }
    }

    private void Update()
    {
        var players = FindObjectsOfType<Wyzard>();
        if (players.Length > 0)
        {
            foreach (var player in players)
            {
                if (!player.isDead)
                {
                    // Still a player alive
                    return;
                }
            }

            var gameOver = FindObjectOfType<GameOver>(true);
            gameOver.gameObject.SetActive(true);
        }
        else
        {
            // If the server is up for 5 minutes and doesn't have any players, quit
            if (Time.time > 5 * 60)
            {
                Application.Quit();
            }
        }
    }

#if UNITY_EDITOR
    [MenuItem("Build/Build Windows (x64)", priority = 0)]
    public static bool BuildGame()
    {
        // Specify build options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
        buildPlayerOptions.locationPathName = Path.Combine("Builds", "MPWyzard.exe");
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        // Perform the build
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        // Output the result of the build
        Debug.Log($"Build ended with status: {report.summary.result}");

        // Check if the build was successful
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build was successful!");
        }
        else if (report.summary.result == BuildResult.Failed)
        {
            Debug.Log("Build failed.");
        }
        else if (report.summary.result == BuildResult.Cancelled)
        {
            Debug.Log("Build was cancelled.");
        }
        else if (report.summary.result == BuildResult.Unknown)
        {
            Debug.Log("Build result is unknown.");
        }

        // Additional information about the build can be logged
        Debug.Log($"Total errors: {report.summary.totalErrors}");
        Debug.Log($"Total warnings: {report.summary.totalWarnings}");

        return report.summary.result == BuildResult.Succeeded;
    }

    [MenuItem("Build/Build and Launch (Server + Client)", priority = 1)]
    public static void BuildAndLaunch2()
    {
        CloseAll();
        if (BuildGame())
        {
            Launch2();
        }
    }

    [MenuItem("Build/Launch (Server + Client)", priority = 2)]
    public static void Launch2()
    {
        Run("Builds\\MPWyzard.exe", "--host 7777");
        Run("Builds\\MPWyzard.exe", "");
    }


    [MenuItem("Build/Close All", priority = 5)]
    public static void CloseAll()
    {
        // Get all processes with the specified name
        Process[] processes = Process.GetProcessesByName("MPWyzard");

        foreach (var process in processes)
        {
            try
            {
                // Close the process
                process.Kill();
                // Wait for the process to exit
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                // Handle exceptions, if any
                // This could occur if the process has already exited or you don't have permission to kill it
                Debug.LogWarning($"Error trying to kill process {process.ProcessName}: {ex.Message}");
            }
        }
    }

    private static void Run(string path, string args)
    {
        // Start a new process
        Process process = new Process();

        // Configure the process using the StartInfo properties
        process.StartInfo.FileName = path;
        process.StartInfo.Arguments = args;
        process.StartInfo.WindowStyle = ProcessWindowStyle.Normal; // Choose the window style: Hidden, Minimized, Maximized, Normal
        process.StartInfo.RedirectStandardOutput = false; // Set to true to redirect the output (so you can read it in Unity)
        process.StartInfo.UseShellExecute = true; // Set to false if you want to redirect the output

        // Run the process
        process.Start();
    }
#endif
}
