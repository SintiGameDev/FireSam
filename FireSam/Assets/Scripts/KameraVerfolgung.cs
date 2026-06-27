using UnityEngine;

public class KameraVerfolgung : MonoBehaviour
{
    [Header("Ziel")]
    [SerializeField] private Transform ziel;

    [Header("Offset (Verschiebung relativ zum Ziel)")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);

    [Header("Glättung")]
    [Tooltip("Höher = die Kamera folgt schneller/straffer. 1–3 weich, 8+ straff")]
    [SerializeField] private float lerpGeschwindigkeit = 5f;

    [Header("Achsen sperren (optional)")]
    [SerializeField] private bool folgeX = true;
    [SerializeField] private bool folgeY = true;

    private void LateUpdate()
    {
        if (ziel == null) return;

        Vector3 zielPosition = ziel.position + offset;

        // gesperrte Achsen behalten die aktuelle Kameraposition
        if (!folgeX) zielPosition.x = transform.position.x;
        if (!folgeY) zielPosition.y = transform.position.y;
        zielPosition.z = offset.z;   // Z bleibt fix, sonst clippt die 2D-Kamera

        transform.position = Vector3.Lerp(
            transform.position,
            zielPosition,
            1f - Mathf.Exp(-lerpGeschwindigkeit * Time.deltaTime));
    }
}