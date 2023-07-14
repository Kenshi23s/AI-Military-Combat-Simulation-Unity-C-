using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(DebugableObject))]
public class CapturePoint : GridEntity
{
    public enum ZoneStates
    {
        Disputed,
        Neutral,
        BeingTaken,
        Taken
    }

    [SerializeField] float waitingFramesTilSearch = 30, zoneRadius = 15;

    #region Events
    public UnityEvent onPointOwnerChange;

    public UnityEvent onProgressChange;

    //ya se q es rarisimo esto jocha no me asesines :C
    public UnityEvent<ILookup<Team,Entity>> onEntitiesAroundUpdate;
    #endregion

    public float TakeProgress { get; private set; }
   

    [field: SerializeField] public float ProgressRequiredForCapture { get; private set; }
    float captureProgress = 0;

    public float CaptureProgress
    {
        get => captureProgress;
        private set
        {
            captureProgress = Mathf.Clamp(value, -ProgressRequiredForCapture, ProgressRequiredForCapture);
        }
    }

    public float ZoneProgressNormalized 
    {
        get
        {
            if (captureProgress == 0) return 0.5f;

            return Mathf.Abs(captureProgress) / (ProgressRequiredForCapture * 2);
        }
    }
        
  
    public ZoneStates CurrentCaptureState { get; private set; }
    
    public Team takenBy = Team.None;
    public Team beingTakenBy { get; private set; }

    List<Entity> CombatEntitiesAround = new List<Entity>();

   
    protected override void EntityAwake()
    {
        DebugEntity.AddGizmoAction(DrawRadius);
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
                .Where(x => x != this).OfType<Entity>()
                .ToList();
            DebugEntity.Log(CombatEntitiesAround.Count.ToString());
            //divido la lista entre equipo rojo y verde con el lookup
            //si pasan el predicado, accedo a esos items con [true] y si no
            //accedo a los otros items con [false]
            var split = CombatEntitiesAround
                .Where(x => x.MyTeam != Team.None)
                .ToLookup(x => x.MyTeam);


            
            onEntitiesAroundUpdate?.Invoke(split);

            if (!split[Team.Red].Any() || !split[Team.Blue].Any()) 
            {
                DebugEntity.Log("No hay unidades en el area");
                continue;
            }
          

            //rojo                    
            if (split[Team.Red].Any() && split[Team.Blue].Any())
            {
                CurrentCaptureState = ZoneStates.Disputed;
                DebugEntity.Log("Esta en disputa, hay unidades de ambos equipos");
                continue;
            }
            string debug = "Esta siendo tomada por";
            if (split[Team.Red].Any())
            {
                debug += "El equipo rojo";
                CaptureProgress += Time.deltaTime * split[Team.Red].Count();
                beingTakenBy = Team.Red;
            }                    
            else
            {
                debug += "El equipo azul";
                CaptureProgress -= Time.deltaTime * split[Team.Blue].Count();
                beingTakenBy = Team.Blue;
            }
            DebugEntity.Log(debug);

            onProgressChange?.Invoke();
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
