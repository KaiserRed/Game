using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PogoEnemy : MonoBehaviour, IPogoable
{
    [Header("Patrol")]
    [SerializeField] private Vector3 pointAOffset = new Vector3(-2f, 0f, 0f);
    [SerializeField] private Vector3 pointBOffset = new Vector3(2f, 0f, 0f);
    [SerializeField] private float speed = 2f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 5f;

    [Header("Death VFX")]
    [SerializeField] private GameObject deathEffectPrefab;

    [Header("Damage")]
    [SerializeField] private string playerTag = "Player";

    private Vector3 spawnPosition;
    private Vector3 pointA;
    private Vector3 pointB;
    private Vector3 target;

    private void Start()
    {
        spawnPosition = transform.position;
        pointA = spawnPosition + pointAOffset;
        pointB = spawnPosition + pointBOffset;
        target = pointB;
    }

    private void Update()
    {
        Vector3 next = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        transform.position = next;

        if (Vector3.Distance(transform.position, target) < 0.05f)
            target = (target == pointA) ? pointB : pointA;

        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 10f * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other) => TryDamagePlayer(other);
    private void OnCollisionEnter(Collision collision) => TryDamagePlayer(collision.collider);

    private void TryDamagePlayer(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var pogo = other.GetComponentInParent<Pogo>();
        if (pogo != null && pogo.IsAttacking) return;

        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        player.Teleport(player.SpawnPoint);
    }

    public void OnPogoHit(PlayerController player)
    {
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        StartCoroutine(DieAndRespawn());
    }

    private IEnumerator DieAndRespawn()
    {
        SetVisible(false);
        enabled = false;

        if (respawnDelay > 0f)
        {
            yield return new WaitForSeconds(respawnDelay);
            transform.position = spawnPosition;
            target = pointB;
            SetVisible(true);
            enabled = true;
        }
    }

    private void SetVisible(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = visible;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = visible;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 a = Application.isPlaying ? pointA : transform.position + pointAOffset;
        Vector3 b = Application.isPlaying ? pointB : transform.position + pointBOffset;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawWireSphere(a, 0.2f);
        Gizmos.DrawWireSphere(b, 0.2f);
    }
}
