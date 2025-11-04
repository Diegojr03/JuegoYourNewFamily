using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageEntryUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI timeText;
    public Image backgroundImage;

    [Header("Colors")]
    public Color playerColor = new Color(0.2f, 0.6f, 1f, 0.3f);
    public Color npcColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
    public Color playerTextColor = Color.cyan;
    public Color npcTextColor = Color.white;

    public void Setup(string speaker, string message, string time)
    {
        if (speakerNameText != null) speakerNameText.text = speaker;
        if (dialogueText != null) dialogueText.text = message;
        if (timeText != null) timeText.text = time;

        // Colores según si es jugador o NPC
        bool isPlayer = speaker == "Lilith"; // Ajusta según el nombre de tu protagonista

        if (backgroundImage != null)
        {
            backgroundImage.color = isPlayer ? playerColor : npcColor;
        }

        if (speakerNameText != null)
        {
            speakerNameText.color = isPlayer ? playerTextColor : npcTextColor;
        }
    }
}
