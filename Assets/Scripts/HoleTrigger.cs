using UnityEngine;
using PurrNet;
using System.Collections;

public class HoleTrigger : NetworkBehaviour
{
    [SerializeField] private string ballTag = "Ball";
    [SerializeField] private float sinkDelay = 0.5f;
    [SerializeField] private ParticleSystem holeEffect;
    [SerializeField] private AudioClip holeSound;

    // SyncVar zamiast NetworkVariable – PurrNet tak to robi
    private readonly SyncVar<bool> isOccupied = new(false);
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && holeSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[KLIENT] OnTriggerEnter wykrył: {other.name}, IsServer: {isServer}");
        if (!other.CompareTag(ballTag)) return;
        
        // Wywołanie metody po stronie serwera
        ServerHandleBallEnter(other.gameObject);
    }

    [ServerRpc]
    private void ServerHandleBallEnter(GameObject ballObject)
    {
        if (isOccupied.value) return;  // .value zamiast .Value
        
        var ball = ballObject.GetComponent<GolfBallController>();
        if (ball == null) return;

        Debug.Log("[SERVER] Piłka wpadła do dołka!");
        
        isOccupied.value = true;
        
        // Efekty widoczne dla wszystkich graczy
        RpcPlayHoleEffects(transform.position);
        
        // Zatrzymanie piłki przez serwer
        ball.ServerOnEnterHole();
        
        StartCoroutine(ServerResetHoleAfterDelay(ballObject, sinkDelay));
    }

    [ObserversRpc]
    private void RpcPlayHoleEffects(Vector3 position)
    {
        if (holeSound != null && audioSource != null)
            audioSource.PlayOneShot(holeSound);
        if (holeEffect != null)
            Instantiate(holeEffect, position, Quaternion.identity);
    }

    [ServerRpc]
    private IEnumerator ServerResetHoleAfterDelay(GameObject ballObject, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (ballObject != null)
        {
            // Prawidłowe usunięcie piłki z sieci
            var netId = ballObject.GetComponent<NetworkBehaviour>();
            if (netId != null) netId.Despawn();
        }
        
        yield return new WaitForSeconds(0.5f);
        isOccupied.value = false;
        Debug.Log("[SERVER] Dołek ponownie dostępny.");
    }
}