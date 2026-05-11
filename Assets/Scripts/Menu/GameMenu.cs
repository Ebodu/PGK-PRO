using UnityEngine;
using UnityEngine.SceneManagement;
using PurrNet;

public class GameMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName;
    
    private NetworkManager networkManager;

    void Start()
    {
        // Znajdź TYLKO PurrNet NetworkManager
        networkManager = FindObjectOfType<NetworkManager>();
        
        if (networkManager == null)
        {
            Debug.LogError("Nie znaleziono PurrNet NetworkManager!");
            return;
        }

        // Uruchom jako host (serwer + klient)
        if (!networkManager.isServer && !networkManager.isClient)
        {
            networkManager.StartServer();
            networkManager.StartClient();
            Debug.Log("PurrNet Host uruchomiony");
        }
    }

    public async void Play()
    {
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager nie istnieje!");
            return;
        }

        if (!networkManager.isServer)
        {
            Debug.LogError("Nie jesteś serwerem!");
            return;
        }

        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("Nazwa sceny nie jest ustawiona!");
            return;
        }

        Debug.Log($"Ładuję scenę: {gameSceneName}");
        await networkManager.sceneModule.LoadSceneAsync(gameSceneName, LoadSceneMode.Single);
        Debug.Log("Scena załadowana!");
    }

    public void Quit()
    {
        if (networkManager != null)
        {
            if (networkManager.isClient)
                networkManager.StopClient();
            if (networkManager.isServer)
                networkManager.StopServer();
        }
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}