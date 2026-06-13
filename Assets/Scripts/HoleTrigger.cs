using UnityEngine;
using PurrNet;
using System.Collections;
using System.Collections.Generic;
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
    private int playerCount = 0; // tylko do pozycji i koloru

    private class PlayerTimerData
    {
        public float elapsedTime;
        public bool running;
        public bool finished;
        public TextMeshPro text;
        public int displayIndex; // który z kolei gracz (do pozycji/koloru)
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && holeSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();
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
            Debug.LogWarning($"Timer dla gracza {ownerId} już istnieje!");
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
        Debug.Log($"⏱️ Timer startuje dla gracza {ownerId}");
    }

    private TextMeshPro CreateTimerDisplay(int index)
    {
        GameObject timerObj = new GameObject($"HoleTimer_P{index + 1}");
        timerObj.transform.SetParent(transform);
        timerObj.transform.localPosition = timerOffset + new Vector3(index * timerSpacing, 0, 0);
        timerObj.transform.localRotation = Quaternion.identity; // pionowo jak znak drogowy

        TextMeshPro tmp = timerObj.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 3f;
        tmp.color = GetPlayerColor(index);
        tmp.text = $"P{index + 1}: 00:00.00";
        return tmp;
    }

    private void StopTimer(ulong ownerId)
    {
        if (!playerTimers.TryGetValue(ownerId, out var data)) return;

        data.running = false;
        data.finished = true;
        UpdateTimerDisplay(data);

        if (data.text != null)
            data.text.color = Color.green;

        Debug.Log($"🏁 Gracz {ownerId} skończył! Czas: {data.text?.text}");
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
            new Color(1f, 0.5f, 0f) // pomarańczowy
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

        // Sprawdź czy ten gracz już skończył
        if (playerTimers.TryGetValue(ownerId, out var data) && data.finished)
            return;

        Debug.Log($"🏆 Piłka gracza {ownerId} wpadła do dołka!");

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