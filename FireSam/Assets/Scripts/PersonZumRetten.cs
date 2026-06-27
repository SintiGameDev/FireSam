using UnityEngine;

public class PersonZumRetten : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D anderer)
    {
        if (!anderer.CompareTag("Spieler")) return;
        if (SpielManager.Instance != null && !SpielManager.Instance.SpielLaeuft) return; // Haus abgebrannt

        var spieler = anderer.GetComponent<SpielerBewegung>();
        if (spieler != null && spieler.AnzahlGerettet < 3)
        {
            spieler.PersonGerettet();
            gameObject.SetActive(false);
        }
    }
}
