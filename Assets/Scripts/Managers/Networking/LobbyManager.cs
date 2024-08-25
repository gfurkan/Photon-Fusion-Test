using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;


namespace Networking
{
public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
#region Fields

public static LobbyManager _instance;

[SerializeField] private GameObject _sessionEntryPrefab;
[SerializeField] private Transform _sessionListContent;

private List<SessionInfo> _sessionList = new List<SessionInfo>();
private NetworkRunner _networkRunner;
private string _playerName;

#endregion

#region Properties

public static LobbyManager Instance => _instance;
public NetworkRunner NetworkRunner => _networkRunner;

#endregion

#region Unity Methods

private void Awake()
{
    _instance = this;
}

private  void Start()
{
    ConnectOnStart("Player1");
}

#endregion

#region Private Methods

 async void ConnectOnStart(string playerName)
{
    _playerName = playerName;
    if (_networkRunner == null)
    {
        _networkRunner = gameObject.AddComponent<NetworkRunner>();
    }

   await _networkRunner.JoinSessionLobby(SessionLobby.Shared);
   LobbyUIManager.Instance.OpenMenu(0);
}
#endregion

#region Public Methods

public void RefreshSessionListUI()
{
    foreach (Transform child in _sessionListContent)
    {
        Destroy(child.gameObject);
    }

    foreach (SessionInfo sessionInfo in _sessionList)
    {
        if (sessionInfo.IsVisible)
        {
            GameObject entry = GameObject.Instantiate(_sessionEntryPrefab, _sessionListContent);
            SessionListEntry sessionListEntry = entry.GetComponent<SessionListEntry>();
            sessionListEntry.SessionName.text = sessionInfo.Name;
            sessionListEntry.PlayerCount.text = $"{sessionInfo.PlayerCount}/{sessionInfo.MaxPlayers}";

            if (sessionInfo.IsOpen == false || sessionInfo.PlayerCount >= sessionInfo.MaxPlayers)
            {
                sessionListEntry.Button.interactable = false;
            }
            else
            {
                sessionListEntry.Button.interactable = true;
            }
        }
    }
}
public async void CreateSession()
{
    if (_networkRunner == null)
    {
        _networkRunner = gameObject.AddComponent<NetworkRunner>();
    }
    var startGameArgs = new StartGameArgs()
    {
        GameMode = GameMode.Shared, // Sunucu olarak lobi oluştur
        SessionName = LobbyUIManager.Instance.SessionName,
        PlayerCount = LobbyUIManager.Instance.MaxPlayerCount,// Lobi oturum adı
    };
    LobbyUIManager.Instance.SetMenuTransitionText("Creating Session...");
    var result = await _networkRunner.StartGame(startGameArgs);

    if (result.Ok)
    {
        Debug.Log("Lobi başarıyla oluşturuldu!");
        LobbyUIManager.Instance.OpenMenu(3);
    }
    else
    {
        Debug.LogError($"Lobi oluşturulamadı: {result.ShutdownReason}");
    }
}

// Lobiye girme (katılma)
public async void JoinSession(string sessionName)
{
    if (_networkRunner == null)
    {
        _networkRunner = gameObject.AddComponent<NetworkRunner>();
    }
    var startGameArgs = new StartGameArgs()
    {
        GameMode = GameMode.Shared, // Müşteri olarak lobiye katıl
        SessionName = sessionName, // Katılmak istenen lobi oturum adı
        PlayerCount = 2
    };
    LobbyUIManager.Instance.SetMenuTransitionText("Joining Session...");
    var result = await _networkRunner.StartGame(startGameArgs);

    if (result.Ok)
    {
        Debug.Log("Lobiye başarıyla katıldınız!");
        LobbyUIManager.Instance.OpenMenu(3);
    }
    else
    {
        Debug.LogError($"Lobiye katılım başarısız: {result.ShutdownReason}");
    }
}

// Hızlı giriş (quick join)
public async void QuickJoin()
{
    var startGameArgs = new StartGameArgs()
    {
        GameMode = GameMode.Client, // Hızlı giriş yapacak
        SessionName = "", // Oturum adı belirtmeyin
        SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
    };
    LobbyUIManager.Instance.SetMenuTransitionText("Joining a Random Session...");
    var result = await _networkRunner.StartGame(startGameArgs);

    if (result.Ok)
    {
        Debug.Log("Hızlı giriş başarılı!");
    }
    else
    {
        Debug.LogError($"Hızlı giriş başarısız: {result.ShutdownReason}");
    }
}
#endregion
public void OnConnectedToServer(NetworkRunner runner)
{
    Debug.Log("Ağa başarıyla bağlanıldı!");

}

// Bağlantı kesildiğinde çağrılır
public void OnDisconnectedFromServer(NetworkRunner runner)
{
    Debug.Log("Ağ bağlantısı kesildi.");
}

// Diğer oyuncular bağlandığında çağrılır
public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
{
    Debug.Log("Player başarıyla bağlanıldı!");

}

public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
{
    
}

public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
{
 
}

public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
{
   
}

public void OnInput(NetworkRunner runner, NetworkInput input)
{

}

public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
{

}

public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
{

}


public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
{

}

public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
{

}

public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
{

}

public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
{

}

public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
{
    _sessionList.Clear();
    _sessionList = sessionList;
    foreach (SessionInfo entry in _sessionList)
    {
        RefreshSessionListUI();
    }
}

public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
{

}

public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
{

}

public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
{

}

public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
{

}

public void OnSceneLoadDone(NetworkRunner runner)
{

}

public void OnSceneLoadStart(NetworkRunner runner)
{

}
}
}

