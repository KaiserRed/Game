using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool oneShot = true;
    [SerializeField] private GameObject activatedVisual;

    [Header("Notification")]
    [Tooltip("TextMeshPro-текст на Canvas для надписи 'Сохранено'. Можно оставить пустым.")]
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private string notificationMessage = "Сохранено!";
    [SerializeField] private float notificationDuration = 2f;

    private bool used;

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (used && oneShot) return;
        if (!other.CompareTag(playerTag)) return;

        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        player.SetSpawnPoint(transform.position);
        used = true;
        if (activatedVisual != null) activatedVisual.SetActive(true);
        if (notificationText != null) StartCoroutine(ShowNotification());
    }

    private IEnumerator ShowNotification()
    {
        notificationText.text = notificationMessage;
        notificationText.gameObject.SetActive(true);
        yield return new WaitForSeconds(notificationDuration);
        notificationText.gameObject.SetActive(false);
    }
}