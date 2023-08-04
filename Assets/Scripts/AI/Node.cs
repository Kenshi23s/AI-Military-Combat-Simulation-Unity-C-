using System.Collections.Generic;
using UnityEngine;
using FacundoColomboMethods;
using System.Linq;
using System;

[RequireComponent(typeof(DebugableObject))]
public class Node : MonoBehaviour
{


    [NonSerialized]public List<Node> Neighbors = new List<Node>();
    [SerializeField] public int cost = 0;

    public Vector3 groundPosition;

    public void AddCost(int value) =>  cost += value * 1; 
    public void SubstractCost(int value) => cost = Mathf.Clamp(cost-(value * 1),0,int.MaxValue); 

   
    public void IntializeNode()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, Mathf.Infinity, AI_Manager.instance.GroundMask))
            groundPosition = hitInfo.point;
        else
            groundPosition = transform.position;
        
   
        LayerMask wallMask = AI_Manager.instance.WallMask;
        AI_Manager I = AI_Manager.instance;

        Neighbors = I.nodes.GetWhichAreOnSight(transform.position, wallMask, RaycastType.Sphere, 1f)
                    .Where(x => x != this)
                    .Where(x => Vector3.Distance(x.transform.position,transform.position) < I.MaxDistanceBetweenNodes)
                    .ToList();

        GetComponent<DebugableObject>().AddGizmoAction(NodeGizmo);
        GetComponent<MeshRenderer>().enabled = false;
    }

    private void NodeGizmo()
    {
       if (Neighbors.Count < 0) return;

       List<Node> nodes2 = Neighbors.Aggregate(new List<Node>(), (x, y) =>
       {
           if (y.Neighbors.Contains(this)) x.Add(y);
           return x;
       });
       foreach (Node node in nodes2)
       {      
         Gizmos.color = Color.blue;
         Gizmos.DrawLine(node.transform.position, transform.position);       
       }
        //Gizmos.DrawWireSphere(transform.position, AgentsManager.instance.nodeInteractradius);
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundPosition,1f);
    }

}
