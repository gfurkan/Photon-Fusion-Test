using System;
using Fusion;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using Task = System.Threading.Tasks.Task;

public struct PlayerInfo: INetworkStruct
{
    [Networked] public NetworkString<_16> PlayerName { get; set; }
    [Networked] public bool IsReady { get; set; }
}

public class GameManager : NetworkBehaviour
{
    #region Fields

    [Networked]  
    public NetworkDictionary<PlayerRef, PlayerInfo> PlayerInfos => default;
    public static event Action<PlayerRef, PlayerInfo> OnPlayerInfoChanged;
    public static event Action<PlayerRef, PlayerInfo> OnPlayerInfoAdded;

    #endregion

    #region Properties
    
    public static GameManager Instance { get; private set; }

    #endregion


    #region Unity Methods

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

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
        
        StartGame();
    }

    private async void StartGame()
    {
        Debug.Log("Tüm oyuncular hazır, yeni sahne yükleniyor...");
        await Task.Delay(500);
        await Runner.LoadScene(SceneRef.FromIndex(2), LoadSceneMode.Single);
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
            }
        }
        else
        {
            RPC_RequestAddPlayerInfo(PlayerName);
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
            }
        }
        else
        {
            Rpc_RequestChangeReadyState(isReady);
        }
    }
    
    public void RemovePlayerInfo(PlayerRef player)
    {
        if (Runner.IsServer && PlayerInfos.ContainsKey(player))
        {
            PlayerInfos.Remove(player);
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
    }

    #endregion
    
    

}
