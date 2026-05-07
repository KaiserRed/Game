using UnityEngine;

public class Respawn : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private float killY = -20f;

    [SerializeField] private bool useCheckpoints = false;

    private void Awake()
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (player == null) player = FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (player == null) return;
        if (player.transform.position.y < killY)
        {
            player.Teleport(player.SpawnPoint);
        }
    }
}
