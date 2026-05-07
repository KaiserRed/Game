using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    [Header("Icons")]
    [SerializeField] private Image dashIcon;
    [SerializeField] private Image doubleJumpIcon;
    [SerializeField] private Image glideIcon;

    [Header("Colors")]
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color usedColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (player == null) return;

        if (dashIcon != null)
            dashIcon.color = player.DashAvailable ? readyColor : usedColor;

        if (doubleJumpIcon != null)
            doubleJumpIcon.color = player.DoubleJumpAvailable ? readyColor : usedColor;

        if (glideIcon != null)
        {
            float remaining = player.GlideRemainingNormalized;
            glideIcon.color = remaining > 0.05f ? readyColor : usedColor;
            glideIcon.fillAmount = remaining;
        }
    }
}
