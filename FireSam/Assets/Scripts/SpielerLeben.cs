using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SpielerLeben : MonoBehaviour
{
    [SerializeField] private int maxLeben = 3;
    [SerializeField] private float unverwundbarkeitsdauer = 1.2f;
    [SerializeField] private float blinkIntervall = 0.1f;

    [Header("Rückstoß beim Verbrennen")]
    [SerializeField] private float rueckstossHoch = 14f;
    [SerializeField] private float rueckstossSeite = 8f;
    [SerializeField] private float rueckstossSperre = 0.25f;

    [Header("Feedback")]
    [SerializeField] private Color trefferFarbe = new Color(1f, 0.3f, 0.2f);
    [SerializeField] private SpriteRenderer grafik;

    private int leben;
    private float unverwundbarTimer;
    private float blinkTimer;
    private bool sichtbar = true;
    private bool tot;
    private Color normalFarbe;
    private Rigidbody2D rb;
    private SpielerBewegung bewegung;

    public int Leben => leben;
    public int MaxLeben => maxLeben;
    public bool Tot => tot;

    private void Awake()
    {
        leben = maxLeben;
        rb = GetComponent<Rigidbody2D>();
        bewegung = GetComponent<SpielerBewegung>();
        if (grafik == null) grafik = GetComponentInChildren<SpriteRenderer>();
        if (grafik != null) normalFarbe = grafik.color;
    }

    private void Update()
    {
        if (unverwundbarTimer <= 0f) return;
        unverwundbarTimer -= Time.deltaTime;

        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f)
        {
            blinkTimer = blinkIntervall;
            sichtbar = !sichtbar;
            if (grafik != null) grafik.color = sichtbar ? trefferFarbe : normalFarbe;
        }

        if (unverwundbarTimer <= 0f && grafik != null)
            grafik.color = normalFarbe;
    }

    public void NimmSchaden(int menge, Vector2 quelle)
    {
        if (tot || unverwundbarTimer > 0f) return;
        if (SpielManager.Instance != null && !SpielManager.Instance.SpielLaeuft) return;

        leben = Mathf.Max(0, leben - menge);

        if (leben <= 0) { Sterben(); return; }

        // weg von der Flamme springen
        float richtung = Mathf.Sign(transform.position.x - quelle.x);
        if (richtung == 0f) richtung = Random.value < 0.5f ? -1f : 1f;
        Vector2 kraft = new Vector2(richtung * rueckstossSeite, rueckstossHoch);

        if (bewegung != null) bewegung.Rueckstoss(kraft, rueckstossSperre);
        else rb.linearVelocity = kraft;

        unverwundbarTimer = unverwundbarkeitsdauer;
        blinkTimer = 0f;
    }

    private void Sterben()
    {
        tot = true;
        if (grafik != null) grafik.color = normalFarbe;
        if (bewegung != null) bewegung.enabled = false;
        if (SpielUI.Instance != null)
            SpielUI.Instance.ZeigeGameOver("GAME OVER", "Du bist verbrannt!");
    }

    private void OnGUI()   // HUD bleibt vorerst, Game-Over läuft über SpielUI
    {
        if (tot) return;
        var stil = new GUIStyle(GUI.skin.label) { fontSize = 22 };
        GUI.Label(new Rect(10, 38, 300, 40), $"Leben: {leben}/{maxLeben}", stil);
    }
}