using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CASO 3: APOCALIPSIS ZOMBI
/// Contexto: una infección ataca la ciudad.
/// Reglas del sistema:
///   - Hay 100 personas sanas y 1 zombi al inicio.
///   - Cada día, CADA zombi contagia a 1 persona sana.
///   - No hay cura (una persona infectada nunca vuelve a estar sana).
///   - La simulación termina cuando ya no quedan personas sanas.
/// </summary>
public class ZombieSimulation : MonoBehaviour
{
    [Header("CONDICIONES INICIALES (no cambian una vez empieza la simulación)")]
    [Tooltip("Cantidad de personas sanas al día 0")]
    public int poblacionInicialSana = 100;

    [Tooltip("Cantidad de zombis al día 0")]
    public int zombiesIniciales = 1;

    [Tooltip("Segundos reales que dura cada 'día' simulado")]
    public float tiempoPorDia = 1.5f;

    [Header("Parámetros de dibujo (grilla de personas)")]
    public int columnas = 11;
    public float espaciado = 1.2f;

    // ------------------ VARIABLES DE ESTADO ------------------
    // Estas SÍ cambian mientras se ejecuta la simulación
    // -----------------------------------------------------------
    private int personasSanas;
    private int personasZombi;
    private int diaActual;
    private bool simulacionActiva;

    private float temporizador;
    private GameObject[] personas;      // representación visual (un cubo por persona)
    private Material[] materiales;      // material propio de cada cubo (para poder pintarlo)
    private bool[] esZombi;             // estado de cada persona: true = zombi, false = sana

    // =========================================================
    // START(): condiciones iniciales + dibujo inicial del ecosistema
    // =========================================================
    void Start()
    {
        // 1) Configurar condiciones iniciales del sistema
        personasSanas = poblacionInicialSana;
        personasZombi = zombiesIniciales;
        diaActual = 0;
        simulacionActiva = true;
        temporizador = 0f;

        int totalPersonas = poblacionInicialSana + zombiesIniciales;
        personas = new GameObject[totalPersonas];
        materiales = new Material[totalPersonas];
        esZombi = new bool[totalPersonas];

        // 2) Crear un cubo por cada persona y ubicarlo en una grilla
        for (int i = 0; i < totalPersonas; i++)
        {
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            int fila = i / columnas;
            int col = i % columnas;

            cubo.transform.position = new Vector3(col * espaciado, -fila * espaciado, 0f);
            cubo.transform.localScale = Vector3.one * 0.9f;
            cubo.name = "Persona_" + i;

            // Creamos un material propio por cubo, con un shader "Unlit" que se ve
            // igual de bien en Built-in Render Pipeline o en URP (no depende de luces).
            Material mat = new Material(ObtenerShaderDisponible());
            cubo.GetComponent<Renderer>().material = mat;

            materiales[i] = mat;
            personas[i] = cubo;

            // Los primeros "zombiesIniciales" cubos arrancan infectados
            esZombi[i] = i < zombiesIniciales;
        }

        // 3) Ubicar la cámara para que encuadre toda la grilla de personas
        EncuadrarCamara(totalPersonas);

        Debug.Log("=== INICIO SIMULACIÓN: APOCALIPSIS ZOMBI ===");
        Debug.Log($"Día {diaActual}: Sanos = {personasSanas} | Zombis = {personasZombi}");

        // 4) Dibujar el estado inicial
        DrawView();
    }

    // Busca un shader "unlit" (no depende de luces) compatible con Built-in RP o URP,
    // para evitar que los cubos se vean blancos/lavados por falta de luz o por el
    // pipeline de renderizado del proyecto.
    private Shader ObtenerShaderDisponible()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Color");
        if (s == null) s = Shader.Find("Standard");
        return s;
    }

    // Posiciona la cámara principal para que encuadre toda la grilla de cubos.
    // Funciona tanto si el proyecto es 2D (cámara ortográfica mirando el plano X-Y)
    // como si es 3D (cámara en perspectiva).
    private void EncuadrarCamara(int totalPersonas)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        int filas = Mathf.CeilToInt((float)totalPersonas / columnas);
        float anchoTotal = (columnas - 1) * espaciado;
        float altoTotal = (filas - 1) * espaciado;
        Vector3 centro = new Vector3(anchoTotal / 2f, -altoTotal / 2f, 0f);

        if (cam.orthographic)
        {
            // Proyecto 2D: la cámara mira el plano X-Y desde -Z.
            cam.transform.position = new Vector3(centro.x, centro.y, -10f);

            float margen = 1.5f;
            float sizeVertical = (altoTotal / 2f) + margen;
            float sizeHorizontal = ((anchoTotal / 2f) + margen) / cam.aspect;
            cam.orthographicSize = Mathf.Max(sizeVertical, sizeHorizontal, 2f);
        }
        else
        {
            // Proyecto 3D: cámara en perspectiva mirando hacia la grilla.
            float distancia = Mathf.Max(anchoTotal, altoTotal) * 1.3f + 5f;
            cam.transform.position = centro + new Vector3(0f, 0f, -distancia);
            cam.transform.LookAt(centro);
        }
    }

    // =========================================================
    // UPDATE(): controla el paso del tiempo y dispara Simulate()
    // =========================================================
    void Update()
    {
        if (!simulacionActiva) return;

        temporizador += Time.deltaTime;

        if (temporizador >= tiempoPorDia)
        {
            temporizador = 0f;
            Simulate();
        }
    }

    // =========================================================
    // SIMULATE(): aplica las reglas de evolución del sistema (1 día)
    // =========================================================
    void Simulate()
    {
        diaActual++;

        // Regla del caso: cada zombi contagia a 1 persona sana por día,
        // pero no se puede contagiar a más personas de las que quedan sanas.
        int nuevosContagios = Mathf.Min(personasZombi, personasSanas);

        if (nuevosContagios > 0)
        {
            // Elegimos al azar, entre las personas sanas, quiénes se infectan hoy
            List<int> indicesSanos = new List<int>();
            for (int i = 0; i < esZombi.Length; i++)
            {
                if (!esZombi[i]) indicesSanos.Add(i);
            }

            for (int c = 0; c < nuevosContagios; c++)
            {
                int idxAleatorio = Random.Range(0, indicesSanos.Count);
                int indicePersona = indicesSanos[idxAleatorio];

                esZombi[indicePersona] = true;      // no hay cura -> el cambio es permanente
                indicesSanos.RemoveAt(idxAleatorio);
            }

            personasSanas -= nuevosContagios;
            personasZombi += nuevosContagios;
        }

        Debug.Log($"Día {diaActual}: Sanos = {personasSanas} | Zombis = {personasZombi} (+{nuevosContagios} contagios hoy)");

        // Actualizar la representación visual con el nuevo estado
        DrawView();

        // Condición de fin: ya no quedan personas sanas
        if (personasSanas <= 0)
        {
            simulacionActiva = false;
            Debug.Log($"=== FIN DE LA SIMULACIÓN: toda la población fue infectada en el día {diaActual} ===");
        }
    }

    // =========================================================
    // DRAWVIEW(): representa visualmente el estado actual en Game View
    // =========================================================
    void DrawView()
    {
        for (int i = 0; i < personas.Length; i++)
        {
            Color color = esZombi[i] ? Color.red : Color.green; // rojo = zombi, verde = sano
            Material mat = materiales[i];

            // Fijamos el color en ambas propiedades posibles según el shader/pipeline
            // que haya quedado activo, para asegurar que siempre se vea el color.
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        }
    }

    // =========================================================
    // UI simple en pantalla para ver día / conteos / fin de simulación
    // =========================================================
    void OnGUI()
    {
        GUIStyle style = new GUIStyle { fontSize = 20 };
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 400, 30), $"Día: {diaActual}", style);
        GUI.Label(new Rect(10, 35, 400, 30), $"Personas sanas: {personasSanas}", style);
        GUI.Label(new Rect(10, 60, 400, 30), $"Zombis: {personasZombi}", style);

        if (!simulacionActiva)
        {
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(10, 90, 600, 30), "SIMULACIÓN FINALIZADA: toda la población fue infectada", style);
        }
    }
}
