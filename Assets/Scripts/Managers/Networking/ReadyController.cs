using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class ReadyController : MonoBehaviour
{
    #region Fields

    [SerializeField] private Toggle _toggle;
    private PlayerRef _playerRef;

    #endregion

    #region Public Methods
    
    public void Initialize(PlayerRef playerRef)
    {
        _playerRef = playerRef;
    }
    
    public void SetValue()
    {
        Debug.Log("Toggle Value Changed To > " + _toggle.isOn);
        if (LobbyConnectionManager.Instance != null)
        {
            LobbyConnectionManager.Instance.ChangePlayerReadyState(_playerRef, _toggle.isOn);
        }
        else
        {
            Debug.LogError("Lobby Connection Manager Instance is Null");
        }
    }
    
    #endregion

}