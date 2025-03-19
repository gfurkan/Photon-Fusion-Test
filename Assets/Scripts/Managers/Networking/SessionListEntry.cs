using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionListEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text sessionNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button joinButton;

    public void Setup(string sessionName, int currentPlayers, int maxPlayers, System.Action onJoinClicked)
    {
        sessionNameText.text = sessionName;
        playerCountText.text = $"{currentPlayers}/{maxPlayers}";

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => onJoinClicked.Invoke());
    }
}