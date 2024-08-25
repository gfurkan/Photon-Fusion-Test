using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Networking
{
public class SessionListEntry : MonoBehaviour
{
#region Fields

[SerializeField] private TextMeshProUGUI _sessionName, _playerCount;
[SerializeField] private Button _button;

#endregion

#region Properties

public TextMeshProUGUI SessionName => _sessionName;
public TextMeshProUGUI PlayerCount => _playerCount;
public Button Button => _button;

public int MaxPlayerCount { get; set; }

#endregion

#region Unity Methods

void Start()
{
    
}
void Update()
{
   
}

#endregion

#region Private Methods

#endregion

#region PublicMethods

public void JoinSession()
{
    LobbyManager.Instance.JoinSession(_sessionName.text);

}
#endregion
}
}

