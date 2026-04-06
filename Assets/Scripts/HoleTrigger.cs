using UnityEngine;
using PurrNet;
using System.Collections;

public class HoleTrigger : NetworkBehaviour
{
    [SerializeField] private string ballTag = "Ball";
    [SerializeField] private float sinkDelay = 0.5f;
    [SerializeField] private bool destroyBall = false;
    [SerializeField] private ParticleSystem holeEffect;
    [SerializeField] private AudioClip holeSound;

    private AudioSource audioSource;
    private bool isUsed = false;
    private float blockUntilTime = 0f; // nowe

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && holeSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < blockUntilTime) return; // blokada czasowa
        if (isUsed) return;
        if (!other.CompareTag(ballTag)) return;

        Debug.Log("🏆 Piłka wpadła do dołka!");

        GolfBallController ball = other.GetComponent<GolfBallController>();
        if (ball == null) return;

        isUsed = true;
        blockUntilTime = Time.time + 0.5f; // blokuj na 0.5s

        ball.DisableMovement();

        if (holeSound != null && audioSource != null)
            audioSource.PlayOneShot(holeSound);
        if (holeEffect != null)
            Instantiate(holeEffect, transform.position, Quaternion.identity);

        if (destroyBall)
            Destroy(ball.gameObject, sinkDelay);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(ballTag)) return;
        // Nie resetuj od razu - pozwól, by czas blokady zapobiegł ponownemu wejściu
        // Opcjonalnie: resetuj po czasie
        StartCoroutine(ResetHoleAfterDelay(0.3f));
    }

    private IEnumerator ResetHoleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isUsed = false;
        Debug.Log("Dołek gotowy na następną piłkę.");
    }
}