using UnityEngine;

public class Pogo : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;

    [Header("Hitbox")]
    [SerializeField] private Vector3 hitboxOffset = new Vector3(0f, -0.85f, 0f);
    [SerializeField] private Vector3 hitboxSize = new Vector3(1.0f, 0.6f, 1.0f);
    [SerializeField] private float hitboxDuration = 0.15f;
    [SerializeField] private float hitboxCooldown = 0.10f;

    [Header("Bounce")]
    [SerializeField] private float bounceVelocity = 14f;

    [Header("Collision")]
    [SerializeField] private LayerMask pogoMask = ~0;

    [Header("Visual (необязательно)")]
    [SerializeField] private GameObject hitboxVisual;

    private float activeTimer;
    private float cooldownTimer;

    public bool IsAttacking => activeTimer > 0f;

    private void Awake()
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (hitboxVisual != null) hitboxVisual.SetActive(false);
    }

    public void PerformPogoAttack()
    {
        if (cooldownTimer > 0f) return;
        if (activeTimer > 0f) return;
        activeTimer = hitboxDuration;
        cooldownTimer = hitboxDuration + hitboxCooldown;
        if (hitboxVisual != null) hitboxVisual.SetActive(true);
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (activeTimer <= 0f) return;

        activeTimer -= Time.deltaTime;
        if (CheckHit() || activeTimer <= 0f)
        {
            activeTimer = 0f;
            if (hitboxVisual != null) hitboxVisual.SetActive(false);
        }
    }

    private bool CheckHit()
    {
        Vector3 center = transform.position + hitboxOffset;
        Collider[] hits = Physics.OverlapBox(
            center,
            hitboxSize * 0.5f,
            transform.rotation,
            pogoMask,
            QueryTriggerInteraction.Collide);

        foreach (var c in hits)
        {
            if (c.transform.IsChildOf(transform)) continue;

            var pog = c.GetComponentInParent<IPogoable>();
            if (pog != null)
            {
                pog.OnPogoHit(player);
                if (player != null) player.OnPogoBounce(bounceVelocity * pog.BounceMultiplier);
                return true;
            }
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = activeTimer > 0f ? new Color(1f, 0.4f, 0f, 0.9f) : new Color(1f, 1f, 0f, 0.3f);
        Vector3 center = transform.position + hitboxOffset;
        Matrix4x4 m = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, hitboxSize);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = m;
    }
}
