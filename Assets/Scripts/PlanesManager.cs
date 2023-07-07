using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlanesManager : MonoSingleton<PlanesManager> 
{
    public float maxHeight;
    public float DistanceX;
    public float maxDistanceZ;
    public float Scale;

    public float maxUpwardsAngleOnTakeOff;

    public LayerMask ground;

    public bool inOptimalHeight(Vector3 Position)
    {
        return Position.y > maxHeight;
       
    }
   
    protected override void SingletonAwake()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public bool InCombatZone(Plane plane) { return true; }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        //HeightLimit();
        //CombatZoneLimitsX();
        //CombatZoneLimitsZ();
    }

    void HeightLimit()
    {
        for (int i = 0; i < 50; i++)
        {
            for (int j = 0; j < 50; j++)
            {

                Vector3 gridScaled = new Vector3(i * Scale, maxHeight, j * Scale);
                Gizmos.DrawLine(new Vector3(i, maxHeight, j) + gridScaled, new Vector3(i + Scale + 10, maxHeight, j) + gridScaled);
                Gizmos.DrawLine(new Vector3(i, maxHeight, j) + gridScaled, new Vector3(i, maxHeight, j + Scale + 10) + gridScaled);

            }
        }
    }




    void CombatZoneLimitsX()
    {
        Vector3 pos = transform.position;
        for (int i = 0; i < DistanceX; i++)
        {
            for (int j = 0; j < maxHeight; j++)
            {
               
                Gizmos.DrawLine(new Vector3(i,j,maxDistanceZ), new Vector3(i+1,j, maxDistanceZ));
                Gizmos.DrawLine( new Vector3(i, j, maxDistanceZ),  new Vector3(i , j+1 , maxDistanceZ));
            }
        }

        for (int i = 0; i < DistanceX; i++)
        {
            for (int j = 0; j < maxHeight; j++)
            {

                Gizmos.DrawLine(new Vector3(-i, j,-maxDistanceZ ), new Vector3(-i - 1, j, -maxDistanceZ));
                Gizmos.DrawLine( new Vector3(-i, j, -maxDistanceZ), new Vector3(-i, j + 1, -maxDistanceZ));
            }
        }

    }
    void CombatZoneLimitsZ()
    {
        Vector3 pos = transform.position;
        for (int z = 0; z < maxDistanceZ; z++)
        {
            for (int y = 0; y < maxHeight; y++)
            {

                Gizmos.DrawLine(new Vector3(DistanceX, y, z), new Vector3(DistanceX, y, z + 1));
                Gizmos.DrawLine(new Vector3(DistanceX, y, z), new Vector3(DistanceX, y + 1, z));
            }
        }

        for (int z = 0; z < maxDistanceZ; z++)
        {
            for (int i = 0; i < maxHeight; i++)
            {

                Gizmos.DrawLine(new Vector3(-DistanceX, i, -z), new Vector3(-DistanceX - 1, i,-z));
                Gizmos.DrawLine(new Vector3(-DistanceX, i,-z), new Vector3(-DistanceX, i + 1, -z));
            }
        }

    }


}
