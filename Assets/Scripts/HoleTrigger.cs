using UnityEngine;

public class HoleTrigger : MonoBehaviour
{
    [Header("Ustawienia dołka")]
    [SerializeField] private string ballTag = "Ball";
    [SerializeField] private float sinkDelay = 0.5f;
    [SerializeField] private bool disableBallOnHole = true;

    [Header("Opcjonalne efekty")]
    [SerializeField] private ParticleSystem holeEffect;
    [SerializeField] private AudioClip holeSound;

    private AudioSource audioSource;
    private bool isUsed = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && holeSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isUsed) return;
        if (!other.CompareTag(ballTag)) return;

        GolfBallController ballController = other.GetComponent<GolfBallController>();
        if (ballController == null) return;

        // ✅ Zapobiega wielokrotnemu wejściu (np. gdy piłka się trzęsie w dołku)
        if (isUsed) return;
        isUsed = true;

        OnBallEnteredHole(ballController);
    }

    private void OnBallEnteredHole(GolfBallController ball)
    {
        Debug.Log($"⛳ Piłka wpadła do dołka! {ball.name}");

        // 1. Efekt dźwiękowy
        if (holeSound != null && audioSource != null)
            audioSource.PlayOneShot(holeSound);

        // 2. Efekt cząsteczek
        if (holeEffect != null)
            Instantiate(holeEffect, transform.position, Quaternion.identity);

        // 3. Zatrzymanie piłki i uniemożliwienie dalszych uderzeń
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;  // ✅ To całkowicie wyłącza fizykę
        }

        // 4. Wyłączenie skryptu sterowania (żeby Update nie próbował nic robić)
        ball.enabled = false;

        // 5. Schowanie wizualnych wskaźników
        if (ball.arrow != null)
            ball.arrow.gameObject.SetActive(false);
        if (ball.rangeIndicator != null)
            ball.rangeIndicator.SetActive(false);

        // 6. Opcjonalne wyłączenie/zniszczenie piłki po czasie
        if (disableBallOnHole)
            Destroy(ball.gameObject, sinkDelay);

        // 7. 📍 TU DODAJ SWOJĄ LOGIKĘ PUNKTÓW / NAPĘDNEGO DOŁKA
        // ScoreManager.Instance?.AddScore(1);
        // Debug.Log("🏆 +1 punkt! Przejdź do następnego dołka.");
    }

    // Resetowanie dołka (przydatne przy powtórce poziomu)
    public void ResetHole()
    {
        isUsed = false;
    }

    // Wizualizacja w edytorze
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}