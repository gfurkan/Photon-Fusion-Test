using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FusionLobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private NetworkObject gameManagerPrefab;
    [SerializeField] private GameObject sessionEntryPrefab;
    [SerializeField] private Transform sessionListParent;

    private NetworkRunner runner;
    private List<SessionInfo> sessionList = new List<SessionInfo>();

    private void Start()
    {
        CreateRunner();
        JoinLobby();
    }

    private void CreateRunner()
    {
            print("----------------  created runner");
        if (runner == null)
        {
            runner = Instantiate(runnerPrefab);
            runner.ProvideInput = true;
            runner.AddCallbacks(this);
            print("++++++++++++++++++++  created runner");
        }
    }

    private async void JoinLobby()
    {
        await runner.JoinSessionLobby(SessionLobby.Shared);
        LobbyUIManager.Instance.OpenMenu(0);
    }

    public async void HostSession()
    {
        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = LobbyUIManager.Instance.SessionName,
            Scene = SceneRef.FromIndex(1),
            PlayerCount = LobbyUIManager.Instance.MaxPlayerCount,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log("Session hosted: " + LobbyUIManager.Instance.SessionName);
            LobbyUIManager.Instance.OpenMenu(3);
        }
        else
        {
            Debug.LogError("Host failed: " + result.ShutdownReason);
        }
    }

    public async void JoinSession(string sessionName)
    {
        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            LobbyUIManager.Instance.OpenMenu(3);
            Debug.Log("Joined session: " + sessionName);
        }
        else
        {
            Debug.LogError("Join failed: " + result.ShutdownReason);
        }
    }

    public async void QuickJoin()
    {
        // Oyun lobi listesine ulaşın
        if (sessionList.Count > 0)
        {
            // Mevcut oturumlardan birine rastgele katıl
            var randomSession = sessionList[UnityEngine.Random.Range(0, sessionList.Count)];
            JoinSession(randomSession.Name);
            Debug.Log("Quick joined session: " + randomSession.Name);
        }
        else
        {
            Debug.LogWarning("No available sessions to join.");
        }
    }

    public async void QuitJoin()
    {
        // Oturumdan çıkın ve lobiye geri dönün
        if (runner != null)
        {
            await runner.Shutdown();
            runner = null;
            CreateRunner();
            LobbyUIManager.Instance.SetMenuTransitionText("Leaving Session...");
            Debug.Log("Leaving session...");
            JoinLobby(); // Lobiye geri dön
        }
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> updatedList)
    {
        sessionList = updatedList;

        foreach (Transform child in sessionListParent)
            Destroy(child.gameObject);

        foreach (var session in sessionList)
        {
            if (session.IsVisible && session.IsOpen)
            {
                var entry = Instantiate(sessionEntryPrefab, sessionListParent);
                entry.GetComponent<SessionListEntry>().Setup(session.Name, session.PlayerCount, session.MaxPlayers, () => JoinSession(session.Name));
            }
        }
    }
    void HostMigrationResume(NetworkRunner runner)
    {
        // On host migration, we resume the session by restoring NetworkObject state
        foreach (var resumeNO in runner.GetResumeSnapshotNetworkObjects())
        {
            if (resumeNO.TryGetBehaviour<NetworkTransform>(out var posRot))
            {
                runner.Spawn(resumeNO, onBeforeSpawned: (runner, newNO) =>
                {
                    // Copy state from the old NetworkObject to the new one
                    newNO.CopyStateFrom(resumeNO);

                    // Optionally, copy partial state (e.g., custom NetworkBehaviour)
                    if (resumeNO.TryGetBehaviour<NetworkBehaviour>(out var customBehaviour))
                    {
                        newNO.GetComponent<NetworkBehaviour>().CopyStateFrom(customBehaviour);
                    }
                });
            }
        }
    }
    // --- Empty INetworkRunnerCallbacks ---
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { print("Player joined"); }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason) 
    {
        if (reason == ShutdownReason.HostMigration)
        {
            // Handle the case where the shutdown reason is host migration
            Debug.Log("Shutdown due to Host Migration.");
        }
        else
        {
            Debug.Log("Shutdown due to unknown reason.");
        }
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        // Shutdown the old runner, as host is migrating
        await runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);
        var newRunner = Instantiate(runnerPrefab);
        this.runner = newRunner;
        print("Runner created +++++++++++++++++++++++++++");

        // Start the new Runner using the HostMigrationToken and pass the callback to resume the session
        StartGameResult result = await newRunner.StartGame(new StartGameArgs()
        {
            HostMigrationToken = hostMigrationToken,  // This is necessary to resume the session
            HostMigrationResume = HostMigrationResume, // This callback will be used to resume the session
            // Other args like session settings can go here
        });

        // Check if the new runner successfully started
        if (result.Ok)
        {
            Debug.Log("Host migration succeeded, new host started successfully.");
        }
        else
        {
            Debug.LogWarning("Host migration failed: " + result.ShutdownReason);
        }
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
