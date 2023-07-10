using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class SpatialGrid3D : MonoBehaviour
{
    #region Variables

    [SerializeField] bool takePositionFromTransform = false;
    //puntos de inicio de la grilla
    public float x, y, z;
    // las dimensiones de las celdas
    [Min(0)]
    public float cellWidth, cellHeight, cellDepth;
    // la cantidad de celdas
    [Min(0)]
    public int width, height, depth;

    //ultimas posiciones conocidas de los elementos, guardadas para comparación.
    private Dictionary<GridEntity, Tuple<int, int, int>> lastPositions;
    //los "contenedores"
    private HashSet<GridEntity>[,,] buckets;

    //el valor de posicion que tienen los elementos cuando no estan en la zona de la grilla.
    /*
     Const es implicitamente statica
     const tengo que ponerle el valor apenas la declaro, readonly puedo hacerlo en el constructor.
     Const solo sirve para tipos de dato primitivos.
     */
    readonly public Tuple<int, int, int> Outside = Tuple.Create(-1, -1, -1);

    //Una colección vacía a devolver en las queries si no hay nada que devolver
    readonly public GridEntity[] Empty = new GridEntity[0];
    #endregion

    #region FUNCIONES

    private void OnValidate() 
    {
        runInEditMode = takePositionFromTransform;

        CalculateGridCenter();
    }

    void SetGridPosition()
    {
        x = transform.position.x;
        y = transform.position.y;
        z = transform.position.z;

        CalculateGridCenter();
    }

    void CalculateGridCenter() 
    {
        Vector3 dimensions = new Vector3(width * cellWidth, height * cellHeight, depth * cellDepth);
        Vector3 center = new Vector3(x, y, z) + dimensions / 2;
        _gridCenter = center;
    }

    private void Awake()
    {
        CalculateGridCenter();

        lastPositions = new Dictionary<GridEntity, Tuple<int, int, int>>();
        buckets = new HashSet<GridEntity>[width, height, depth];
        
        //creamos todos los hashsets
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                for (int k = 0; k < depth; k++)
                {
                    buckets[i, j, k] = new HashSet<GridEntity>();
                }

        //P/alumnos: por que no usamos OfType<>() despues del RecursiveWalker() aca?
        //var ents = RecursiveWalker(transform)
        //    .Select(x => x.GetComponent<GridEntity>())
        //    .Where(x => x != null);

        var ents = FindObjectsOfType<GridEntity>(false);

        foreach (var e in ents)
        {
            AddEntity(e);
        }
    }

    public void AddEntity(GridEntity entity)
    {
        entity.SetSpatialGrid(this);
        entity.OnMove += UpdateEntity;

        UpdateEntity(entity);
    }

    public void RemoveEntity(GridEntity entity)
    {
        
        entity.SetSpatialGrid(null);
        entity.OnMove -= UpdateEntity;   
    }

    public void Update() 
    {
        if(!Application.IsPlaying(gameObject) && takePositionFromTransform && transform.hasChanged) 
        {
            SetGridPosition();
        }    
    }

    public void UpdateEntity(GridEntity entity)
    {
        var lastPos = lastPositions.ContainsKey(entity) ? lastPositions[entity] : Outside;
        var currentPos = GetPositionInGrid(entity.gameObject.transform.position);

        //Misma posición, no necesito hacer nada
        if (lastPos.Equals(currentPos))
            return;

        //Lo "sacamos" de la posición anterior
        if (IsInsideGrid(lastPos))
            buckets[lastPos.Item1, lastPos.Item2, lastPos.Item3].Remove(entity);

        //Lo "metemos" a la celda nueva, o lo sacamos si salio de la grilla
        if (IsInsideGrid(currentPos))
        {
            buckets[currentPos.Item1, currentPos.Item2, currentPos.Item3].Add(entity);
            lastPositions[entity] = currentPos;
          
            entity.onGrid = true;
        }
        else
        {
          
            lastPositions.Remove(entity);
            entity.onGrid = false;
        }
    }

    public IEnumerable<GridEntity> Query(Vector3 aabbFrom, Vector3 aabbTo, Func<Vector3, bool> filterByPosition)
    {
        var from = new Vector3(Mathf.Min(aabbFrom.x, aabbTo.x), Mathf.Min(aabbFrom.y, aabbTo.y), Mathf.Min(aabbFrom.z, aabbTo.z));
        var to = new Vector3(Mathf.Max(aabbFrom.x, aabbTo.x), Mathf.Max(aabbFrom.y, aabbTo.y), Mathf.Max(aabbFrom.z, aabbTo.z));

        var fromCoord = GetPositionInGrid(from);
        var toCoord = GetPositionInGrid(to);

        //¡Ojo que clampea a 0,0 el Outside! TODO: Checkear cuando descartar el query si estan del mismo lado
        fromCoord = Tuple.Create(Utility.Clampi(fromCoord.Item1, 0, width), Utility.Clampi(fromCoord.Item2, 0, height), Utility.Clampi(fromCoord.Item3, 0, depth));
        toCoord = Tuple.Create(Utility.Clampi(toCoord.Item1, 0, width), Utility.Clampi(toCoord.Item2, 0, height), Utility.Clampi(toCoord.Item3, 0, depth));

        if (!IsInsideGrid(fromCoord) && !IsInsideGrid(toCoord))
            return Empty;
        
        // Creamos tuplas de cada celda
        var cols = Generate(fromCoord.Item1, x => x + 1)
            .TakeWhile(x => x < width && x <= toCoord.Item1);

        var rows = Generate(fromCoord.Item2, y => y + 1)
            .TakeWhile(y => y < height && y <= toCoord.Item2);

        var aisles = Generate(fromCoord.Item3, z => z + 1)
            .TakeWhile(z => z < depth && z <= toCoord.Item3);

        var cells = 
        cols.SelectMany(
            col => rows.SelectMany(
                row => aisles.Select(aisle => Tuple.Create(col, row, aisle))
            )
        );

        // Iteramos las que queden dentro del criterio
        return cells
            .SelectMany(cell => buckets[cell.Item1, cell.Item2, cell.Item3])
            .Where(e =>
                from.x <= e.transform.position.x && e.transform.position.x <= to.x &&
                from.y <= e.transform.position.y && e.transform.position.y <= to.y &&
                from.z <= e.transform.position.z && e.transform.position.z <= to.z
            ).Where(e => filterByPosition(e.transform.position));
    }

    public Tuple<int, int, int> GetPositionInGrid(Vector3 pos)
    {
        //quita la diferencia, divide segun las celdas y floorea
        return Tuple.Create(Mathf.FloorToInt((pos.x - x) / cellWidth),
                            Mathf.FloorToInt((pos.y - y) / cellHeight),
                            Mathf.FloorToInt((pos.z - z) / cellDepth));
    }

    public bool IsInsideGrid(Tuple<int, int, int> position)
    {
        //si es menor a 0 o mayor a width o height, no esta dentro de la grilla
        return 0 <= position.Item1 && position.Item1 < width &&
            0 <= position.Item2 && position.Item2 < height && 
            0 <= position.Item3 && position.Item3 < depth;
    }

    void OnDestroy()
    {
        //var ents = RecursiveWalker(transform).Select(x => x.GetComponent<GridEntity>()).Where(e => e != null);

        var ents = FindObjectsOfType<GridEntity>(false);

        foreach (var e in ents)
        {
            e.SetSpatialGrid(null);
            e.OnMove -= UpdateEntity;
        }
    }

    #region GENERATORS
    private static IEnumerable<Transform> RecursiveWalker(Transform parent)
    {
        foreach (Transform child in parent)
        {
            foreach (Transform grandchild in RecursiveWalker(child))
                yield return grandchild;
            yield return child;
        }
    }

    IEnumerable<T> Generate<T>(T seed, Func<T, T> mutate)
    {
        T accum = seed;
        while (true)
        {
            yield return accum;
            accum = mutate(accum);
        }
    }
    #endregion

    #endregion

    Vector3 _gridCenter;
    public Vector3 GetMidleOfGrid()
    {
        return _gridCenter;
    }
    #region GRAPHIC REPRESENTATION
    public bool AreGizmosShutDown;
    public bool activatedGrid;
    public bool showLogs = true;
    private void OnDrawGizmos()
    {


        if (AreGizmosShutDown) return;

        // Dibujamos el centro de la grilla
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_gridCenter, 50f);
        //

        Gizmos.color = Color.white;
        var rows = Generate(y, curr => curr + cellHeight).Take(height + 1)
                .SelectMany(rowY => Generate(z, curr => curr + cellDepth).Take(depth + 1)
                                    .Select(rowZ => Tuple.Create(   new Vector3(x, rowY, rowZ),
                                                                    new Vector3(x + cellWidth * width, rowY, rowZ))));

        var cols = Generate(x, curr => curr + cellWidth).Take(width + 1)
                .SelectMany(colX => Generate(z, curr => curr + cellDepth).Take(depth + 1)
                                    .Select(colZ => Tuple.Create(   new Vector3(colX, y, colZ),
                                                                    new Vector3(colX, y + cellHeight * height, colZ))));

        var aisles = Generate(x, curr => curr + cellWidth).Take(width + 1)
                .SelectMany(aisleX => Generate(y, curr => curr + cellHeight).Take(height + 1)
                                    .Select(aisleY => Tuple.Create( new Vector3(aisleX, aisleY, z),
                                                                    new Vector3(aisleX, aisleY, z + cellDepth * depth))));
                                                                    
        var allLines = rows.Concat(cols).Concat(aisles);
        Gizmos.color = new Color(1, 1, 1, 0.1f);

        foreach (var elem in allLines)
        {
            Gizmos.DrawLine(elem.Item1, elem.Item2);
        }

        if (buckets == null) return;

        var originalCol = GUI.color;
        GUI.color = Color.red;
        if (!activatedGrid)
        {
            IEnumerable<GridEntity> allElems = Enumerable.Empty<GridEntity>();
            foreach(var elem in buckets)
                allElems = allElems.Concat(elem);

            int connections = 0;
            foreach (var ent in allElems)
            {
                foreach(var neighbour in allElems.Where(x => x != ent))
                {
                    Gizmos.DrawLine(ent.transform.position, neighbour.transform.position);
                    connections++;
                }
                if(showLogs)
                    Debug.Log("tengo " + connections + " conexiones por individuo");
                connections = 0;
            }
        }
        else
        {
            int connections = 0;
            foreach (var elem in buckets)
            {
                foreach(var ent in elem)
                {
                    foreach (var n in elem.Where(x => x != ent))
                    {
                        Gizmos.DrawLine(ent.transform.position, n.transform.position);
                        connections++;
                    }
                    if(showLogs)
                        Debug.Log("tengo " + connections + " conexiones por individuo");
                    connections = 0;
                }
            }
        }
        
        GUI.color = originalCol;
        showLogs = false;
    }
    #endregion
}
