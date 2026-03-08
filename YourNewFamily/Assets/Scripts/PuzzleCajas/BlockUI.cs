using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Estatua;

public class Block : MonoBehaviour, IPointerClickHandler
{
    public enum BlockType { M, L, Conejo }
    public enum BlockOrientation { Horizontal, Vertical }
    public enum Direccion
    {
        Arriba,
        Abajo,
        Izquierda,
        Derecha
    }

    // Variable estática para el bloque seleccionado
    private static Block currentlySelectedBlock = null;

    public string blockName;
    public BlockType type;
    public BlockOrientation orientation;

    [HideInInspector] public int width;
    [HideInInspector] public int height;
    [HideInInspector] public Vector2Int gridPosition;

    [Header("UI Arrows")]
    public GameObject arrowPrefabArriba;
    public GameObject arrowPrefabAbajo;
    public GameObject arrowPrefabIzquierda;
    public GameObject arrowPrefabDerecha;
    public float arrowOffset = 100f;

    private List<GameObject> activeArrows = new List<GameObject>();

    void Start()
    {
        GetComponent<Image>().raycastTarget = true;
    }

    void Update()
    {
        // 🔥 NUEVO: Detectar teclas de flecha si este bloque está seleccionado
        if (currentlySelectedBlock == this && type != BlockType.Conejo)
        {
            // Detectar teclas de flecha
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                TryMoveWithKeyboard(Vector2Int.down); // Invertido por la UI
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                TryMoveWithKeyboard(Vector2Int.up); // Invertido por la UI
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                TryMoveWithKeyboard(Vector2Int.left);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                TryMoveWithKeyboard(Vector2Int.right);
            }
        }
    }

    // 🔥 NUEVO: Método para intentar mover con teclado
    private void TryMoveWithKeyboard(Vector2Int gridDirection)
    {
        Debug.Log($"Intento de movimiento con teclado: {gridDirection}");

        if (GridManagerUI.Instance.CanMoveBlock(this, gridDirection))
        {
            GridManagerUI.Instance.MoveBlock(this, gridDirection);

            // Ocultar flechas después de mover
            HideArrows();
            currentlySelectedBlock = null;
        }
        else
        {
            Debug.Log($"No se puede mover en dirección {gridDirection} con teclado");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Si es el conejo, no hacer nada
        if (type == BlockType.Conejo)
        {
            Debug.Log("El conejo no se puede mover");
            return;
        }

        Debug.Log($"Click en {blockName}");

        // Ocultar flechas del bloque anterior si existe
        if (currentlySelectedBlock != null && currentlySelectedBlock != this)
        {
            currentlySelectedBlock.HideArrows();
        }

        ShowArrows();
        currentlySelectedBlock = this;
    }

    public void HideArrows()
    {
        foreach (GameObject arrow in activeArrows)
        {
            Destroy(arrow);
        }
        activeArrows.Clear();
    }

    void ShowArrows()
    {
        // Si es el conejo, no mostrar flechas
        if (type == BlockType.Conejo)
            return;

        Debug.Log($"ShowArrows ejecutándose para {blockName}");
        HideArrows();

        CreateArrow(Vector2Int.up, Direccion.Arriba);
        CreateArrow(Vector2Int.down, Direccion.Abajo);
        CreateArrow(Vector2Int.left, Direccion.Izquierda);
        CreateArrow(Vector2Int.right, Direccion.Derecha);
    }

    void CreateArrow(Vector2Int direction, Direccion direccion)
    {
        // Seleccionar el prefab según la dirección
        GameObject prefabToUse = null;
        switch (direccion)
        {
            case Direccion.Arriba:
                prefabToUse = arrowPrefabArriba;
                break;
            case Direccion.Abajo:
                prefabToUse = arrowPrefabAbajo;
                break;
            case Direccion.Izquierda:
                prefabToUse = arrowPrefabIzquierda;
                break;
            case Direccion.Derecha:
                prefabToUse = arrowPrefabDerecha;
                break;
        }

        if (prefabToUse == null)
        {
            Debug.LogError($"Flecha {direccion} no asignada en {blockName}");
            return;
        }

        GameObject arrow = Instantiate(prefabToUse, GridManagerUI.Instance.arrowsParent);
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        RectTransform blockRect = GetComponent<RectTransform>();

        Vector2 blockPos = blockRect.anchoredPosition;
        Vector2 blockSize = blockRect.sizeDelta;

        // CORRECCIÓN: Ajustar al centro del bloque (porque el pivote está en esq. superior izq)
        Vector2 blockCenter = new Vector2(
            blockPos.x + blockSize.x / 2,  // Centro X = posición X + mitad del ancho
            blockPos.y - blockSize.y / 2   // Centro Y = posición Y - mitad del alto (porque Y negativa es abajo en UI)
        );

        Vector2 arrowPos = blockCenter;
        float offset = arrowOffset;

        // Posicionar según dirección
        if (direccion == Direccion.Arriba)
        {
            arrowPos.y += blockSize.y / 2 + offset;
        }
        else if (direccion == Direccion.Abajo)
        {
            arrowPos.y -= blockSize.y / 2 + offset;
        }
        else if (direccion == Direccion.Izquierda)
        {
            arrowPos.x -= blockSize.x / 2 + offset;
        }
        else if (direccion == Direccion.Derecha)
        {
            arrowPos.x += blockSize.x / 2 + offset;
        }

        arrowRect.anchoredPosition = arrowPos;

        // Determinar qué dirección enviar al grid
        Vector2Int gridDirection;
        if (direccion == Direccion.Arriba)
        {
            gridDirection = Vector2Int.down;
        }
        else if (direccion == Direccion.Abajo)
        {
            gridDirection = Vector2Int.up;
        }
        else if (direccion == Direccion.Izquierda)
        {
            gridDirection = Vector2Int.left;
        }
        else
        {
            gridDirection = Vector2Int.right;
        }

        // Configurar botón
        Button arrowButton = arrow.GetComponent<Button>();
        arrowButton.onClick.RemoveAllListeners();
        arrowButton.onClick.AddListener(() => OnArrowClick(gridDirection));

        // Verificar si el movimiento es posible
        if (!GridManagerUI.Instance.CanMoveBlock(this, gridDirection))
        {
            arrowButton.interactable = false;
        }

        activeArrows.Add(arrow);
    }

    void OnArrowClick(Vector2Int gridDirection)
    {
        Debug.Log($"OnArrowClick recibió dirección de grid: {gridDirection}");

        // Verificar si se puede mover
        if (GridManagerUI.Instance.CanMoveBlock(this, gridDirection))
        {
            GridManagerUI.Instance.MoveBlock(this, gridDirection);
        }
        else
        {
            Debug.Log($"No se puede mover en dirección {gridDirection}");
        }

        HideArrows();
        currentlySelectedBlock = null;
    }

    public List<Vector2Int> GetOccupiedCells()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (orientation == BlockOrientation.Horizontal)
                {
                    cells.Add(new Vector2Int(gridPosition.x + x, gridPosition.y));
                }
                else
                {
                    cells.Add(new Vector2Int(gridPosition.x, gridPosition.y + y));
                }
            }
        }

        return cells;
    }

    void OnDestroy()
    {
        HideArrows();
        if (currentlySelectedBlock == this)
        {
            currentlySelectedBlock = null;
        }
    }

    // 🔥 NUEVO: Método estático para obtener el bloque seleccionado actualmente
    public static Block GetCurrentlySelectedBlock()
    {
        return currentlySelectedBlock;
    }
}