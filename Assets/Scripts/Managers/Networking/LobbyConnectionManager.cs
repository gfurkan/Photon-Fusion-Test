using System;
using Fusion;
using Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using Task = System.Threading.Tasks.Task;

public struct PlayerInfo: INetworkStruct
{
    [Networked] public NetworkString<_16> PlayerName { get; set; }
    [Networked] public bool IsReady { get; set; }
}

public class LobbyConnectionManager : SingletonNetworkManager<LobbyConnectionManager>
{
    #region Fields
    [SerializeField] private NetworkObject _gameManagerPrefab;
    [Networked,HideInInspector] public NetworkDictionary<PlayerRef, PlayerInfo> PlayerInfos => default;
    
    public static event Action<PlayerRef, PlayerInfo> OnPlayerInfoChanged;
    public static event Action<PlayerRef, PlayerInfo> OnPlayerInfoAdded;

    #endregion

    #region Private Methods

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestAddPlayerInfo(string PlayerName, RpcInfo info = default)
    {
        AddPlayerInfo(info.Source, PlayerName);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPlayerAdded(PlayerRef player, PlayerInfo playerInfo)
    {
        OnPlayerInfoAdded?.Invoke(player,playerInfo);
    }
 
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_RequestChangeReadyState(bool isReady, RpcInfo info = default)
    {
        ChangePlayerReadyState(info.Source, isReady);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayerInfoChanged(PlayerRef player, PlayerInfo playerInfo)
    {
        OnPlayerInfoChanged?.Invoke(player, playerInfo);
        if (Runner.IsServer)
        {
            CheckAllPlayersReady();
        }
    }

    private void CheckAllPlayersReady()
    {
        if (!Runner.IsServer) return;

        foreach (var playerInfo in PlayerInfos)
        {
            if (!playerInfo.Value.IsReady)
                return;
        }
        
        StartGame(Runner);
    }

    private async void StartGame(NetworkRunner runner)
    {
        Debug.Log("All players ready, loading gameplay scene...");
        await Task.Delay(500);
        await runner.LoadScene(SceneRef.FromIndex(2), LoadSceneMode.Single);
        
        runner.Spawn(_gameManagerPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
        Debug.Log("GameManager gameplay sahnesinde spawn edildi.");
    }
    #endregion
    
    #region Public Methods
    
    public void AddPlayerInfo(PlayerRef player, string PlayerName)
    {
        if (Runner.IsServer)
        {
            if (!PlayerInfos.ContainsKey(player))
            {
                var playerInfo = new PlayerInfo
                {
                    PlayerName = PlayerName,
                    IsReady = false
                };
                PlayerInfos.Add(player, playerInfo);
                RPC_NotifyPlayerAdded(player,playerInfo);
                
                Debug.Log("New player info added by host");
            }
        }
        else
        {
            RPC_RequestAddPlayerInfo(PlayerName);
            Debug.Log("New player info add request sent by client");
        }
    }
    
    public void ChangePlayerReadyState(PlayerRef player, bool isReady)
    {
        if (Runner.IsServer)
        {
            if (PlayerInfos.TryGet(player, out var playerInfo))
            {
                playerInfo.IsReady = isReady;
                PlayerInfos.Set(player, playerInfo);
                Rpc_PlayerInfoChanged(player, playerInfo);
                Debug.Log("Player ready state info changed by host");
            }
        }
        else
        {
            Rpc_RequestChangeReadyState(isReady);
            Debug.Log("Player ready state info change request sent by client");
        }
    }
    
    public void RemovePlayerInfo(PlayerRef player)
    {
        if (Runner.IsServer && PlayerInfos.ContainsKey(player))
        {
            PlayerInfos.Remove(player);
            Debug.Log("Player info removed > Player" + player.PlayerId);
        }
    }
    
    
    #endregion

    #region Override Methods

    public override void Spawned()
    {
        base.Spawned();
        foreach (var info in PlayerInfos)
        {
            OnPlayerInfoChanged?.Invoke(info.Key, info.Value);
        }
        Debug.Log("Player infos synced when the joining room");
    }

    #endregion
    
}

