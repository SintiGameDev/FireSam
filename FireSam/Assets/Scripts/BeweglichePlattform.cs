using UnityEngine;

public class BeweglichePlattform : MonoBehaviour
{
    [SerializeField] private Vector2 richtung = Vector2.right;
    [SerializeField] private float strecke = 3f;
    [SerializeField] private float geschwindigkeit = 2f;

    private Vector2 startPosition;

    private void Start() => startPosition = transform.position;

    private void Update()
    {
        float versatz = Mathf.PingPong(Time.time * geschwindigkeit, strecke);
        transform.position = startPosition + richtung.normalized * versatz;
    }

    private void OnCollisionEnter2D(Collision2D k)
    {
        if (k.collider.CompareTag("Spieler"))
            k.collider.transform.SetParent(transform);
    }

    private void OnCollisionExit2D(Collision2D k)
    {
        if (k.collider.CompareTag("Spieler"))
            k.collider.transform.SetParent(null);
    }
}