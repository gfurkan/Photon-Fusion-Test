using System.Threading;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Managers;

public class CustomSceneManager : SingletonManager<CustomSceneManager>,INetworkSceneManager
{
    public async Task SwitchScene(SceneRef sceneRef, NetworkRunner runner, CancellationToken cancellationToken)
    {
        string sceneName = sceneRef.ToString(); // SceneRef sahne adını döndürür
        var loadSceneTask = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        while (!loadSceneTask.isDone && !cancellationToken.IsCancellationRequested)
        {
            await Task.Yield();
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Debug.Log("Scene load canceled.");
        }
        else
        {
            Debug.Log($"Scene '{sceneName}' loaded with Single mode.");
        }
    }
    public void Initialize(NetworkRunner runner)
    {
        throw new System.NotImplementedException();
    }

    public void Shutdown()
    {
        throw new System.NotImplementedException();
    }

    public bool IsRunnerScene(Scene scene)
    {
        throw new System.NotImplementedException();
    }

    public bool TryGetPhysicsScene2D(out PhysicsScene2D scene2D)
    {
        throw new System.NotImplementedException();
    }

    public bool TryGetPhysicsScene3D(out PhysicsScene scene3D)
    {
        throw new System.NotImplementedException();
    }

    public void MakeDontDestroyOnLoad(GameObject obj)
    {
        throw new System.NotImplementedException();
    }

    public bool MoveGameObjectToScene(GameObject gameObject, SceneRef sceneRef)
    {
        throw new System.NotImplementedException();
    }

    public NetworkSceneAsyncOp LoadScene(SceneRef sceneRef, NetworkLoadSceneParameters parameters)
    {
        throw new System.NotImplementedException();
    }

    public NetworkSceneAsyncOp UnloadScene(SceneRef sceneRef)
    {
        throw new System.NotImplementedException();
    }

    public SceneRef GetSceneRef(GameObject gameObject)
    {
        throw new System.NotImplementedException();
    }

    public SceneRef GetSceneRef(string sceneNameOrPath)
    {
        throw new System.NotImplementedException();
    }

    public bool OnSceneInfoChanged(NetworkSceneInfo sceneInfo, NetworkSceneInfoChangeSource changeSource)
    {
        throw new System.NotImplementedException();
    }

    public bool IsBusy { get; }
    public Scene MainRunnerScene { get; }
}