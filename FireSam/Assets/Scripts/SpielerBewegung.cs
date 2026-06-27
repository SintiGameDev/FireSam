using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SpielerBewegung : MonoBehaviour
{
    [Header("Laufen")]
    [SerializeField] private float laufgeschwindigkeit = 7f;

    [Header("Abprall-Höhe (Index 0 = 0 Personen ... 3 = 3 Personen)")]
    [SerializeField] private float[] abprallkraftProRettung = { 16f, 13f, 10f, 7.5f };

    [Header("Schwerkraft je Rettung (gravityScale, Index 0..3)")]
    [SerializeField] private float[] schwerkraftProRettung = { 2.5f, 3.5f, 5f, 7f };

    [Header("Jetpack (Wasser) – Schub je Rettung, Index 0..3")]
    [SerializeField] private float[] jetpackSchubProRettung = { 38f, 32f, 27f, 23f };
    [Tooltip("Obergrenze der Aufstiegsgeschwindigkeit, damit man nicht endlos beschleunigt")]
    [SerializeField] private float maxAufstiegsGeschwindigkeit = 8f;

    [Header("Treibstoff")]
    [SerializeField] private float maxTreibstoff = 100f;
    [SerializeField] private float verbrauchProSekunde = 40f;
    [SerializeField] private float auffuellenProSekundeAmBoden = 60f;

    [Header("Auto-Wandabprall (seitlich)")]
    [Tooltip("Layer der Wände/Plattformen, an denen seitlich abgeprallt wird – auf 'Boden' setzen!")]
    [SerializeField] private LayerMask wandSchicht;
    [Tooltip("Zusätzliche Reichweite über die halbe Spielerbreite hinaus")]
    [SerializeField] private float wandCheckPuffer = 0.08f;
    [Tooltip("x = Wegstoß horizontal, y = Schub nach oben. Index 0..3 nach Personen")]
    [SerializeField]
    private Vector2[] wandabprallProRettung = {
        new Vector2(10f, 8f), new Vector2(8f, 6.5f), new Vector2(6f, 5f), new Vector2(4f, 3.5f)
    };
    [Tooltip("Sekunden, in denen die Links/Rechts-Steuerung nach Wandabprall gesperrt ist")]
    [SerializeField] private float wandSteuerSperre = 0.2f;

    [Header("Boden-Check")]
    [SerializeField] private Transform bodenCheck;
    [SerializeField] private float bodenCheckRadius = 0.2f;
    [SerializeField] private LayerMask bodenSchicht;

    [Header("Grafik (optional, nur Spiegeln)")]
    [SerializeField] private SpriteRenderer grafik;

    private Rigidbody2D rb;
    private Collider2D koerper;
    private float eingabeHorizontal;
    private bool jetpackGedrueckt;
    private bool amBoden;
    private int blickrichtung = 1;
    private int geretttePersonen = 0;
    private float treibstoff;
    private float steuerSperreTimer;

    public int AnzahlGerettet => geretttePersonen;
    public float TreibstoffAnteil => treibstoff / maxTreibstoff; // für UI-Balken

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        koerper = GetComponent<Collider2D>();
        treibstoff = maxTreibstoff;
        AktualisierePhysik();
    }

    private void Update()
    {
        eingabeHorizontal = Input.GetAxisRaw("Horizontal");
        jetpackGedrueckt = Input.GetKey(KeyCode.Space);

        if (eingabeHorizontal != 0)
        {
            blickrichtung = (int)Mathf.Sign(eingabeHorizontal);
            if (grafik != null) grafik.flipX = blickrichtung < 0;
        }
    }

    private void FixedUpdate()
    {
        bool vorherAmBoden = amBoden;
        amBoden = Physics2D.OverlapCircle(bodenCheck.position, bodenCheckRadius, bodenSchicht);

        BewegeHorizontal();

        // vertikaler Abprall beim Landen (steigende Flanke)
        if (amBoden && !vorherAmBoden && rb.linearVelocity.y <= 0.1f)
            Abprallen();

        PruefeWandabprall();   // seitlicher Abprall

        Jetpack();
        Treibstoffhaushalt();
    }

    private void BewegeHorizontal()
    {
        if (steuerSperreTimer > 0f) return;   // nach Wandabprall kurz gesperrt
        rb.linearVelocity = new Vector2(eingabeHorizontal * laufgeschwindigkeit, rb.linearVelocity.y);
    }

    private void Jetpack()
    {
        if (!jetpackGedrueckt || treibstoff <= 0f) return;

        rb.linearVelocity += Vector2.up * AktuellerSchub() * Time.fixedDeltaTime;

        if (rb.linearVelocity.y > maxAufstiegsGeschwindigkeit)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxAufstiegsGeschwindigkeit);
    }

    private void Treibstoffhaushalt()
    {
        if (jetpackGedrueckt && treibstoff > 0f)
            treibstoff -= verbrauchProSekunde * Time.fixedDeltaTime;
        else if (amBoden)
            treibstoff += auffuellenProSekundeAmBoden * Time.fixedDeltaTime;

        treibstoff = Mathf.Clamp(treibstoff, 0f, maxTreibstoff);
    }

    private void PruefeWandabprall()
    {
        if (steuerSperreTimer > 0f)              // Sperre läuft -> nur runterzählen
        {
            steuerSperreTimer -= Time.fixedDeltaTime;
            return;
        }
        if (amBoden) return;                     // nur in der Luft abprallen

        Bounds b = koerper.bounds;
        float reichweite = b.extents.x + wandCheckPuffer;

        if (WandGetroffen(b, Vector2.right, reichweite)) WandAbprallen(-1); // Wand rechts -> nach links
        else if (WandGetroffen(b, Vector2.left, reichweite)) WandAbprallen(1);  // Wand links -> nach rechts
    }

    // drei Strahlen pro Seite (oben/Mitte/unten), grün = Treffer, rot = nichts
    private bool WandGetroffen(Bounds b, Vector2 richtung, float reichweite)
    {
        float[] yVersatz = { b.extents.y * 0.8f, 0f, -b.extents.y * 0.8f };
        bool treffer = false;

        foreach (float yo in yVersatz)
        {
            Vector2 start = new Vector2(b.center.x, b.center.y + yo);
            bool hit = Physics2D.Raycast(start, richtung, reichweite, wandSchicht);
            if (hit) treffer = true;
            Debug.DrawRay(start, richtung * reichweite, hit ? Color.green : Color.red);
        }
        return treffer;
    }

    private void WandAbprallen(int richtungWeg)
    {
        int i = Mathf.Clamp(geretttePersonen, 0, wandabprallProRettung.Length - 1);
        Vector2 kraft = wandabprallProRettung[i];

        rb.linearVelocity = new Vector2(richtungWeg * kraft.x, kraft.y);

        blickrichtung = richtungWeg;
        if (grafik != null) grafik.flipX = blickrichtung < 0;

        steuerSperreTimer = wandSteuerSperre;    // Steuerung kurz sperren
    }

    private void Abprallen()
    {
        int i = Mathf.Clamp(geretttePersonen, 0, abprallkraftProRettung.Length - 1);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, abprallkraftProRettung[i]);
    }

    private float AktuellerSchub()
    {
        int i = Mathf.Clamp(geretttePersonen, 0, jetpackSchubProRettung.Length - 1);
        return jetpackSchubProRettung[i];
    }

    public void AktualisierePhysik()
    {
        int i = Mathf.Clamp(geretttePersonen, 0, schwerkraftProRettung.Length - 1);
        rb.gravityScale = schwerkraftProRettung[i];
    }

    public void PersonGerettet()
    {
        geretttePersonen = Mathf.Min(geretttePersonen + 1, 3);
        AktualisierePhysik();
    }

    private void OnDrawGizmosSelected()
    {
        if (bodenCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(bodenCheck.position, bodenCheckRadius);
        }
    }
}