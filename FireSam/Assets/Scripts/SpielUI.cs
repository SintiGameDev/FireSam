using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SpielUI : MonoBehaviour
{
    public static SpielUI Instance { get; private set; }

    private GameObject panel;
    private Text titelText;
    private Text untertitelText;
    private bool angezeigt;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BaueUI();
    }

    public void ZeigeGameOver(string titel, string untertitel)
    {
        if (angezeigt) return;
        angezeigt = true;
        titelText.text = titel;
        untertitelText.text = untertitel;
        panel.SetActive(true);
    }

    private void BaueUI()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var canvasGO = new GameObject("SpielUI_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        panel = NeuesUIObjekt("Panel", canvasGO.transform);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.78f);
        Strecke(panel.GetComponent<RectTransform>());
        panel.SetActive(false);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        titelText = ErzeugeText("Titel", panel.transform, font, 90, FontStyle.Bold);
        titelText.color = new Color(1f, 0.55f, 0.15f);
        Positioniere(titelText.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(1400, 160));

        untertitelText = ErzeugeText("Untertitel", panel.transform, font, 42, FontStyle.Normal);
        Positioniere(untertitelText.rectTransform, new Vector2(0.5f, 0.46f), new Vector2(1400, 120));

        var btnGO = NeuesUIObjekt("NeustartButton", panel.transform);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.9f, 0.45f, 0.1f, 1f);
        var btn = btnGO.AddComponent<Button>();
        Positioniere(btnGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0.30f), new Vector2(360, 110));
        var btnText = ErzeugeText("Text", btnGO.transform, font, 40, FontStyle.Bold);
        btnText.text = "Neustart";
        Strecke(btnText.rectTransform);
        btn.onClick.AddListener(Neustart);
    }

    private void Neustart()
    {
        Time.timeScale = 1f;
        var s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
    }

    private GameObject NeuesUIObjekt(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private Text ErzeugeText(string name, Transform parent, Font font, int groesse, FontStyle stil)
    {
        var go = NeuesUIObjekt(name, parent);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = groesse;
        t.fontStyle = stil;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private void Positioniere(RectTransform rt, Vector2 anker, Vector2 groesse)
    {
        rt.anchorMin = rt.anchorMax = anker;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = groesse;
    }

    private void Strecke(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}