using UnityEngine;

public class PipeController : MonoBehaviour
{
    [Header("Configuración de Orientación Correcta")]
    [Tooltip("Ángulo Z correcto para resolver el puzzle (0, 90, 180, 270)")]
    public float anguloCorrecto = 0f;

    [Header("Referencias (Opcional)")]
    public bool mostrarDebug = true;

    private float anguloActual = 0f;
    private bool esCorrecta = false;

    void Start()
    {
        // Guardar el ángulo inicial
        anguloActual = transform.eulerAngles.z;
        VerificarOrientacion();
    }

    // Gira la tubería a la izquierda (90 grados)
    public void GirarIzquierda()
    {
        anguloActual += 90f;
        AplicarRotacion();
    }

    // Gira la tubería a la derecha (90 grados)
    public void GirarDerecha()
    {
        anguloActual -= 90f;
        AplicarRotacion();
    }

    private void AplicarRotacion()
    {
        // Normalizar el ángulo (mantenerlo entre 0 y 360)
        anguloActual = NormalizarAngulo(anguloActual);
        transform.eulerAngles = new Vector3(0, 0, anguloActual);
        VerificarOrientacion();

        if (mostrarDebug)
            Debug.Log($"{gameObject.name} rotado a {anguloActual}° - Correcta: {esCorrecta}");
    }

    private void VerificarOrientacion()
    {
        esCorrecta = Mathf.Abs(anguloActual - anguloCorrecto) < 0.1f;
    }

    public bool EstaCorrecta()
    {
        return esCorrecta;
    }

    private float NormalizarAngulo(float angulo)
    {
        angulo = angulo % 360f;
        if (angulo < 0) angulo += 360f;
        return angulo;
    }

    // Método para establecer orientación inicial desde el inspector
    public void EstablecerOrientacionInicial(float angulo)
    {
        anguloActual = NormalizarAngulo(angulo);
        transform.eulerAngles = new Vector3(0, 0, anguloActual);
        VerificarOrientacion();
    }
}