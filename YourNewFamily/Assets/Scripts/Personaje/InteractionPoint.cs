using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YNF.Progreso;

public class InteractionPoint : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    public float interactionDistance = 2f;
    public Vector3 promptOffset = new Vector3(0, 1f, 0);

    [Header("Sistemas de Diálogo")]
    public DialogueSystem dialogueSystem;
    public DialogueChoiceSystem dialogueChoiceSystem;

    [Header("Configuración")]
    public KeyCode interactionKey = KeyCode.F;
    public bool isDialogueTrigger = true;

    private Transform player;
    private bool canInteract = false;
    private MovimientoPersonaje playerMovement;
    private bool avisoDeCercania;

    /// <summary>
    /// Traza: deja constancia de que este punto existe y esta encendido.
    /// Si un dialogo no aparece en el log al cargar, es que su objeto no se
    /// encendio, y eso es un problema de guardado, no de colliders.
    /// </summary>
    void OnEnable()
    {
        LogProgreso.Info($"punto de interaccion listo: {gameObject.name} " +
                         $"(dialogo: {NombreDeDialogo()}) en {transform.position}", gameObject);
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player != null)
            playerMovement = player.GetComponent<MovimientoPersonaje>();

        // Si no se asignó manualmente, buscar en el mismo objeto
        if (dialogueSystem == null)
            dialogueSystem = GetComponent<DialogueSystem>();

        if (dialogueChoiceSystem == null)
            dialogueChoiceSystem = GetComponent<DialogueChoiceSystem>();
    }

    void Update()
    {
        if (canInteract && Input.GetKeyDown(interactionKey))
        {
            TriggerInteraction();
        }

        AvisarSiEstaCercaSinContacto();
        InformeAlPulsar();
    }

    // El informe lo escribe un solo punto por fotograma, no los ochenta y seis.
    private static int fotogramaDelUltimoInforme = -1;

    /// <summary>
    /// Diagnostico: al pulsar la tecla de interaccion deja en el log donde
    /// esta el jugador y cuales son los cinco puntos de interaccion mas
    /// cercanos, con la distancia real a su collider y si hay contacto. Es lo
    /// que convierte un "no me sale la F" en un dato: o el punto no esta ahi,
    /// o esta pero el jugador se queda a X de su borde.
    /// </summary>
    private void InformeAlPulsar()
    {
        if (player == null) return;
        if (!Input.GetKeyDown(interactionKey)) return;
        if (fotogramaDelUltimoInforme == Time.frameCount) return;
        fotogramaDelUltimoInforme = Time.frameCount;

        InteractionPoint[] puntos = FindObjectsOfType<InteractionPoint>(true);
        System.Array.Sort(puntos, (a, b) =>
            DistanciaAlBorde(a).CompareTo(DistanciaAlBorde(b)));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"tecla pulsada con el jugador en {player.position}. Puntos mas cercanos:");
        int n = 0;
        foreach (InteractionPoint p in puntos)
        {
            if (p == null || n >= 5) break;
            n++;
            sb.AppendLine($"   borde a {DistanciaAlBorde(p):0.00}  centro a " +
                          $"{Vector2.Distance(player.position, p.transform.position):0.00}  " +
                          $"{p.gameObject.name} (dialogo: {p.NombreDeDialogo()}) " +
                          $"activo={p.gameObject.activeInHierarchy} contacto={p.canInteract}");
        }
        LogProgreso.Info(sb.ToString());
    }

    private float DistanciaAlBorde(InteractionPoint p)
    {
        if (p == null) return float.MaxValue;
        Collider2D col = p.GetComponent<Collider2D>();
        if (col == null) return Vector2.Distance(player.position, p.transform.position);
        return Mathf.Sqrt(col.bounds.SqrDistance(player.position));
    }

    /// <summary>
    /// El campo interactionDistance esta declarado y configurado objeto por
    /// objeto, pero el script nunca lo usaba: la interaccion depende solo de
    /// chocar con el collider. De momento no se cambia ese comportamiento,
    /// solo se aprovecha la distancia para trazar el caso que interesa: el
    /// jugador esta al lado del punto y aun asi no hay contacto. Eso separa
    /// "no llegue a acercarme" de "me acerque y el collider no responde".
    /// </summary>
    private void AvisarSiEstaCercaSinContacto()
    {
        if (player == null) return;

        float d = DistanciaAlBorde(this);

        if (!canInteract && d <= 0.75f)
        {
            if (!avisoDeCercania)
            {
                avisoDeCercania = true;
                LogProgreso.Info($"a {d:0.00} del borde de {gameObject.name} " +
                                 $"(dialogo: {NombreDeDialogo()}) pero sin contacto con su collider",
                                 gameObject);
            }
        }
        else if (d > 1.5f)
        {
            avisoDeCercania = false;
        }
    }

    /// <summary>
    /// Traza de diagnostico: deja constancia de cuando este punto se vuelve
    /// interactuable y cuando deja de serlo. Sin esto no hay forma de saber,
    /// leyendo el log, si un dialogo no salta porque el jugador nunca llego a
    /// tocar el collider o porque fallo el sistema de dialogo.
    /// </summary>
    private void Marcar(bool puede)
    {
        if (canInteract == puede) return;
        canInteract = puede;
        LogProgreso.Info($"interaccion {(puede ? "disponible" : "fuera de alcance")}: " +
                         $"{gameObject.name} (dialogo: {NombreDeDialogo()})", gameObject);
    }

    private string NombreDeDialogo()
    {
        if (dialogueSystem != null && !string.IsNullOrEmpty(dialogueSystem.dialogueId))
            return dialogueSystem.dialogueId;
        if (dialogueChoiceSystem != null) return "eleccion";
        return "ninguno";
    }

    // --- IMPORTANTE: USAS OnCollision, pero deberías usar Trigger si es diálogo ---
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Marcar(true);
            InteractionPromptManager.Instance?.ShowPrompt(this);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Marcar(false);
            InteractionPromptManager.Instance?.HidePrompt();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Marcar(true);
            InteractionPromptManager.Instance?.ShowPrompt(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Marcar(false);
            InteractionPromptManager.Instance?.HidePrompt();
        }
    }

    void TriggerInteraction()
    {
        LogProgreso.Info($"tecla de interaccion en {gameObject.name} " +
                         $"(dialogo: {NombreDeDialogo()})", gameObject);

        if (isDialogueTrigger)
        {
            StartDialogue();
        }
        else
        {
            Debug.Log("Activando puzzle...");
        }
    }

    void StartDialogue()
    {
        InteractionPromptManager.Instance?.HidePrompt();

        // 1) PRIORIDAD: First DialogueChoiceSystem
        if (dialogueChoiceSystem != null)
        {
            dialogueChoiceSystem.StartDialogue();
            return;
        }

        // 2) Si no hay choice, usa el DialogueSystem normal
        if (dialogueSystem != null)
        {
            dialogueSystem.StartDialogue();
            return;
        }

        Debug.Log("No hay ningún sistema de diálogo asignado en " + gameObject.name);
    }

    // Métodos públicos para configuración
    public void SetInteractionKey(KeyCode newKey)
    {
        interactionKey = newKey;
    }

    public void SetInteractionType(bool isForDialogue)
    {
        isDialogueTrigger = isForDialogue;
    }
}
