using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionListEntry : MonoBehaviour
{
    #region Fields

    [SerializeField] private TMP_Text _sessionNameText;
    [SerializeField] private TMP_Text _playerCountText;
    [SerializeField] private Button _joinButton;

    #endregion

    #region Public Methods

    public void Setup(string sessionName, int currentPlayers, int maxPlayers, System.Action onJoinClicked)
    {
        _sessionNameText.text = sessionName;
        _playerCountText.text = $"{currentPlayers}/{maxPlayers}";

        _joinButton.onClick.RemoveAllListeners();
        _joinButton.onClick.AddListener(() => onJoinClicked.Invoke());
    }

    #endregion

}