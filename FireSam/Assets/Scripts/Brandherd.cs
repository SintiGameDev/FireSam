using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class Brandherd : MonoBehaviour
{
    [SerializeField] private int schaden = 1;

    private ParticleSystem ps;
    private CircleCollider2D hurtbox;
    private bool eingerichtet;
    private static Material flammenMaterial;

    public void Initialisiere(float startGroesse)
    {
        Setup();
        SetGroesse(startGroesse);
    }

    private void Awake() => Setup();

    private void Setup()
    {
        if (eingerichtet) return;
        eingerichtet = true;

        ps = GetComponent<ParticleSystem>();
        KonfiguriereParticles();

        hurtbox = gameObject.AddComponent<CircleCollider2D>();
        hurtbox.isTrigger = true;
        hurtbox.radius = 0.3f;
    }

    public void SetGroesse(float g)
    {
        g = Mathf.Max(0.05f, g);
        var main = ps.main;
        main.startSize = new ParticleSystem.MinMaxCurve(g * 0.7f, g * 1.2f);
        var emission = ps.emission;
        emission.rateOverTime = 6f + g * 28f;
        var shape = ps.shape;
        shape.radius = g * 0.35f;
        if (hurtbox != null) hurtbox.radius = g * 0.6f;
    }

    private void KonfiguriereParticles()
    {
        var main = ps.main;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.3f, 2.4f);
        main.startSize = 0.25f;
        main.startColor = Color.white;
        main.gravityModifier = -0.25f;                       // zusätzlicher Auftrieb
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 35;

        var emission = ps.emission;
        emission.rateOverTime = 14f;

        // Kegel nach OBEN drehen (Default zeigt +Z = in den Bildschirm)
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 14f;
        shape.radius = 0.1f;
        shape.rotation = new Vector3(-90f, 0f, 0f);          // +Z -> +Y

        // Farbverlauf gelb -> orange -> rot -> aus (harte Stufen = retro)
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(new Color(1f, 0.95f, 0.45f), 0.0f),
                new GradientColorKey(new Color(1f, 0.6f, 0.12f), 0.35f),
                new GradientColorKey(new Color(0.92f, 0.22f, 0.05f), 0.7f),
                new GradientColorKey(new Color(0.3f, 0.07f, 0.05f), 1.0f)
            },
            new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve kurve = new AnimationCurve(
            new Keyframe(0f, 0.5f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.15f));
        size.size = new ParticleSystem.MinMaxCurve(1f, kurve);

        var rend = GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.material = HoleMaterial();
        rend.sortingOrder = 20;
    }

    private static Material HoleMaterial()
    {
        if (flammenMaterial != null) return flammenMaterial;

        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        flammenMaterial = new Material(sh);

        Texture2D tex = ErzeugePixelFlamme();
        flammenMaterial.mainTexture = tex;
        if (flammenMaterial.HasProperty("_BaseMap"))
            flammenMaterial.SetTexture("_BaseMap", tex);
        return flammenMaterial;
    }

    private static Texture2D ErzeugePixelFlamme()
    {
        int s = 8;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Vector2 c = new Vector2((s - 1) / 2f, (s - 1) / 2f);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x - c.x) / (s * 0.5f);
                float dy = (y - c.y) / (s * 0.6f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, d < 0.8f ? 1f : 0f));
            }
        tex.Apply();
        return tex;
    }

    private void OnTriggerStay2D(Collider2D other) => Verletze(other);
    private void OnTriggerEnter2D(Collider2D other) => Verletze(other);

    private void Verletze(Collider2D other)
    {
        if (!other.CompareTag("Spieler")) return;
        var leben = other.GetComponent<SpielerLeben>();
        if (leben != null) leben.NimmSchaden(schaden, transform.position);
    }
}