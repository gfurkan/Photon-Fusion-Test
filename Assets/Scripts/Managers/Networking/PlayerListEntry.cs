using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListEntry : MonoBehaviour
{
    #region Fields

    [SerializeField] private Toggle _readyToggle;
    [SerializeField] private TextMeshProUGUI _playerName;
    [SerializeField] private ReadyController _readyController;
    
    #endregion

    #region Public Methods

    public void SetPlayerData(bool isReady, string name, PlayerRef playerRef)
    {
        _readyToggle.isOn = isReady;
        _playerName.text = name;
        _readyController.Initialize(playerRef);
        
        if (playerRef == GameManager.Instance.Runner.LocalPlayer)
        {
            _readyToggle.interactable = true;
        }
        else
        {
            _readyToggle.interactable = false;
        }

        Debug.Log("Player data setted.");
    }

    #endregion

}
