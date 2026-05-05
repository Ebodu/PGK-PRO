using UnityEngine;
using UnityEngine.SceneManagement;
using PurrNet;

public class GameMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName;
    [SerializeField] private bool startAsHost = true;
    
    private NetworkManager networkManager;

    void Start()
    {
        networkManager = FindObjectOfType<NetworkManager>();
        
        if (networkManager == null)
        {
            Debug.LogError("Nie znaleziono NetworkManager! Dodaj go do sceny MainMenu.");
            return;
        }

        // Automatycznie uruchom jako host
        if (startAsHost)
        {
            networkManager.StartServer();
            networkManager.StartClient();
            Debug.Log("Uruchomiono jako Host (Serwer + Klient)");
        }
    }

    public async void Play()
    {
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager nie istnieje!");
            return;
        }

        // Jeśli nie jesteś serwerem, nie możesz ładować scen
        if (!networkManager.isServer)
        {
            Debug.LogError("Tylko serwer może ładować sceny! Uruchom jako host.");
            return;
        }

        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("Nazwa sceny nie jest ustawiona!");
            return;
        }

        Debug.Log($"Serwer ładuje scenę: {gameSceneName}");
        
        // Proste ładowanie sceny
        await networkManager.sceneModule.LoadSceneAsync(gameSceneName, LoadSceneMode.Single);
        
        Debug.Log("Scena załadowana!");
    }

    public void Quit()
    {
        // Zatrzymaj połączenia przed wyjściem
        if (networkManager != null)
        {
            if (networkManager.isClient)
                networkManager.StopClient();
            if (networkManager.isServer)
                networkManager.StopServer();
        }
        
        Application.Quit();
    }
}