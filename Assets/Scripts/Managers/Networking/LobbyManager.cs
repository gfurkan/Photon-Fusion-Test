using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using Networking;
using UnityEngine;
using Random = UnityEngine.Random;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    #region Fields

    [SerializeField] private NetworkRunner _runnerPrefab;
    [SerializeField] private NetworkObject gameManagerPrefab;
    
    [SerializeField] private PlayerListEntry playerListEntryPrefab;
    [SerializeField] private Transform playerListParent;
    
    [SerializeField] private GameObject sessionEntryPrefab;
    [SerializeField] private Transform sessionListParent;

    private NetworkRunner _runner;
    private List<SessionInfo> sessionList = new List<SessionInfo>();

    #endregion

    #region Unity Methods

    private void OnEnable()
    {
        GameManager.OnPlayerInfoAdded += UpdatePlayerList;
        GameManager.OnPlayerInfoChanged += UpdatePlayerList;
    }

    private void OnDisable()
    {
        GameManager.OnPlayerInfoAdded -= UpdatePlayerList;
        GameManager.OnPlayerInfoChanged -= UpdatePlayerList;
    }
    private void Start()
    {
        CreateRunner();
        JoinLobby();
        
    }

    #endregion

    #region Private Methods

    private void CreateRunner()
    {
        if (_runner == null)
        {
            _runner = Instantiate(_runnerPrefab);
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
        }
    }

    private async void JoinLobby()
    {
        await _runner.JoinSessionLobby(SessionLobby.Shared);
        LobbyUIManager.Instance.OpenMenu(0);
    }
    
    private void UpdatePlayerList(PlayerRef playerRef, PlayerInfo playerInfo)
    {
    
        foreach (Transform child in playerListParent.transform)
        {
            Destroy(child.gameObject);
        }
        var sortedPlayerInfos = GameManager.Instance.PlayerInfos
            .OrderBy(p => p.Key.RawEncoded)
            .ToList();
    
        foreach (var data in sortedPlayerInfos)
        {
            var entry = Instantiate(playerListEntryPrefab, playerListParent.transform);
            entry.SetPlayerData(data.Value.IsReady, data.Value.PlayerName.ToString(), data.Key);
        }
    }
    
    private void HostMigrationResume(NetworkRunner _runner)
    {
        foreach (var resumeNO in _runner.GetResumeSnapshotNetworkObjects())
        {
            if (resumeNO.TryGetBehaviour<NetworkTransform>(out var posRot))
            {
                _runner.Spawn(resumeNO, onBeforeSpawned: (_runner, newNO) =>
                {
                    newNO.CopyStateFrom(resumeNO);
                    
                    if (resumeNO.TryGetBehaviour<NetworkBehaviour>(out var customBehaviour))
                    {
                        newNO.GetComponent<NetworkBehaviour>().CopyStateFrom(customBehaviour);
                    }
                });
            }
        }
    }
    #endregion

    #region Public Methods

    public async void HostSession()
    {
        var result = await _runner.StartGame(new StartGameArgs
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
            
            if (_runner.IsServer)
            {
                await _runner.SpawnAsync(gameManagerPrefab, Vector3.zero, Quaternion.identity, _runner.LocalPlayer);
                string randomName = "Player" + Random.Range(0, 999);
                
                GameManager.Instance.AddPlayerInfo(_runner.LocalPlayer,randomName);
                Debug.Log("GameManager spawned by host.");
            }
        }
        else
        {
            Debug.LogError("Host failed: " + result.ShutdownReason);
        }
    }

    public async void JoinSession(string sessionName)
    {
        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex(1),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            LobbyUIManager.Instance.OpenMenu(3);
            Debug.Log("Joined session: " + sessionName);
            
            string randomName = "Player" + Random.Range(0, 999);
            await Task.Delay(100);
            
            GameManager.Instance.AddPlayerInfo(_runner.LocalPlayer,randomName);
        }
        else
        {
            Debug.LogError("Join failed: " + result.ShutdownReason);
        }
    }

    public async void QuickJoin()
    {
        if (sessionList.Count > 0)
        {
            var randomSession = sessionList[Random.Range(0, sessionList.Count)];
            JoinSession(randomSession.Name);
            
            Debug.Log("Quick joined session: " + randomSession.Name);
            string randomName = "Player" + Random.Range(0, 999);
            
            await Task.Delay(100);
            GameManager.Instance.AddPlayerInfo(_runner.LocalPlayer,randomName);
        }
        else
        {
            Debug.LogWarning("No available sessions to join.");
        }
    }

    public async void QuitJoin()
    {
        if (_runner != null)
        {
            await _runner.Shutdown();
            _runner = null;
            CreateRunner();
            
            LobbyUIManager.Instance.SetMenuTransitionText("Leaving Session...");
            Debug.Log("Leaving session...");
            
            JoinLobby();
        }
    }
    public void UpdatePlayerList()
    {
        foreach (Transform child in playerListParent.transform)
        {
            Destroy(child.gameObject);
        }

        var sortedPlayerInfos = GameManager.Instance.PlayerInfos
            .OrderBy(p => p.Key.RawEncoded)
            .ToList();

        foreach (var data in sortedPlayerInfos)
        {
            var entry = Instantiate(playerListEntryPrefab, playerListParent.transform);
            entry.SetPlayerData(data.Value.IsReady, data.Value.PlayerName.ToString(), data.Key);
        }
        Debug.Log("Update List If a Player Lefts");
    }
    
    #endregion

    #region Network Events

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
    
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (player != runner.LocalPlayer)
        {
            GameManager.Instance.RemovePlayerInfo(player);
            UpdatePlayerList();
        }
    }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason) 
    {
        if (reason == ShutdownReason.HostMigration)
        {
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
        await runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);
        var newRunner = Instantiate(_runnerPrefab);
        
        newRunner.ProvideInput = true;
        newRunner.AddCallbacks(this);
        
        this._runner = newRunner;
        StartGameResult result = await newRunner.StartGame(new StartGameArgs()
        {
            HostMigrationToken = hostMigrationToken, 
            HostMigrationResume = HostMigrationResume,
        });
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

    #endregion
    
}
