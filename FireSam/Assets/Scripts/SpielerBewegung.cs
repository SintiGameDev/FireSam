using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
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

    [Header("Boden-Check")]
    [SerializeField] private Transform bodenCheck;
    [SerializeField] private float bodenCheckRadius = 0.2f;
    [SerializeField] private LayerMask bodenSchicht;

    [Header("Grafik (optional, nur Spiegeln)")]
    [SerializeField] private SpriteRenderer grafik;

    private Rigidbody2D rb;
    private float eingabeHorizontal;
    private bool jetpackGedrueckt;
    private bool amBoden;
    private int blickrichtung = 1;
    private int geretttePersonen = 0;
    private float treibstoff;

    public int AnzahlGerettet => geretttePersonen;
    public float TreibstoffAnteil => treibstoff / maxTreibstoff; // für UI-Balken

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        amBoden = Physics2D.OverlapCircle(bodenCheck.position, bodenCheckRadius, bodenSchicht);

        BewegeHorizontal();
        Jetpack();
        Treibstoffhaushalt();
    }

    private void BewegeHorizontal()
    {
        rb.linearVelocity = new Vector2(eingabeHorizontal * laufgeschwindigkeit, rb.linearVelocity.y);
    }

    private void Jetpack()
    {
        if (!jetpackGedrueckt || treibstoff <= 0f) return;

        // Schub wirkt gegen die (skalierte) Schwerkraft -> mehr Personen = weniger Bremswirkung
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

    // Auto-Abprall: nur wenn von oben auf eine Boden-Fläche gelandet
    private void OnCollisionEnter2D(Collision2D k)
    {
        if (((1 << k.gameObject.layer) & bodenSchicht) == 0) return;

        foreach (var kontakt in k.contacts)
        {
            if (kontakt.normal.y > 0.5f)
            {
                Abprallen();
                break;
            }
        }
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
        if (bodenCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(bodenCheck.position, bodenCheckRadius);
    }
}