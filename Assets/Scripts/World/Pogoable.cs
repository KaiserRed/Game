using UnityEngine;


public class Pogoable : MonoBehaviour, IPogoable
{
    [SerializeField, Range(0.5f, 3f)] private float bounceMultiplier = 1.5f;

    [SerializeField] private AudioSource bounceSfx;

    public float BounceMultiplier => bounceMultiplier;

    public void OnPogoHit(PlayerController player)
    {
        if (bounceSfx != null) bounceSfx.Play();
    }
}
