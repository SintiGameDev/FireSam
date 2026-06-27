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
    [SerializeField] private float maxAufstiegsGeschwindigkeit = 8f;

    [Header("Treibstoff")]
    [SerializeField] private float maxTreibstoff = 100f;
    [SerializeField] private float verbrauchProSekunde = 40f;
    [SerializeField] private float auffuellenProSekundeAmBoden = 60f;

    [Header("Auto-Wandabprall (seitlich)")]
    [Tooltip("Leer lassen = nutzt automatisch 'Boden Schicht'")]
    [SerializeField] private LayerMask wandSchicht;
    [Tooltip("Zusätzliche Reichweite über die halbe Spielerbreite hinaus")]
    [SerializeField] private float wandCheckPuffer = 0.15f;
    [SerializeField]
    private Vector2[] wandabprallProRettung = {
        new Vector2(10f, 8f), new Vector2(8f, 6.5f), new Vector2(6f, 5f), new Vector2(4f, 3.5f)
    };
    [SerializeField] private float wandSteuerSperre = 0.2f;
    [Tooltip("Schaltet die Konsolen-Diagnose an/aus")]
    [SerializeField] private bool wandabprallDebug = true;

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
    public float TreibstoffAnteil => treibstoff / maxTreibstoff;

    // wandSchicht falls leer -> bodenSchicht
    private LayerMask AktiveWandSchicht => wandSchicht.value == 0 ? bodenSchicht : wandSchicht;

    private float heartbeatTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        koerper = GetComponent<Collider2D>();
        treibstoff = maxTreibstoff;
        Physics2D.queriesStartInColliders = false;
        AktualisierePhysik();

        // BEDINGUNGSLOS – muss beim Play sofort erscheinen
        Debug.LogError($"[DIAG] SpielerBewegung läuft. bodenSchicht={bodenSchicht.value}, " +
                       $"wandSchicht={wandSchicht.value}, aktiv={AktiveWandSchicht.value}, " +
                       $"colliderTyp={koerper.GetType().Name}");
    }

    private void FixedUpdate()
    {
        // Heartbeat: jede Sekunde ein Log -> beweist, dass FixedUpdate läuft
        heartbeatTimer += Time.fixedDeltaTime;
        if (heartbeatTimer >= 1f)
        {
            heartbeatTimer = 0f;
            Bounds hb = koerper.bounds;
            Debug.Log($"[DIAG] heartbeat | amBoden={amBoden} | pos={(Vector2)transform.position} | " +
                      $"vel={rb.linearVelocity} | extents={hb.extents} | reichweite={hb.extents.x + wandCheckPuffer:F2}");
        }

        bool vorherAmBoden = amBoden;
        amBoden = Physics2D.OverlapCircle(bodenCheck.position, bodenCheckRadius, bodenSchicht);

        BewegeHorizontal();

        if (amBoden && !vorherAmBoden && rb.linearVelocity.y <= 0.1f)
            Abprallen();

        PruefeWandabprall();

        Jetpack();
        Treibstoffhaushalt();
    }

    private void PruefeWandabprall()
    {
        if (steuerSperreTimer > 0f)
        {
            steuerSperreTimer -= Time.fixedDeltaTime;
            return;
        }

        // Diagnose: castet IMMER, auch am Boden, ohne Layer-Filter
        Bounds b = koerper.bounds;
        float reichweite = b.extents.x + wandCheckPuffer;

        WandGetroffen(b, Vector2.right, reichweite, "RECHTS");
        WandGetroffen(b, Vector2.left, reichweite, "LINKS");

        if (amBoden) return;   // Abprall trotzdem nur in der Luft

        if (TrefferGefiltert(b, Vector2.right, reichweite)) WandAbprallen(-1);
        else if (TrefferGefiltert(b, Vector2.left, reichweite)) WandAbprallen(1);
    }

    // reine Diagnose – loggt JEDEN getroffenen Collider, egal welcher Layer
    private void WandGetroffen(Bounds b, Vector2 richtung, float reichweite, string label)
    {
        float[] yVersatz = { b.extents.y * 0.8f, 0f, -b.extents.y * 0.8f };
        foreach (float yo in yVersatz)
        {
            Vector2 start = new Vector2(b.center.x, b.center.y + yo);
            RaycastHit2D diag = Physics2D.Raycast(start, richtung, reichweite);
            bool hit = diag.collider != null;
            Debug.DrawRay(start, richtung * reichweite, hit ? Color.green : Color.red);

            if (hit)
            {
                int layer = diag.collider.gameObject.layer;
                bool imFilter = (AktiveWandSchicht.value & (1 << layer)) != 0;
                Debug.Log($"[DIAG] {label} trifft '{diag.collider.name}' | Layer='{LayerMask.LayerToName(layer)}' " +
                          $"| dist={diag.distance:F2} | imFilter={imFilter} | amBoden={amBoden}");
            }
        }
    }

    // echter, gefilterter Check für den Abprall
    private bool TrefferGefiltert(Bounds b, Vector2 richtung, float reichweite)
    {
        float[] yVersatz = { b.extents.y * 0.8f, 0f, -b.extents.y * 0.8f };
        foreach (float yo in yVersatz)
        {
            Vector2 start = new Vector2(b.center.x, b.center.y + yo);
            if (Physics2D.Raycast(start, richtung, reichweite, AktiveWandSchicht))
                return true;
        }
        return false;
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

    private void BewegeHorizontal()
    {
        if (steuerSperreTimer > 0f) return;
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

    private bool WandGetroffen(Bounds b, Vector2 richtung, float reichweite)
    {
        float[] yVersatz = { b.extents.y * 0.8f, 0f, -b.extents.y * 0.8f };
        bool treffer = false;

        foreach (float yo in yVersatz)
        {
            Vector2 start = new Vector2(b.center.x, b.center.y + yo);

            // echter Check (mit Layer-Filter)
            bool hit = Physics2D.Raycast(start, richtung, reichweite, AktiveWandSchicht);
            if (hit) treffer = true;
            Debug.DrawRay(start, richtung * reichweite, hit ? Color.green : Color.red);

            // Diagnose: ohne Layer-Filter -> trifft ALLES, zeigt den echten Layer
            if (wandabprallDebug)
            {
                RaycastHit2D diag = Physics2D.Raycast(start, richtung, reichweite);
                if (diag.collider != null)
                    Debug.Log($"[Wandabprall] Strahl {richtung} trifft '{diag.collider.name}' " +
                              $"auf Layer '{LayerMask.LayerToName(diag.collider.gameObject.layer)}' " +
                              $"(Distanz {diag.distance:F2}) – im Filter? " +
                              $"{((AktiveWandSchicht.value & (1 << diag.collider.gameObject.layer)) != 0)}");
            }
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
        steuerSperreTimer = wandSteuerSperre;

        if (wandabprallDebug) Debug.Log($"[Wandabprall] PRALLT! Richtung {richtungWeg}");
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

    public void Rueckstoss(Vector2 kraft, float sperre)
    {
        rb.linearVelocity = kraft;
        steuerSperreTimer = Mathf.Max(steuerSperreTimer, sperre);
    }
}