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
    [SerializeField] private TeamControls _blue = new TeamControls();
    [SerializeField] private TeamControls _red = new TeamControls();

    [SerializeField] private Slider _civilians;
    [SerializeField] private TMP_Text _civiliansValue;

    [SerializeField] private Slider _timeScale;
    [SerializeField] private TMP_Text _timeScaleValue;

    [SerializeField] private Button _startButton;
    [SerializeField] private Button _resetButton;

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

        BindTeam(_blue, () => _settings.Blue);
        BindTeam(_red, () => _settings.Red);

        BindSlider(_civilians, _civiliansValue, 0, MatchSettings.MaxCivilians, true,
            () => _settings.Civilians, value => _settings.Civilians = Mathf.RoundToInt(value), value => value.ToString("0"));

        BindSlider(_timeScale, _timeScaleValue, MatchSettings.MinTimeScale, MatchSettings.MaxTimeScale, false,
            () => _settings.TimeScale, value => _settings.TimeScale = SnapTimeScale(value), value => SnapTimeScale(value).ToString("0.##") + "x");

        _startButton.onClick.AddListener(StartMatch);
        _resetButton.onClick.AddListener(ResetToDefaults);
    }

    private void StartMatch()
    {
        _settings.Save();
        TeamsManager.instance.StartMatch(_settings);
        gameObject.SetActive(false);
    }

    // Copies the inspector defaults into the panel.
    private void ResetToDefaults()
    {
        _settings = _defaults.Clone();
        RefreshTeam(_blue, _settings.Blue);
        RefreshTeam(_red, _settings.Red);
        _civilians.value = _settings.Civilians;
        _timeScale.value = _settings.TimeScale;
    }

    // getTeam returns the current settings of the team, so the bindings follow ResetToDefaults.
    private void BindTeam(TeamControls controls, Func<TeamMatchSettings> getTeam)
    {
        BindSlider(controls.Fireteams, controls.FireteamsValue, 0, MatchSettings.MaxFireteams, true,
            () => getTeam().Fireteams, value => getTeam().Fireteams = Mathf.RoundToInt(value), value => value.ToString("0"));

        BindSlider(controls.MembersPerFireteam, controls.MembersPerFireteamValue, 1, MatchSettings.MaxMembersPerFireteam, true,
            () => getTeam().MembersPerFireteam, value => getTeam().MembersPerFireteam = Mathf.RoundToInt(value), value => value.ToString("0"));

        BindSlider(controls.Planes, controls.PlanesValue, 0, MatchSettings.MaxPlanes, true,
            () => getTeam().Planes, value => getTeam().Planes = Mathf.RoundToInt(value), value => value.ToString("0"));
    }

    private static void RefreshTeam(TeamControls controls, TeamMatchSettings team)
    {
        controls.Fireteams.value = team.Fireteams;
        controls.MembersPerFireteam.value = team.MembersPerFireteam;
        controls.Planes.value = team.Planes;
    }

    // Sets the range and value of slider, and writes each change to the settings and to label.
    private static void BindSlider(Slider slider, TMP_Text label, float min, float max, bool wholeNumbers,
        Func<float> read, Action<float> write, Func<float, string> format)
    {
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.SetValueWithoutNotify(read());
        label.text = format(slider.value);

        slider.onValueChanged.AddListener(value =>
        {
            write(value);
            label.text = format(value);
        });
    }

    private static float SnapTimeScale(float value)
    {
        return Mathf.Round(value / TimeScaleStep) * TimeScaleStep;
    }
}
