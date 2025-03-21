using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    #region Fields

    [SerializeField] private NetworkRunner _runnerPrefab;
    [SerializeField] private NetworkObject _gameManagerPrefab;
    [SerializeField] private PlayerListEntry _playerListEntryPrefab;
    [SerializeField] private Transform _playerListParent;
    [SerializeField] private GameObject _sessionEntryPrefab;
    [SerializeField] private Transform _sessionListParent;
    
    private NetworkRunner _runner;
    private List<SessionInfo> _sessionList = new List<SessionInfo>();

    #endregion

    #region Unity Methods

    private void OnEnable()
    {
        GameManager.OnPlayerInfoAdded += RefreshPlayerList;
        GameManager.OnPlayerInfoChanged += RefreshPlayerList;
    }

    private void OnDisable()
    {
        GameManager.OnPlayerInfoAdded -= RefreshPlayerList;
        GameManager.OnPlayerInfoChanged -= RefreshPlayerList;
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
        Debug.Log("Connected to Server.");
    }

    private void RefreshPlayerList(PlayerRef playerRef, PlayerInfo playerInfo) => RefreshPlayerList();
    
    private void RefreshPlayerList()
    {
        foreach (Transform child in _playerListParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var data in GameManager.Instance.PlayerInfos.OrderBy(p => p.Key.RawEncoded))
        {
            var entry = Instantiate(_playerListEntryPrefab, _playerListParent);
            entry.SetPlayerData(data.Value.IsReady, data.Value.PlayerName.ToString(), data.Key);
        }

        Debug.Log("Player list updated.");
    }

    #endregion

    #region Public Methods

    public async void HostSession()
    {
        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = LobbyUIManager.Instance.SessionName,
            Scene = SceneRef.FromIndex(0),
            PlayerCount = LobbyUIManager.Instance.MaxPlayerCount,
            SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            LobbyUIManager.Instance.OpenMenu(3);

            if (_runner.IsServer)
            {
                await _runner.SpawnAsync(_gameManagerPrefab, Vector3.zero, Quaternion.identity, _runner.LocalPlayer);
                string randomName = $"Player{Random.Range(0, 999)}";
                GameManager.Instance.AddPlayerInfo(_runner.LocalPlayer, randomName);
                Debug.Log($"Created a room and joined as host");
            }
        }
        else
        {
            Debug.LogError($"Host failed: {result.ShutdownReason}");
        }
    }

    public async void JoinSession(string sessionName)
    {
        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex(0),
            SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            LobbyUIManager.Instance.OpenMenu(3);
            string randomName = $"Player{Random.Range(0, 999)}";

            Debug.Log("Joined room as client.");
            await Task.Delay(500); // Ensure GameManager instance exists
            GameManager.Instance.AddPlayerInfo(_runner.LocalPlayer, randomName);
        }
        else
        {
            Debug.LogError($"Join failed: {result.ShutdownReason}");
        }
    }

    public void QuickJoin()
    {
        if (_sessionList.Count > 0)
        {
            var randomSession = _sessionList[Random.Range(0, _sessionList.Count)];
            JoinSession(randomSession.Name);
            Debug.Log("Joined a random room as client.");
        }
        else
        {
            Debug.LogWarning("No sessions available to quick join.");
        }
    }
    
    public async void QuitRoom()
    {
        if (_runner != null)
        {
            await SceneManager.LoadSceneAsync(0); // Scene transition when quiting room.
            await _runner.Shutdown();
            
            _runner = null;
            CreateRunner();

            LobbyUIManager.Instance.SetMenuTransitionText("Leaving Session...");
            JoinLobby();
        }
    }

    #endregion

    #region Used Network Callbacks

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> updatedList)
    {
        _sessionList = updatedList;

        if (_sessionListParent != null)
        {
            foreach (Transform child in _sessionListParent)
                Destroy(child.gameObject);

            foreach (var session in _sessionList.Where(s => s.IsVisible && s.IsOpen))
            {
                var entry = Instantiate(_sessionEntryPrefab, _sessionListParent);
                entry.GetComponent<SessionListEntry>().Setup(session.Name, session.PlayerCount, session.MaxPlayers, () => JoinSession(session.Name));
            }
            Debug.Log("Room list updated.");
        }
      
    }
       

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("Player left room.");
        if (player != runner.LocalPlayer)
        {
            GameManager.Instance.RemovePlayerInfo(player);
            Debug.Log("The quiting player's infos are deleted.");
            RefreshPlayerList();
        }
        
    }

    public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        await runner.Shutdown(shutdownReason:ShutdownReason.HostMigration);

        var newRunner = Instantiate(_runnerPrefab);
        newRunner.ProvideInput = true;
        newRunner.AddCallbacks(this);
        _runner = newRunner;

        var result = await newRunner.StartGame(new StartGameArgs
        {
            HostMigrationToken = hostMigrationToken,
            HostMigrationResume = r => Debug.Log("Host migration resumed.")
        });

        if (!result.Ok)
        {
            Debug.LogWarning($"Host migration failed: {result.ShutdownReason}");
        }
    }
    
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Material defaultSkyboxMaterial = new Material(Shader.Find("Skybox/Procedural"));
        RenderSettings.skybox = defaultSkyboxMaterial;
        DynamicGI.UpdateEnvironment();
    }
    
    #endregion


    #region Unused Network Callbacks

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    #endregion

}
