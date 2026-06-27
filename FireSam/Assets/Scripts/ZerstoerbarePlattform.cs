using UnityEngine;

public class ZerstoerbarePlattform : MonoBehaviour
{
    [Header("Zerstörung durch Feuer")]
    [Tooltip("Soll die Plattform durch Feuer zerstört werden? (Benötigt das Skript BrennbarePlattform)")]
    [SerializeField] private bool zerstoerbarDurchFeuer = true;
    [Tooltip("Ab welcher Feuer-Intensität (0.0 bis 1.0) soll die Plattform verschwinden?")]
    [SerializeField, Range(0.1f, 1f)] private float zerstoerungsIntensitaet = 1f;

    [Header("Zerstörung durch Spieler")]
    [Tooltip("Soll die Plattform durch einen seitlichen Wandsprung zerstört werden?")]
    [SerializeField] private bool zerstoerbarDurchWandsprung = true;

    private BrennbarePlattform brennbarePlattform;

    private void Awake()
    {
        // Holt sich die Referenz auf das Brand-Skript, falls vorhanden
        brennbarePlattform = GetComponent<BrennbarePlattform>();
    }

    private void Update()
    {
        if (zerstoerbarDurchFeuer && brennbarePlattform != null)
        {
            // Wenn das Feuer die definierte Intensität erreicht hat, zerstören
            if (brennbarePlattform.Intensitaet >= zerstoerungsIntensitaet)
            {
                Zerstoeren();
            }
        }
    }

    // Wird vom Raycast in SpielerBewegung.cs aufgerufen
    public void WandAbprallErlebt()
    {
        if (zerstoerbarDurchWandsprung)
        {
            Zerstoeren();
        }
    }

    private void Zerstoeren()
    {
        // Das Zerstören des GameObjects entfernt automatisch auch alle daran hängenden Brandherde (Particle Systems)
        Destroy(gameObject);
    }
}