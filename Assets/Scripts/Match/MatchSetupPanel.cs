using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel shown before the match where the player sets the units of each team, the civilians and the simulation speed.
/// Start Match saves the settings and calls TeamsManager.StartMatch.
/// </summary>
/// <remarks>The panel opens with the last saved settings, or with the inspector values of TeamsManager.</remarks>
public class MatchSetupPanel : MonoBehaviour
{
    [SerializeField] private TeamControls Blue = new TeamControls();
    [SerializeField] private TeamControls Red = new TeamControls();

    [SerializeField] private Slider Civilians;
    [SerializeField] private TMP_Text CiviliansValue;

    [SerializeField] private Slider TimeScale;
    [SerializeField] private TMP_Text TimeScaleValue;

    [SerializeField] private Button StartButton;
    [SerializeField] private Button ResetButton;

    // Settings shown in the panel and the defaults of the Reset button.
    private MatchSettings _settings;
    private MatchSettings _defaults;

    // The time scale slider moves in steps of this size.
    private const float TimeScaleStep = 0.25f;

    /// <summary>Sliders and value labels of one team.</summary>
    [Serializable]
    public class TeamControls
    {
        public Slider Fireteams;
        public TMP_Text FireteamsValue;
        public Slider MembersPerFireteam;
        public TMP_Text MembersPerFireteamValue;
        public Slider Planes;
        public TMP_Text PlanesValue;
    }

    private void Start()
    {
        _defaults = TeamsManager.instance.DefaultSettings();
        _settings = MatchSettings.LoadOrDefault(_defaults);

        SetupTeamSliders(Blue);
        SetupTeamSliders(Red);
        SetupSlider(Civilians, 0, MatchSettings.MaxCivilians, true);
        SetupSlider(TimeScale, MatchSettings.MinTimeScale, MatchSettings.MaxTimeScale, false);
        ShowSettings();

        Blue.Fireteams.onValueChanged.AddListener(HandleBlueFireteamsChanged);
        Blue.MembersPerFireteam.onValueChanged.AddListener(HandleBlueMembersPerFireteamChanged);
        Blue.Planes.onValueChanged.AddListener(HandleBluePlanesChanged);
        Red.Fireteams.onValueChanged.AddListener(HandleRedFireteamsChanged);
        Red.MembersPerFireteam.onValueChanged.AddListener(HandleRedMembersPerFireteamChanged);
        Red.Planes.onValueChanged.AddListener(HandleRedPlanesChanged);
        Civilians.onValueChanged.AddListener(HandleCiviliansChanged);
        TimeScale.onValueChanged.AddListener(HandleTimeScaleChanged);

        StartButton.onClick.AddListener(HandleStartButtonClick);
        ResetButton.onClick.AddListener(HandleResetButtonClick);
    }

    #region Handlers

    private void HandleBlueFireteamsChanged(float value) => SetFireteams(_settings.Blue, Blue, value);
    private void HandleBlueMembersPerFireteamChanged(float value) => SetMembersPerFireteam(_settings.Blue, Blue, value);
    private void HandleBluePlanesChanged(float value) => SetPlanes(_settings.Blue, Blue, value);
    private void HandleRedFireteamsChanged(float value) => SetFireteams(_settings.Red, Red, value);
    private void HandleRedMembersPerFireteamChanged(float value) => SetMembersPerFireteam(_settings.Red, Red, value);
    private void HandleRedPlanesChanged(float value) => SetPlanes(_settings.Red, Red, value);

    private void HandleCiviliansChanged(float value)
    {
        _settings.Civilians = Mathf.RoundToInt(value);
        CiviliansValue.text = _settings.Civilians.ToString();
    }

    private void HandleTimeScaleChanged(float value)
    {
        _settings.TimeScale = SnapTimeScale(value);
        TimeScaleValue.text = FormatTimeScale(_settings.TimeScale);
    }

    private void HandleStartButtonClick()
    {
        _settings.Save();
        TeamsManager.instance.StartMatch(_settings);
        gameObject.SetActive(false);
    }

    // Copies the inspector defaults into the panel.
    private void HandleResetButtonClick()
    {
        _settings = _defaults.Clone();
        ShowSettings();
    }

    #endregion

    private static void SetFireteams(TeamMatchSettings team, TeamControls controls, float value)
    {
        team.Fireteams = Mathf.RoundToInt(value);
        controls.FireteamsValue.text = team.Fireteams.ToString();
    }

    private static void SetMembersPerFireteam(TeamMatchSettings team, TeamControls controls, float value)
    {
        team.MembersPerFireteam = Mathf.RoundToInt(value);
        controls.MembersPerFireteamValue.text = team.MembersPerFireteam.ToString();
    }

    private static void SetPlanes(TeamMatchSettings team, TeamControls controls, float value)
    {
        team.Planes = Mathf.RoundToInt(value);
        controls.PlanesValue.text = team.Planes.ToString();
    }

    // Writes _settings into the sliders and labels without raising the change handlers.
    private void ShowSettings()
    {
        ShowTeam(Blue, _settings.Blue);
        ShowTeam(Red, _settings.Red);

        Civilians.SetValueWithoutNotify(_settings.Civilians);
        CiviliansValue.text = _settings.Civilians.ToString();

        TimeScale.SetValueWithoutNotify(_settings.TimeScale);
        TimeScaleValue.text = FormatTimeScale(_settings.TimeScale);
    }

    private static void ShowTeam(TeamControls controls, TeamMatchSettings team)
    {
        controls.Fireteams.SetValueWithoutNotify(team.Fireteams);
        controls.FireteamsValue.text = team.Fireteams.ToString();
        controls.MembersPerFireteam.SetValueWithoutNotify(team.MembersPerFireteam);
        controls.MembersPerFireteamValue.text = team.MembersPerFireteam.ToString();
        controls.Planes.SetValueWithoutNotify(team.Planes);
        controls.PlanesValue.text = team.Planes.ToString();
    }

    private static void SetupTeamSliders(TeamControls controls)
    {
        SetupSlider(controls.Fireteams, 0, MatchSettings.MaxFireteams, true);
        SetupSlider(controls.MembersPerFireteam, 1, MatchSettings.MaxMembersPerFireteam, true);
        SetupSlider(controls.Planes, 0, MatchSettings.MaxPlanes, true);
    }

    private static void SetupSlider(Slider slider, float min, float max, bool wholeNumbers)
    {
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
    }

    private static float SnapTimeScale(float value)
    {
        return Mathf.Round(value / TimeScaleStep) * TimeScaleStep;
    }

    private static string FormatTimeScale(float value)
    {
        return value.ToString("0.##") + "x";
    }
}
