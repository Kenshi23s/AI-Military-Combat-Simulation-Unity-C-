using System.Collections.Generic;
using UnityEngine;
using FacundoColomboMethods;
using System.Linq;
using System;

[RequireComponent(typeof(DebugableObject))]
public class Node : MonoBehaviour
{


    public List<Node> Neighbors = new List<Node>();
    [SerializeField] public int cost = 0;

    public Vector3 groundPosition;

    public void AddCost(int value) =>  cost += value * 1; 
    public void SubstractCost(int value) => cost = Mathf.Clamp(cost-(value * 1),0,int.MaxValue); 

   
    public void IntializeNode()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, 5f, AI_Manager.instance.wall_Mask))
            groundPosition = hitInfo.point;
        else
            groundPosition = transform.position;
        
   
        LayerMask wallMask = AI_Manager.instance.wall_Mask;

        Neighbors = AI_Manager.instance.nodes.GetWhichAreOnSight(transform.position, wallMask, RaycastType.Sphere, 1f)
                    .Where(x=> x!=this).Where(x=>Vector3.Distance(x.transform.position,transform.position)<25f).ToList();

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
        
    }

}
