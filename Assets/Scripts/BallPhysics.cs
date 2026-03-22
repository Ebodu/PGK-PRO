using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    [Header("Odbicie")]
    public float bounciness = 0.8f;          // wspó³czynnik odbicia (1 = pe³ne, 0 = brak)
    public float minVelocityToBounce = 0.5f; // minimalna prêdkoœæ, przy której gra dŸwiêk/efekty

    [Header("Efekty")]
    public AudioClip bounceSound;            // dŸwiêk odbicia
    public ParticleSystem bounceParticles;   // efekt cz¹steczek przy odbiciu

    private Rigidbody rb;
    private AudioSource audioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && bounceSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Opcjonalnie: ustaw bounciness w PhysicMaterial (jeœli chcesz sterowaæ z kodu)
        Collider col = GetComponent<Collider>();
        if (col != null && col.material != null)
        {
            col.material.bounciness = bounciness;
        }
        else
        {
            Debug.LogWarning("Brak PhysicMaterial na colliderze! Dodaj go dla poprawnego odbicia.");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // SprawdŸ, czy prêdkoœæ uderzenia jest wystarczaj¹ca, by uruchomiæ efekty
        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minVelocityToBounce) return;

        // Odtwórz dŸwiêk
        if (bounceSound != null && audioSource != null)
            audioSource.PlayOneShot(bounceSound, impactSpeed * 0.1f); // g³oœnoœæ zale¿na od prêdkoœci

        // Odpal cz¹steczki w miejscu kontaktu
        if (bounceParticles != null)
        {
            ContactPoint contact = collision.contacts[0];
            ParticleSystem particles = Instantiate(bounceParticles, contact.point, Quaternion.LookRotation(contact.normal));
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration);
        }

        // Opcjonalnie: mo¿esz dodatkowo dostosowaæ wektor prêdkoœci po odbiciu
        // (Unity robi to automatycznie, ale jeœli chcesz zmodyfikowaæ, odkomentuj poni¿szy kod)
        /*
        Vector3 incomingVelocity = rb.linearVelocity;
        Vector3 normal = collision.contacts[0].normal;
        Vector3 reflectedVelocity = Vector3.Reflect(incomingVelocity, normal);
        rb.linearVelocity = reflectedVelocity * bounciness;
        */
    }
}