using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GridManagerUI : MonoBehaviour
{
    public static GridManagerUI Instance;

    [System.Serializable]
    public class BlockData
    {
        public string blockName;
        public Block.BlockType type;
        public Block.BlockOrientation orientation;
        public Vector2Int[] positions;
        public Sprite blockSprite;
    }

    public RectTransform gridParent;
    public GameObject blockPrefab;
    public BlockData[] initialBlocks;

    [Header("Grid Settings")]
    public int rows = 6;
    public int columns = 6;
    public float cellSize = 150f;
    public Vector2 gridStartPosition = new Vector2(100f, -200f);

    [Header("UI")]
    public Button resetButton;
    public RectTransform arrowsParent;

    [Header("FEEDBACK")]
    public AudioClip sonidoCorrecto;
    public AudioClip sonidoCompletado;
    public ParticleSystem particulasCompletado;

    [Header("MENSAJES")]
    public GameObject panelMensaje;
    public TextMeshProUGUI textoMensaje;
    public string mensajeCompletado = "¡Puzzle completado!";
    public float tiempoMostrarMensaje = 3f;
    public float delayAntesDeMensaje = 0.5f;

    [Header("OBJETOS AL COMPLETAR")]
    public GameObject[] objectsToActivateAfter;
    public GameObject[] objectsToDestroyAfter;
    public bool destroyAfterCompletion = false;

    [Header("CONFIGURACIÓN JUGADOR")]
    public MonoBehaviour scriptMovimientoJugador;
    public GameObject jugador;
    private Rigidbody2D rbJugador;
    private Vector2 velocidadAntesDeBloquear;

    private Dictionary<Vector2Int, Block> grid = new Dictionary<Vector2Int, Block>();
    private List<Block> allBlocks = new List<Block>();
    private Vector2Int[] targetCells = { new Vector2Int(3, 3), new Vector2Int(3, 4), new Vector2Int(3, 5) };

    private bool puzzleCompletado = false;
    private AudioSource audioSource;

    // Evento para notificar que el puzzle se completó
    public event Action OnPuzzleCompletado;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Inicializar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Buscar jugador si no está asignado
        if (jugador == null)
            jugador = GameObject.FindGameObjectWithTag("Player");

        if (jugador != null)
            rbJugador = jugador.GetComponent<Rigidbody2D>();

        // Debug de configuración
        foreach (BlockData data in initialBlocks)
        {
            Debug.Log($"Bloque: {data.blockName}, Tipo: {data.type}, Orientation: {data.orientation}, Positions: {data.positions.Length}");
            if (data.type == Block.BlockType.Conejo)
            {
                Debug.Log($"CONEJO encontrado con {data.positions.Length} posiciones");
            }
        }

        // Asegurar que ArrowsParent ocupa todo el Canvas
        RectTransform arrowsRect = arrowsParent.GetComponent<RectTransform>();
        arrowsRect.anchorMin = Vector2.zero;
        arrowsRect.anchorMax = Vector2.one;
        arrowsRect.offsetMin = Vector2.zero;
        arrowsRect.offsetMax = Vector2.zero;

        // Ocultar panel de mensaje al inicio
        if (panelMensaje != null)
            panelMensaje.SetActive(false);

        CreateInitialBoard();
        resetButton.onClick.AddListener(ResetBoard);
    }

    void CreateInitialBoard()
    {
        // Limpiar grid anterior
        grid.Clear();
        foreach (Block block in allBlocks)
        {
            if (block != null)
                Destroy(block.gameObject);
        }
        allBlocks.Clear();

        // Crear cada bloque
        foreach (BlockData data in initialBlocks)
        {
            CreateBlock(data);
        }

        // DEBUG: Buscar el conejo y verificar su tamaño
        foreach (Block block in allBlocks)
        {
            if (block.type == Block.BlockType.Conejo)
            {
                Debug.Log($"CONEJO encontrado en tiempo real: {block.blockName}");
                Debug.Log($"  - width: {block.width}, height: {block.height}");
                Debug.Log($"  - gridPosition: {block.gridPosition}");
                Debug.Log($"  - Cells ocupadas: {string.Join(", ", block.GetOccupiedCells())}");
            }
        }

        CheckVictory();
    }

    void CreateBlock(BlockData data)
    {
        GameObject blockObj = Instantiate(blockPrefab, gridParent);
        Block block = blockObj.GetComponent<Block>();

        block.blockName = data.blockName;
        block.type = data.type;
        block.orientation = data.orientation;

        // Configurar la imagen
        Image img = blockObj.GetComponent<Image>();
        if (img != null && data.blockSprite != null)
        {
            img.sprite = data.blockSprite;
        }

        // CALCULAR TAMAÑO CORRECTAMENTE
        if (data.type == Block.BlockType.Conejo)
        {
            // El conejo SIEMPRE es de tamaño 2 (1x2) y vertical
            block.width = 1;
            block.height = 2;
            Debug.Log($"Conejo detectado - tamaño: {block.width}x{block.height}");
        }
        else if (data.type == Block.BlockType.M)
        {
            // Caja M: tamaño 2
            if (data.orientation == Block.BlockOrientation.Horizontal)
            {
                block.width = 2;
                block.height = 1;
            }
            else
            {
                block.width = 1;
                block.height = 2;
            }
        }
        else if (data.type == Block.BlockType.L)
        {
            // Caja L: tamaño 3
            if (data.orientation == Block.BlockOrientation.Horizontal)
            {
                block.width = 3;
                block.height = 1;
            }
            else
            {
                block.width = 1;
                block.height = 3;
            }
        }

        // Configurar el RectTransform
        RectTransform rect = blockObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(block.width * cellSize, block.height * cellSize);
        rect.pivot = new Vector2(0, 1);

        // Posicionar inicialmente
        Vector2Int firstPos = data.positions[0];
        block.gridPosition = firstPos;

        // Calcular posición en el Canvas
        Vector2 anchoredPos = GridToCanvasPosition(firstPos, block.width, block.height);
        rect.anchoredPosition = anchoredPos;

        // Registrar en grid
        allBlocks.Add(block);
        foreach (Vector2Int pos in data.positions)
        {
            grid[pos] = block;
        }

        Debug.Log($"Bloque {data.blockName} creado - Tipo: {data.type}, Posiciones: {data.positions.Length}, Tamaño: {block.width}x{block.height}");
    }

    public Vector2 GridToCanvasPosition(Vector2Int gridPos, int width, int height)
    {
        float x = gridStartPosition.x + (gridPos.x * cellSize);
        float y = gridStartPosition.y - (gridPos.y * cellSize);
        return new Vector2(x, y);
    }

    public bool CanMoveBlock(Block block, Vector2Int direction)
    {
        List<Vector2Int> currentPositions = block.GetOccupiedCells();
        List<Vector2Int> newPositions = new List<Vector2Int>();

        Debug.Log($"Verificando movimiento {direction} para bloque en {block.gridPosition}");

        // Calcular nuevas posiciones
        foreach (Vector2Int pos in currentPositions)
        {
            Vector2Int newPos = pos + direction;
            newPositions.Add(newPos);
            Debug.Log($"Posición actual: {pos}, nueva posición: {newPos}");

            // Verificar límites del tablero
            if (newPos.x < 0 || newPos.x >= columns || newPos.y < 0 || newPos.y >= rows)
            {
                Debug.Log($"Fuera de límites: {newPos}");
                return false;
            }
        }

        // Verificar que las nuevas posiciones no estén ocupadas
        foreach (Vector2Int newPos in newPositions)
        {
            if (grid.ContainsKey(newPos) && grid[newPos] != block)
            {
                Debug.Log($"Posición ocupada: {newPos} por {grid[newPos].blockName}");
                return false;
            }
        }

        Debug.Log("Movimiento VÁLIDO");
        return true;
    }

    public void MoveBlock(Block block, Vector2Int direction)
    {
        if (!CanMoveBlock(block, direction))
            return;

        // Liberar posiciones antiguas
        List<Vector2Int> oldPositions = block.GetOccupiedCells();
        foreach (Vector2Int pos in oldPositions)
        {
            grid.Remove(pos);
        }

        // Calcular nueva posición base
        block.gridPosition += direction;

        // Ocupar nuevas posiciones
        List<Vector2Int> newPositions = block.GetOccupiedCells();
        foreach (Vector2Int pos in newPositions)
        {
            grid[pos] = block;
        }

        // Mover visualmente
        RectTransform rect = block.GetComponent<RectTransform>();
        Vector2 newPos = GridToCanvasPosition(block.gridPosition, block.width, block.height);
        rect.anchoredPosition = newPos;

        CheckVictory();
    }

    void CheckVictory()
    {
        if (puzzleCompletado) return;

        bool victory = true;
        foreach (Vector2Int cell in targetCells)
        {
            if (grid.ContainsKey(cell))
            {
                victory = false;
                break;
            }
        }

        if (victory)
        {
            CompletarPuzzle();
        }
    }

    // NUEVO: Método para completar el puzzle (adaptado de Puzzle4Botones2D)
    void CompletarPuzzle()
    {
        if (puzzleCompletado) return;

        puzzleCompletado = true;
        Debug.Log("¡PUZZLE DE BLOQUES COMPLETADO!");

        // Sonido
        if (sonidoCorrecto != null)
            audioSource.PlayOneShot(sonidoCorrecto);

        if (sonidoCompletado != null)
            audioSource.PlayOneShot(sonidoCompletado);

        // Partículas
        if (particulasCompletado != null)
            particulasCompletado.Play();

        // Mostrar mensaje con delay
        StartCoroutine(MostrarMensajeConDelay(mensajeCompletado));

        // Desactivar botón de reset
        if (resetButton != null)
            resetButton.interactable = false;

        // Bloquear movimiento del jugador
        BloquearMovimientoJugador(true);

        // Disparar evento
        OnPuzzleCompletado?.Invoke();
    }

    // NUEVO: Mostrar mensaje (adaptado de Puzzle4Botones2D)
    private IEnumerator MostrarMensajeConDelay(string mensaje)
    {
        // Esperar delay antes de mostrar mensaje
        yield return new WaitForSeconds(delayAntesDeMensaje);

        if (panelMensaje != null && textoMensaje != null)
        {
            textoMensaje.text = mensaje;
            panelMensaje.SetActive(true);
            Debug.Log($"✅ Mensaje MOSTRADO: {mensaje}");

            // Ocultar después del tiempo configurado
            StartCoroutine(OcultarMensajeDespuesDeTiempo());

            // Gestionar objetos después del mensaje
            StartCoroutine(GestionarObjetosDespuesDeMensaje());
        }
        else
        {
            Debug.LogError("❌ PanelMensaje o TextoMensaje no asignado en el inspector!");
        }
    }

    // NUEVO: Ocultar mensaje después de tiempo
    private IEnumerator OcultarMensajeDespuesDeTiempo()
    {
        yield return new WaitForSeconds(tiempoMostrarMensaje);

        if (panelMensaje != null)
        {
            panelMensaje.SetActive(false);
            Debug.Log("Mensaje ocultado");
        }
    }

    // NUEVO: Gestionar objetos después de completar (adaptado de Puzzle4Botones2D)
    private IEnumerator GestionarObjetosDespuesDeMensaje()
    {
        // Esperar el tiempo del mensaje + el delay inicial
        yield return new WaitForSeconds(delayAntesDeMensaje + tiempoMostrarMensaje);

        // Activar objetos
        foreach (GameObject obj in objectsToActivateAfter)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                Debug.Log($"Objeto activado: {obj.name}");
            }
        }

        // Destruir objetos
        foreach (GameObject obj in objectsToDestroyAfter)
        {
            if (obj != null)
            {
                Destroy(obj);
                Debug.Log($"Objeto destruido: {obj.name}");
            }
        }

        // Desbloquear movimiento del jugador después de gestionar objetos
        BloquearMovimientoJugador(false);

        // Destruir este objeto si está configurado
        if (destroyAfterCompletion)
        {
            Debug.Log($"Destruyendo puzzle: {gameObject.name}");
            Destroy(gameObject);
        }
    }

    // NUEVO: Bloquear/desbloquear movimiento del jugador
    private void BloquearMovimientoJugador(bool bloquear)
    {
        if (bloquear)
        {
            if (rbJugador != null)
            {
                velocidadAntesDeBloquear = rbJugador.linearVelocity;
                rbJugador.linearVelocity = Vector2.zero;
                rbJugador.angularVelocity = 0f;
            }
        }
        else
        {
            if (rbJugador != null)
            {
                rbJugador.linearVelocity = velocidadAntesDeBloquear;
            }
        }

        if (scriptMovimientoJugador != null)
        {
            scriptMovimientoJugador.enabled = !bloquear;
        }

        Debug.Log($"Movimiento del jugador {(bloquear ? "BLOQUEADO" : "DESBLOQUEADO")}");
    }

    void ResetBoard()
    {
        if (puzzleCompletado) return;
        CreateInitialBoard();
    }

    // NUEVO: Método para reiniciar completamente el puzzle (útil para debugging)
    [ContextMenu("Reiniciar Puzzle Completamente")]
    public void ReiniciarPuzzleCompletamente()
    {
        puzzleCompletado = false;

        // Reactivar botón de reset
        if (resetButton != null)
            resetButton.interactable = true;

        // Ocultar mensaje si está visible
        if (panelMensaje != null)
            panelMensaje.SetActive(false);

        // Desbloquear jugador
        BloquearMovimientoJugador(false);

        // Recrear tablero
        CreateInitialBoard();

        Debug.Log("Puzzle de bloques reiniciado completamente");
    }

    // NUEVO: Método para forzar completar el puzzle (útil para debugging)
    [ContextMenu("Forzar Completar Puzzle")]
    public void ForzarCompletarPuzzle()
    {
        CompletarPuzzle();
    }
}