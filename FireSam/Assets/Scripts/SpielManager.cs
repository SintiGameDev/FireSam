using UnityEngine;

public class SpielManager : MonoBehaviour
{
    public static SpielManager Instance { get; private set; }

    [Header("Timer (Sekunden bis das Haus abbrennt)")]
    [SerializeField] private float gesamtzeit = 60f;

    [Header("Brennende Plattformen")]
    [Tooltip("Erst ab diesem Zeitanteil fangen Plattformen an zu brennen (0..1)")]
    [SerializeField] private float brennStartAnteil = 0.2f;
    [Tooltip("true = von unten nach oben (Feuer steigt), false = zufällig")]
    [SerializeField] private bool vonUntenNachOben = true;

    [Header("Debug-Anzeige (vorläufig, bis echtes UI da ist)")]
    [SerializeField] private bool zeigeDebugAnzeige = true;

    private float verstricheneZeit;
    private bool spielLaeuft = true;
    private BrennbarePlattform[] plattformen;
    private int aktuellBrennend;

    public bool SpielLaeuft => spielLaeuft;
    public float VerbleibendeZeit => Mathf.Max(0f, gesamtzeit - verstricheneZeit);
    public float ZeitAnteil => Mathf.Clamp01(verstricheneZeit / gesamtzeit);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        plattformen = FindObjectsByType<BrennbarePlattform>(FindObjectsSortMode.None);

        if (vonUntenNachOben)
            System.Array.Sort(plattformen, (a, b) =>
                a.transform.position.y.CompareTo(b.transform.position.y));
        else
            Mische(plattformen);
    }

    private void Update()
    {
        if (!spielLaeuft) return;

        verstricheneZeit += Time.deltaTime;
        AktualisiereBraende();

        if (verstricheneZeit >= gesamtzeit)
            HausBrenntAb();
    }

    private void AktualisiereBraende()
    {
        if (plattformen.Length == 0) return;

        float anteil = Mathf.InverseLerp(brennStartAnteil, 1f, ZeitAnteil);
        int sollen = Mathf.FloorToInt(anteil * plattformen.Length);

        while (aktuellBrennend < sollen && aktuellBrennend < plattformen.Length)
        {
            plattformen[aktuellBrennend].Entzuende();
            aktuellBrennend++;
        }
    }

    private void HausBrenntAb()
    {
        spielLaeuft = false;
        foreach (var p in plattformen)
            if (p != null) p.SofortVollbrand();

        var spielerGO = GameObject.FindGameObjectWithTag("Spieler");
        if (spielerGO != null)
        {
            var bew = spielerGO.GetComponent<SpielerBewegung>();
            if (bew != null) bew.enabled = false;
        }

        if (SpielUI.Instance != null)
            SpielUI.Instance.ZeigeGameOver("HAUS ABGEBRANNT",
                "Die Zeit ist abgelaufen – niemand mehr zu retten.");
    }

    private void OnGUI()
    {
        if (!zeigeDebugAnzeige || !spielLaeuft) return;
        GUIStyle stil = new GUIStyle(GUI.skin.label) { fontSize = 22 };
        GUI.Label(new Rect(10, 10, 500, 40), $"Zeit: {VerbleibendeZeit:F1}s", stil);
    }

    private void Mische(BrennbarePlattform[] feld)
    {
        for (int i = feld.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (feld[i], feld[j]) = (feld[j], feld[i]);
        }
    }
}