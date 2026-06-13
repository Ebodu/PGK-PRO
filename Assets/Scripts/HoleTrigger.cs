using UnityEngine;
using PurrNet;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class HoleTrigger : NetworkBehaviour
{
    [SerializeField] private string ballTag = "Ball";
    [SerializeField] private float sinkDelay = 0.5f;
    [SerializeField] private bool destroyBall = false;
    [SerializeField] private ParticleSystem holeEffect;
    [SerializeField] private AudioClip holeSound;

    [Header("Timer UI")]
    [SerializeField] private Vector3 timerOffset = new Vector3(0, 1.5f, 0);
    [SerializeField] private float timerSpacing = 0.6f;

    private AudioSource audioSource;
    private bool isUsed = false;
    private float blockUntilTime = 0f;

    private Dictionary<ulong, PlayerTimerData> playerTimers = new Dictionary<ulong, PlayerTimerData>();
    private int playerCount = 0;
    private TextMeshPro winnerText;

    // Serwer
    private Dictionary<ulong, float> finishedTimes = new Dictionary<ulong, float>();
    private float serverStartTime = -1f;

    private class PlayerTimerData
    {
        public float elapsedTime;
        public bool running;
        public bool finished;
        public TextMeshPro text;
        public int displayIndex;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && holeSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();

        CreateWinnerDisplay();
    }

    private void CreateWinnerDisplay()
    {
        GameObject winnerObj = new GameObject("WinnerText");
        winnerObj.transform.SetParent(transform);
        winnerObj.transform.localPosition = timerOffset + new Vector3(0, 0.8f, 0);
        winnerObj.transform.localRotation = Quaternion.identity;

        winnerText = winnerObj.AddComponent<TextMeshPro>();
        winnerText.alignment = TextAlignmentOptions.Center;
        winnerText.fontSize = 4f;
        winnerText.color = Color.yellow;
        winnerText.text = "";
        winnerObj.SetActive(false);
    }

    private void Update()
    {
        foreach (var data in playerTimers.Values)
        {
            if (!data.running) continue;
            data.elapsedTime += Time.deltaTime;
            UpdateTimerDisplay(data);
        }
    }

    public void StartTimer(ulong ownerId)
    {
        if (playerTimers.ContainsKey(ownerId))
        {
            Debug.LogWarning($"Timer dla gracza {ownerId} juz istnieje!");
            return;
        }

        int index = playerCount++;

        var data = new PlayerTimerData
        {
            elapsedTime = 0f,
            running = true,
            finished = false,
            displayIndex = index,
            text = CreateTimerDisplay(index)
        };

        playerTimers[ownerId] = data;

        NotifyServerPlayerJoined(ownerId);

        Debug.Log($"Timer startuje dla gracza {ownerId}");
    }

    [ServerRpc(requireOwnership: false)]
    private void NotifyServerPlayerJoined(ulong ownerId)
    {
        if (!finishedTimes.ContainsKey(ownerId))
            finishedTimes[ownerId] = -1f;

        // Serwer startuje swoj timer przy pierwszym graczu
        if (serverStartTime < 0f)
            serverStartTime = Time.time;

        Debug.Log($"[Serwer] Gracz {ownerId} dolaczyl. Graczy: {finishedTimes.Count}");
    }

    [ServerRpc(requireOwnership: false)]
    private void NotifyServerPlayerFinished(ulong ownerId)
    {
        // Serwer sam oblicza czas — nie ufa klientom
        float time = Time.time - serverStartTime;
        finishedTimes[ownerId] = time;

        int finished = finishedTimes.Values.Count(t => t >= 0);
        int total = finishedTimes.Count;
        Debug.Log($"[Serwer] Gracz {ownerId} skonczyl z czasem {time:F2}s. {finished}/{total}");

        bool wszyscySkonczyli = finishedTimes.Values.All(t => t >= 0);
        if (wszyscySkonczyli)
        {
            var winner = finishedTimes.OrderBy(kvp => kvp.Value).First();
            BroadcastWinner(winner.Key, winner.Value);
        }
    }

    [ObserversRpc]
    private void BroadcastWinner(ulong winnerId, float winnerTime)
    {
        Debug.Log($"[Klient] Zwyciezca: {winnerId} z czasem {winnerTime:F2}s");

        // Znajdz displayIndex zwyciezcy
        int winnerIndex = -1;
        foreach (var kvp in playerTimers)
        {
            if (kvp.Key == winnerId)
            {
                winnerIndex = kvp.Value.displayIndex;
                // Podswietl timer zwyciezcy na zloto
                if (kvp.Value.text != null)
                    kvp.Value.text.color = Color.yellow;
                break;
            }
        }

        // Jesli nie znaleziono lokalnie (drugi klient nie zna tego gracza)
        // uzyjemy samego czasu i numeru
        int winnerNumber = winnerIndex >= 0 ? winnerIndex + 1 : (int)(winnerId + 1);
        Color winnerColor = winnerIndex >= 0 ? GetPlayerColor(winnerIndex) : Color.yellow;

        int minutes = Mathf.FloorToInt(winnerTime / 60f);
        int seconds = Mathf.FloorToInt(winnerTime % 60f);
        int millis  = Mathf.FloorToInt((winnerTime % 1f) * 100f);
        string timeStr = $"{minutes:00}:{seconds:00}.{millis:00}";

        winnerText.gameObject.SetActive(true);
        winnerText.text = $"Wygrywa P{winnerNumber}!\n{timeStr}";
        winnerText.color = winnerColor;
    }

    private void StopTimer(ulong ownerId)
    {
        if (!playerTimers.TryGetValue(ownerId, out var data)) return;

        data.running = false;
        data.finished = true;
        UpdateTimerDisplay(data);

        if (data.text != null)
            data.text.color = Color.green;

        // Tylko informujemy serwer — on sam liczy czas
        NotifyServerPlayerFinished(ownerId);

        Debug.Log($"Gracz {ownerId} skonczyl!");
    }

    private TextMeshPro CreateTimerDisplay(int index)
    {
        GameObject timerObj = new GameObject($"HoleTimer_P{index + 1}");
        timerObj.transform.SetParent(transform);
        timerObj.transform.localPosition = timerOffset + new Vector3(index * timerSpacing, 0, 0);
        timerObj.transform.localRotation = Quaternion.identity;

        TextMeshPro tmp = timerObj.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 3f;
        tmp.color = GetPlayerColor(index);
        tmp.text = $"P{index + 1}: 00:00.00";
        return tmp;
    }

    private void UpdateTimerDisplay(PlayerTimerData data)
    {
        if (data.text == null) return;
        int minutes = Mathf.FloorToInt(data.elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(data.elapsedTime % 60f);
        int millis  = Mathf.FloorToInt((data.elapsedTime % 1f) * 100f);
        data.text.text = $"P{data.displayIndex + 1}: {minutes:00}:{seconds:00}.{millis:00}";
    }

    private Color GetPlayerColor(int index)
    {
        Color[] colors = {
            Color.white,
            Color.yellow,
            Color.cyan,
            new Color(1f, 0.5f, 0f)
        };
        return colors[index % colors.Length];
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < blockUntilTime) return;
        if (isUsed) return;
        if (!other.CompareTag(ballTag)) return;

        GolfBallController ball = other.GetComponent<GolfBallController>();
        if (ball == null) return;

        ulong ownerId = ball.ownerId;

        if (playerTimers.TryGetValue(ownerId, out var data) && data.finished)
            return;

        Debug.Log($"Pilka gracza {ownerId} wpadla do dolka!");

        isUsed = true;
        blockUntilTime = Time.time + 0.5f;

        StopTimer(ownerId);
        ball.DisableMovement();

        if (holeSound != null && audioSource != null)
            audioSource.PlayOneShot(holeSound);
        if (holeEffect != null)
            Instantiate(holeEffect, transform.position, Quaternion.identity);

        if (destroyBall)
            Destroy(ball.gameObject, sinkDelay);

        StartCoroutine(ResetHoleAfterDelay(0.5f));
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(ballTag)) return;
        StartCoroutine(ResetHoleAfterDelay(0.3f));
    }

    private IEnumerator ResetHoleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isUsed = false;
    }
}