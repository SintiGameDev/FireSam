using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BrennbarePlattform : MonoBehaviour
{
    [Header("Ausbreitung")]
    [Tooltip("Sekunden vom Entzünden bis zum Vollbrand")]
    [SerializeField] private float ausbreitungsdauer = 10f;
    [Tooltip("Brandherde pro Flächeneinheit (mehr = dichter)")]
    [SerializeField] private float herdeProEinheit = 2.5f;
    [SerializeField] private int maxHerdeObergrenze = 40;
    [SerializeField] private float herdMaxGroesse = 0.6f;
    [Tooltip("Wahrscheinlichkeit, dass ein neuer Herd an einem bestehenden entsteht (Klumpen)")]
    [SerializeField, Range(0f, 1f)] private float klumpenWahrscheinlichkeit = 0.65f;
    [SerializeField] private float klumpenRadius = 0.5f;

    [Header("Verkohlung (Plattform dunkelt nach)")]
    [SerializeField] private Color verkohltFarbe = new Color(0.3f, 0.25f, 0.25f);
    [SerializeField, Range(0f, 1f)] private float maxVerkohlung = 0.6f;

    public bool Brennt { get; private set; }
    public float Intensitaet { get; private set; }

    private SpriteRenderer sr;
    private Color normalFarbe;
    private float startZeit;
    private int zielHerde;
    private readonly List<Brandherd> herde = new();

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        normalFarbe = sr.color;
    }

    private void BerechneZiel()
    {
        if (zielHerde > 0) return;
        Vector2 g = sr.bounds.size;
        float flaeche = Mathf.Max(0.1f, g.x * g.y);
        zielHerde = Mathf.Clamp(Mathf.CeilToInt(flaeche * herdeProEinheit), 2, maxHerdeObergrenze);
    }

    public void Entzuende()
    {
        if (Brennt) return;
        Brennt = true;
        BerechneZiel();
        startZeit = Time.time;
        ErzeugeHerd();
    }

    private void Update()
    {
        if (!Brennt) return;

        Intensitaet = Mathf.Clamp01((Time.time - startZeit) / ausbreitungsdauer);

        int soll = Mathf.Max(1, Mathf.CeilToInt(Intensitaet * zielHerde));
        while (herde.Count < soll && herde.Count < zielHerde)
            ErzeugeHerd();

        float groesse = Mathf.Lerp(0.15f, herdMaxGroesse, Intensitaet);
        foreach (var h in herde)
            if (h != null) h.SetGroesse(groesse);

        if (maxVerkohlung > 0f)
            sr.color = Color.Lerp(normalFarbe, verkohltFarbe, Intensitaet * maxVerkohlung);
    }

    public void SofortVollbrand()
    {
        Brennt = true;
        BerechneZiel();
        startZeit = Time.time - ausbreitungsdauer;
        Intensitaet = 1f;

        while (herde.Count < zielHerde)
            ErzeugeHerd();
        foreach (var h in herde)
            if (h != null) h.SetGroesse(herdMaxGroesse);

        if (maxVerkohlung > 0f)
            sr.color = Color.Lerp(normalFarbe, verkohltFarbe, maxVerkohlung);
    }

    private void ErzeugeHerd()
    {
        Bounds b = sr.bounds;
        float rand = 0.1f;
        float x, y;

        if (herde.Count > 0 && Random.value < klumpenWahrscheinlichkeit)
        {
            Vector3 basis = herde[Random.Range(0, herde.Count)].transform.position;
            x = Mathf.Clamp(basis.x + Random.Range(-klumpenRadius, klumpenRadius), b.min.x + rand, b.max.x - rand);
            y = Mathf.Clamp(basis.y + Random.Range(-klumpenRadius, klumpenRadius), b.min.y + rand, b.max.y - rand);
        }
        else
        {
            x = Random.Range(b.min.x + rand, b.max.x - rand);
            y = Random.Range(b.min.y + rand, b.max.y - rand);
        }

        var go = new GameObject("Brandherd");
        go.transform.position = new Vector3(x, y, transform.position.z - 0.1f);
        go.transform.SetParent(transform, true);

        // Plattform-Skalierung neutralisieren -> Hitbox & Pixel bleiben korrekt
        Vector3 ls = transform.lossyScale;
        go.transform.localScale = new Vector3(
            ls.x != 0f ? 1f / ls.x : 1f,
            ls.y != 0f ? 1f / ls.y : 1f, 1f);

        go.AddComponent<ParticleSystem>();
        var herd = go.AddComponent<Brandherd>();
        herd.Initialisiere(0.15f);
        herde.Add(herd);
    }

    public void Loesche()
    {
        Brennt = false;
        Intensitaet = 0f;
        sr.color = normalFarbe;
        foreach (var h in herde)
            if (h != null) Destroy(h.gameObject);
        herde.Clear();
    }
}