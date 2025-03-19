using System.Linq;
using UnityEngine;
using Fusion;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private NetworkPrefabRef playerPrefab;   // Oyuncu prefab'ını buraya at
    [SerializeField] private NetworkPrefabRef ballPrefab;     // Top prefab'ını buraya at (istersen)

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
         //   Runner.PlayerJoined += OnPlayerJoined;
            Debug.Log("GameManager aktif, oyuncular bekleniyor...");
        }
    }

    private void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Oyuncu katıldı: {player.PlayerId}");

        // Oyuncuyu rastgele bir konuma spawn et
        Vector3 spawnPos = new Vector3(Random.Range(-2f, 2f), 1, Random.Range(-2f, 2f));
        runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);

        // Oyuncu katıldığında bir kez top spawn etmek istersen:
        if (ballPrefab != null && runner.ActivePlayers.Count() == 1) // ilk oyuncu gelince top spawn et
        {
            Vector3 ballPos = Vector3.zero + Vector3.up * 1f;
            runner.Spawn(ballPrefab, ballPos, Quaternion.identity);
            Debug.Log("Top sahneye spawn edildi!");
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Object.HasStateAuthority)
        {
            //runner.PlayerJoined -= OnPlayerJoined;
        }
    }
}