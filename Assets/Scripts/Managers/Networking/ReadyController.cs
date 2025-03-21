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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangePlayerReadyState(_playerRef, _toggle.isOn);
        }
        else
        {
            Debug.LogError("GameManager Instance is Null");
        }
    }
    
    #endregion

}