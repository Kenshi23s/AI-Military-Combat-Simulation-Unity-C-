using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Flag of the parent CapturePoint: owner color, capture progress and unit count of each team.
/// The take and dispute panels fade in and out, the numbers pop when they change and the dispute panel pulses.
/// </summary>
public class Flag_UI : MonoBehaviour
{
    [SerializeField] private Image _flagOwner;

    [Header("OnTake")]
    [SerializeField] private GameObject OnTakeGO;
    [SerializeField] private TextMeshProUGUI UnitAmountText;

    [Header("OnDispute")]
    [SerializeField] private GameObject OnDisputeGO;
    [SerializeField, SerializedDictionary("Team", "TextMesh")] private SerializedDictionary<MilitaryTeam, TextMeshProUGUI> TeamTexts;

    private Material _mat;
    private CapturePoint _myCapturePoint;

    // Fade of each panel: its CanvasGroup and the alpha it moves toward.
    private CanvasGroup _takeGroup, _disputeGroup;
    private float _takeTargetAlpha, _disputeTargetAlpha;

    // Team shown by the take panel.
    private MilitaryTeam _takeTeam = MilitaryTeam.None;

    private Vector3 _disputeBaseScale;

    // Base scale of each number and the seconds left of its pop.
    private readonly Dictionary<Transform, Vector3> _textBaseScales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, float> _popTimers = new Dictionary<Transform, float>();
    private readonly List<Transform> _popKeys = new List<Transform>();

    // Seconds of a full fade between alpha 0 and 1.
    private const float FadeDuration = 0.2f;

    // Seconds and peak scale of the pop of a number.
    private const float PopDuration = 0.15f;
    private const float PopScale = 1.25f;

    // Pulses per second and scale amplitude of the dispute panel.
    private const float PulseFrequency = 1.5f;
    private const float PulseAmount = 0.05f;

    private void Awake()
    {
        _myCapturePoint = GetComponentInParent<CapturePoint>();
        if (_myCapturePoint == null)
        {
            Destroy(gameObject);
            return;
        }

        _mat = _flagOwner.material = new Material(_flagOwner.material);

        _takeGroup = GetOrAddCanvasGroup(OnTakeGO);
        _disputeGroup = GetOrAddCanvasGroup(OnDisputeGO);
        HidePanel(OnTakeGO, _takeGroup);
        HidePanel(OnDisputeGO, _disputeGroup);
        _disputeBaseScale = OnDisputeGO.transform.localScale;

        RegisterText(UnitAmountText);
        foreach (var text in TeamTexts.Values)
            RegisterText(text);

        _myCapturePoint.OnProgressChange.AddListener(UpdateProgressUI);
        _myCapturePoint.OnPointOwnerChange.AddListener(SetLetterColor);
        _myCapturePoint.OnTeamsInPointUpdate.AddListener(UpdateCounts);

        _myCapturePoint.OnDisputeStart += ActivateDisputeUI;
        _myCapturePoint.OnDisputeEnd += DeActivateDisputeUI;

        _myCapturePoint.OnBeingCaptured += ActivateOnTake;
        _myCapturePoint.OnStopCapture += DeActivateOnTake;

        UpdateProgressUI(_myCapturePoint.CaptureProgress);
        SetLetterColor(_myCapturePoint.CapturedBy);
    }

    private void LateUpdate()
    {
        Camera camera = Camera.main;
        if (camera != null)
            transform.forward = camera.transform.position - transform.position;

        FadePanel(OnTakeGO, _takeGroup, _takeTargetAlpha);
        FadePanel(OnDisputeGO, _disputeGroup, _disputeTargetAlpha);

        if (_disputeTargetAlpha > 0f)
        {
            float pulse = 1f + PulseAmount * Mathf.Sin(Time.time * PulseFrequency * 2f * Mathf.PI);
            OnDisputeGO.transform.localScale = _disputeBaseScale * pulse;
        }

        UpdatePops();
    }

    #region Panels

    private void ActivateOnTake(MilitaryTeam team)
    {
        _takeTeam = team;
        UnitAmountText.color = team == MilitaryTeam.Red ? Color.red : Color.blue;
        SetNumber(UnitAmountText, _myCapturePoint.UnitsOf(team));
        _takeTargetAlpha = 1f;
    }

    private void DeActivateOnTake()
    {
        _takeTargetAlpha = 0f;
    }

    private void ActivateDisputeUI()
    {
        foreach (var pair in TeamTexts)
            SetNumber(pair.Value, _myCapturePoint.UnitsOf(pair.Key));

        _disputeTargetAlpha = 1f;
    }

    private void DeActivateDisputeUI()
    {
        _disputeTargetAlpha = 0f;
        OnDisputeGO.transform.localScale = _disputeBaseScale;
    }

    // Moves the alpha of group toward targetAlpha. The panel is active while it is visible.
    private static void FadePanel(GameObject panel, CanvasGroup group, float targetAlpha)
    {
        if (targetAlpha > 0f && !panel.activeSelf)
            panel.SetActive(true);

        group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, Time.deltaTime / FadeDuration);

        if (targetAlpha <= 0f && group.alpha <= 0f && panel.activeSelf)
            panel.SetActive(false);
    }

    private static void HidePanel(GameObject panel, CanvasGroup group)
    {
        group.alpha = 0f;
        panel.SetActive(false);
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject panel)
    {
        return panel.TryGetComponent(out CanvasGroup group) ? group : panel.AddComponent<CanvasGroup>();
    }

    #endregion

    #region Numbers

    private void UpdateCounts(Dictionary<MilitaryTeam, IMilitary[]> col)
    {
        if (_takeTeam != MilitaryTeam.None)
            SetNumber(UnitAmountText, Count(col, _takeTeam));

        foreach (var pair in TeamTexts)
            SetNumber(pair.Value, Count(col, pair.Key));
    }

    private static int Count(Dictionary<MilitaryTeam, IMilitary[]> col, MilitaryTeam team)
    {
        return col.TryGetValue(team, out IMilitary[] units) ? units.Length : 0;
    }

    // Writes value in text and pops the text when the value changed.
    private void SetNumber(TextMeshProUGUI text, int value)
    {
        string newText = value.ToString();
        if (text.text == newText)
            return;

        text.text = newText;
        _popTimers[text.transform] = PopDuration;
    }

    private void RegisterText(TextMeshProUGUI text)
    {
        if (text != null && !_textBaseScales.ContainsKey(text.transform))
            _textBaseScales.Add(text.transform, text.transform.localScale);
    }

    // Shrinks each popped number from PopScale back to its base scale.
    private void UpdatePops()
    {
        if (_popTimers.Count == 0)
            return;

        _popKeys.Clear();
        _popKeys.AddRange(_popTimers.Keys);

        foreach (Transform text in _popKeys)
        {
            float timer = _popTimers[text] - Time.deltaTime;
            Vector3 baseScale = _textBaseScales[text];

            if (timer <= 0f)
            {
                text.localScale = baseScale;
                _popTimers.Remove(text);
                continue;
            }

            _popTimers[text] = timer;
            text.localScale = baseScale * Mathf.Lerp(1f, PopScale, timer / PopDuration);
        }
    }

    #endregion

    private void UpdateProgressUI(float progress)
    {
        _mat.SetFloat("_CaptureProgress", progress);

        Color color = progress >= 0 ? Color.red : Color.blue;

        _mat.SetColor("_ProgressFillColor", color);
    }

    private void SetLetterColor(MilitaryTeam team)
    {
        Color color;
        switch (team)
        {
            case MilitaryTeam.Blue:
                color = Color.blue;
                break;
            case MilitaryTeam.Red:
                color = Color.red;
                break;
            default:
                color = Color.white;
                break;
        }

        _mat.SetColor("_LetterColor", color);
    }
}
