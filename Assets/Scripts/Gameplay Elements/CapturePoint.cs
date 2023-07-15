using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(DebugableObject))]
public class CapturePoint : MonoBehaviour
{
    public enum ZoneStates
    {
        Disputed,
        Neutral,
        BeingTaken,
        Taken
    }

    [SerializeField, Min(1)] float _waitingFramesTilSearch = 30;
    [SerializeField, Min(0)] float _zoneRadius = 15;
    DebugableObject _debug;

    #region Events
    public UnityEvent onPointOwnerChange;

    public UnityEvent onProgressChange;

    public UnityEvent<ILookup<Team,Entity>> onEntitiesAroundUpdate;

    public event Action<Team> onCaptureComplete;
    #endregion

    public float TakeProgress { get; private set; }


    [SerializeField]
    SpatialGrid3D _targetGrid;

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

            if (captureProgress > 0)         
                return Mathf.Abs(captureProgress) / (ProgressRequiredForCapture) + 0.5f;          
            else           
                return Mathf.Abs(captureProgress) / (ProgressRequiredForCapture) - 0.5f;    
        }
    }
        
  
    public ZoneStates CurrentCaptureState { get; private set; }
    
    public Team takenBy = Team.None;
    public Team beingTakenBy { get; private set; }

    List<Entity> _combatEntitiesAround = new List<Entity>();

   
    void Awake()
    {
        _debug = GetComponent<DebugableObject>();
        _debug.AddGizmoAction(DrawRadius);
        captureProgress = 0;
    }


    private void Start()
    {
        _targetGrid = FindObjectOfType<SpatialGrid3D>();
        CapturePointManager.instance.AddZone(this);
        StartCoroutine(SearchEntitiesAround());
    }

  
    IEnumerator SearchEntitiesAround()
    {
        while (true) 
        {
            for (int i = 0; i < _waitingFramesTilSearch; i++) yield return null;


            _combatEntitiesAround = SphereQuery()
                .OfType<Entity>()
                .Where(x => x.MyTeam != Team.None)
                .ToList();

          

            var combatants = _combatEntitiesAround
                .Where(x => x.MyTeam != takenBy);
           
            //divido la lista entre equipo rojo y verde con el lookup
            //si pasan el predicado, accedo a esos items con [true] y si no
            //accedo a los otros items con [false]
            var split = _combatEntitiesAround
                .Where(x => x.MyTeam != Team.None)
                .ToLookup(x => x.MyTeam);


            
            onEntitiesAroundUpdate?.Invoke(split);

            if (!split[Team.Red].Any() && !split[Team.Blue].Any()) 
            {
                _debug.Log("No hay unidades en el area");
                continue;
            }
          

            //rojo                    
            if (split[Team.Red].Any() && split[Team.Blue].Any())
            {
                CurrentCaptureState = ZoneStates.Disputed;
                _debug.Log("Esta en disputa, hay unidades de ambos equipos");
                continue;
            }
            string debug = "Esta siendo tomada por el equipo";
            if (split[Team.Red].Any())
            {
                debug += " rojo";
                CaptureProgress += Time.deltaTime * split[Team.Red].Count() * _waitingFramesTilSearch;
                beingTakenBy = Team.Red;
            }                    
            else
            {
                debug += " azul";
                CaptureProgress -= Time.deltaTime * split[Team.Blue].Count() * _waitingFramesTilSearch;
                beingTakenBy = Team.Blue;
            }
            debug += $" el progreso es de {captureProgress}";
            _debug.Log(debug);

            onProgressChange?.Invoke();
            CheckCaptureProgress();
        }    
    }

    void CheckCaptureProgress()
    {
        var aux = beingTakenBy;


        switch (beingTakenBy)
        {
            case Team.Red:
                if (captureProgress >= ProgressRequiredForCapture && takenBy != Team.Red )
                {
                    takenBy = Team.Red;
                    onCaptureComplete?.Invoke(takenBy);
                    onCaptureComplete = delegate { };
                }
                    
                break;
            case Team.Blue:
                if (captureProgress <= -ProgressRequiredForCapture && takenBy != Team.Blue)
                {
                    takenBy = Team.Blue;
                    onCaptureComplete?.Invoke(takenBy);
                    onCaptureComplete = delegate { };
                }
                    
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
        Gizmos.DrawWireSphere(transform.position, _zoneRadius);
    }

    IEnumerable<GridEntity> SphereQuery() 
    {
        //creo una "caja" con las dimensiones deseadas, y luego filtro segun distancia para formar el círculo
        return _targetGrid.Query(
            transform.position + new Vector3(-_zoneRadius, -_zoneRadius, -_zoneRadius),
            transform.position + new Vector3(_zoneRadius, _zoneRadius, _zoneRadius),
            pos => {
                var distance = pos - transform.position;
                return distance.sqrMagnitude < _zoneRadius * _zoneRadius;
            });
    }
}
