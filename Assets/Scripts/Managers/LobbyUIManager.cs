using Managers;
using TMPro;
using UnityEngine;


namespace Networking
{
public class LobbyUIManager : SingletonManager<LobbyUIManager>
{
#region Fields

[SerializeField] private GameObject[] _menuList;
[SerializeField] private TextMeshProUGUI _menuTransitionText;
[SerializeField] private TMP_InputField _sessionCreateName;
[SerializeField] private TMP_InputField _sessionCreateMaxPlayerCount;

private GameObject _currentMenu;

#endregion

#region Properties

public string SessionName => _sessionCreateName.text;
public int MaxPlayerCount => (int.Parse(_sessionCreateMaxPlayerCount.text));

#endregion

#region Unity Methods

void Start()
{
    SetMenuTransitionText("Trying to Connect...");
}
void Update()
{
   
}

#endregion

#region Private Methods

#endregion

#region PublicMethods


public void OpenMenu(int newMenu)
{
    if (_currentMenu != null)
    {
        _currentMenu.SetActive(false);
    }
    _menuTransitionText.gameObject.SetActive(false);
    _currentMenu = _menuList[newMenu];
    _currentMenu.SetActive(true);
}

public void SetMenuTransitionText(string text)
{
    _menuTransitionText.gameObject.SetActive(true);
    _menuTransitionText.text = text;
}
#endregion
}

}

