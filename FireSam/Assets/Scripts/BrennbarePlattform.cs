using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BrennbarePlattform : MonoBehaviour
{
    [Header("Optik im brennenden Zustand")]
    [SerializeField] private Color brennFarbe = new Color(1f, 0.4f, 0.1f);

    public bool Brennt { get; private set; }

    private SpriteRenderer sr;
    private Color normalFarbe;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        normalFarbe = sr.color;
    }

    public void Entzuende()
    {
        if (Brennt) return;
        Brennt = true;
        sr.color = brennFarbe;
    }

    public void Loesche()   // optional
    {
        Brennt = false;
        sr.color = normalFarbe;
    }
}