using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD panel that sends reinforcements during the match: fireteams and planes for each team, and civilians.
/// Each button also has a hotkey.
/// </summary>
/// <remarks>The panel stays hidden until TeamsManager starts the match.</remarks>
public class ReinforcementPanel : MonoBehaviour
{
    [SerializeField] private GameObject Content;

    [SerializeField] private Button BlueFireteam;
    [SerializeField] private Button BluePlane;
    [SerializeField] private Button RedFireteam;
    [SerializeField] private Button RedPlane;
    [SerializeField] private Button Civilians;

    /// <summary>Civilians sent by each civilian reinforcement.</summary>
    public const int CiviliansPerReinforcement = 5;

    /// <summary>Hotkey of the Blue fireteam reinforcement.</summary>
    public const KeyCode BlueFireteamKey = KeyCode.Alpha1;

    /// <summary>Hotkey of the Blue plane reinforcement.</summary>
    public const KeyCode BluePlaneKey = KeyCode.Alpha2;

    /// <summary>Hotkey of the Red fireteam reinforcement.</summary>
    public const KeyCode RedFireteamKey = KeyCode.Alpha9;

    /// <summary>Hotkey of the Red plane reinforcement.</summary>
    public const KeyCode RedPlaneKey = KeyCode.Alpha0;

    /// <summary>Hotkey of the civilian reinforcement.</summary>
    public const KeyCode CiviliansKey = KeyCode.C;

    private void Start()
    {
        BlueFireteam.onClick.AddListener(HandleBlueFireteamButtonClick);
        BluePlane.onClick.AddListener(HandleBluePlaneButtonClick);
        RedFireteam.onClick.AddListener(HandleRedFireteamButtonClick);
        RedPlane.onClick.AddListener(HandleRedPlaneButtonClick);
        Civilians.onClick.AddListener(HandleCiviliansButtonClick);

        Content.SetActive(TeamsManager.instance.MatchStarted);
        TeamsManager.instance.OnMatchStarted += HandleMatchStarted;
    }

    private void OnDestroy()
    {
        if (TeamsManager.instance != null)
            TeamsManager.instance.OnMatchStarted -= HandleMatchStarted;
    }

    private void Update()
    {
        if (!TeamsManager.instance.MatchStarted)
            return;

        if (Input.GetKeyDown(BlueFireteamKey)) HandleBlueFireteamButtonClick();
        if (Input.GetKeyDown(BluePlaneKey)) HandleBluePlaneButtonClick();
        if (Input.GetKeyDown(RedFireteamKey)) HandleRedFireteamButtonClick();
        if (Input.GetKeyDown(RedPlaneKey)) HandleRedPlaneButtonClick();
        if (Input.GetKeyDown(CiviliansKey)) HandleCiviliansButtonClick();
    }

    private void HandleMatchStarted(MatchSettings settings)
    {
        Content.SetActive(true);
    }

    private void HandleBlueFireteamButtonClick()
    {
        TeamsManager.instance.ReinforceFireteam(MilitaryTeam.Blue);
    }

    private void HandleBluePlaneButtonClick()
    {
        TeamsManager.instance.ReinforcePlane(MilitaryTeam.Blue);
    }

    private void HandleRedFireteamButtonClick()
    {
        TeamsManager.instance.ReinforceFireteam(MilitaryTeam.Red);
    }

    private void HandleRedPlaneButtonClick()
    {
        TeamsManager.instance.ReinforcePlane(MilitaryTeam.Red);
    }

    private void HandleCiviliansButtonClick()
    {
        TeamsManager.instance.ReinforceCivilians(CiviliansPerReinforcement);
    }
}
