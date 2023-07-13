using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(DebugableObject))]
public class CapturePoint : GridEntity
{
    [SerializeField] float waitingFramesTilSearch=30, zoneRadius = 15;

    public UnityEvent onPointOwnerChange;
    public float TakeProgress { get; private set; }
    public enum ZoneStates
    {
        Disputed,
        Neutral,       
        BeingTaken,
        Taken
    }

    public float ProgressRequiredForCapture { get; private set; }
    float captureProgress = 0;

    public float CaptureProgress 
    { 
        get => captureProgress; 
        private set 
        { 
            captureProgress = Mathf.Clamp(value, -ProgressRequiredForCapture, ProgressRequiredForCapture);
        }
    }

    public ZoneStates CurrentCaptureState { get; private set; }
    
    public Team takenBy = Team.None;
    public Team beingTakenBy { get; private set; }

    List<Entity> CombatEntitiesAround = new List<Entity>();

    private void Awake()
    {
        _debug = GetComponent<DebugableObject>(); _debug.AddGizmoAction(DrawRadius);

    }


    private void Start()
    {
        CapturePointManager.instance.AddZone(this);
        StartCoroutine(SearchEntitiesAround());
    }

  
    IEnumerator SearchEntitiesAround()
    {
        while (true) 
        {
            for (int i = 0; i < waitingFramesTilSearch; i++) yield return null;


            CombatEntitiesAround = GetEntitiesInRange(zoneRadius)
                .Where(x => x != this)
                .NotOfType<Entity,Civilian>()
                .NotOfType<Entity,Plane>()
                .ToList();

            //divido la lista entre equipo rojo y verde con el lookup
            //si pasan el predicado, accedo a esos items con [true] y si no
            //accedo a los otros items con [false]
            var split = CombatEntitiesAround
                .Where(x => x.MyTeam != Team.None)
                .ToLookup(x => x.MyTeam);

            if (!split[Team.Red].Any() || !split[Team.Blue].Any()) continue;

            //rojo                    
            if (split[Team.Red].Any() && split[Team.Blue].Any())
            {
                CurrentCaptureState = ZoneStates.Disputed;
                continue;
            }
    
            if (split[Team.Red].Any())
            {
                CaptureProgress += Time.deltaTime * split[Team.Red].Count();
                beingTakenBy = Team.Red;
            }                    
            else
            {
                CaptureProgress -= Time.deltaTime * split[Team.Blue].Count();
                beingTakenBy = Team.Blue;
            }               
            CheckCaptureProgress();
        }    
    }

    void CheckCaptureProgress()
    {
        var aux = takenBy;
        switch (beingTakenBy)
        {
            case Team.Red:
                if (captureProgress >= ProgressRequiredForCapture)
                    takenBy = Team.Red;
                break;
            case Team.Blue:
                if (captureProgress <= -ProgressRequiredForCapture)
                    takenBy = Team.Blue;
                break;
            default:
                takenBy = Team.None;
                break;
        }

        if (takenBy != aux) onPointOwnerChange?.Invoke();
    }

    private void DrawRadius()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, zoneRadius);
    }
}
