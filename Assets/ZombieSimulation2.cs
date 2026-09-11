using System.Collections.Generic;
using UnityEngine;
public class ZombieSimulation : MonoBehaviour
{
    public int poblacionInicialSana = 100;
    public int zombiesIniciales = 1;
    public float tiempoPorDia = 1.5f;
    public int columnas = 11;
    public float espaciado = 1.2f;
    private int personasSanas;
    private int personasZombi;
    private int diaActual;
    private bool simulacionActiva;
    private float temporizador;
    private GameObject[] personas;     
    private Material[] materiales;      
    private bool[] esZombi;             
    void Start()
    {
        personasSanas = poblacionInicialSana;
        personasZombi = zombiesIniciales;
        diaActual = 0;
        simulacionActiva = true;
        temporizador = 0f;
        int totalPersonas = poblacionInicialSana + zombiesIniciales;
        personas = new GameObject[totalPersonas];
        materiales = new Material[totalPersonas];
        esZombi = new bool[totalPersonas];
        for (int i = 0; i < totalPersonas; i++)
        {
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            int fila = i / columnas;
            int col = i % columnas;
            cubo.transform.position = new Vector3(col * espaciado, -fila * espaciado, 0f);
            cubo.transform.localScale = Vector3.one * 0.9f;
            cubo.name = "Persona_" + i;
            Material mat = new Material(ObtenerShaderDisponible());
            cubo.GetComponent<Renderer>().material = mat;
            materiales[i] = mat;
            personas[i] = cubo;
            esZombi[i] = i < zombiesIniciales;
        }
        Debug.Log("simulacion Zombis");
        Debug.Log($"Día {diaActual}: Sanos = {personasSanas} | Zombis = {personasZombi}");
        DrawView();
    }
    private Shader ObtenerShaderDisponible()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Color");
        if (s == null) s = Shader.Find("Standard");
        return s;
    }
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
    void Simulate()
    {
        diaActual++;
        int nuevosContagios = Mathf.Min(personasZombi, personasSanas);

        if (nuevosContagios > 0)
        {
            List<int> indicesSanos = new List<int>();
            for (int i = 0; i < esZombi.Length; i++)
            {
                if (!esZombi[i]) indicesSanos.Add(i);
            }

            for (int c = 0; c < nuevosContagios; c++)
            {
                int idxAleatorio = Random.Range(0, indicesSanos.Count);
                int indicePersona = indicesSanos[idxAleatorio];

                esZombi[indicePersona] = true;    
                indicesSanos.RemoveAt(idxAleatorio);
            }

            personasSanas -= nuevosContagios;
            personasZombi += nuevosContagios;
        }

        Debug.Log($"Día {diaActual}: Sanos = {personasSanas} | Zombis = {personasZombi} (+{nuevosContagios} contagios hoy)");
        DrawView();
        if (personasSanas <= 0)
        {
            simulacionActiva = false;
            Debug.Log($"en el {diaActual} todos fueron contagiados");
        }
    }
    // Apoyo de IA para esta función (asignación de colores por estado)
    void DrawView()
    {
        for (int i = 0; i < personas.Length; i++)
        {
            Color color = esZombi[i] ? Color.red : Color.green;
            Material mat = materiales[i];
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        }
    }
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
            GUI.Label(new Rect(10, 90, 600, 30), "toda la población fue infectada", style);
        }
    }
}
