using FacundoColomboMethods;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using static Infantry;

public class Fireteam : MonoBehaviour
{
    List<Infantry> _fireteamMembers = new List<Infantry>();
    public ReadOnlyCollection<Infantry> fireteamMembers;
    public Infantry Leader { get; private set; }

    public Vector3 newPos;
    #region MemberManagment
    public void AddMember(Infantry infantry)
    {
        if (!_fireteamMembers.Contains(infantry))        
            _fireteamMembers.Add(infantry);          
        
    }

    public void AddMember(IEnumerable<Infantry> infantry)
    {
        foreach (var item in infantry.Where(x => !_fireteamMembers.Contains(x)))      
            _fireteamMembers.Add(item);           
    }

    public void RemoveMember(Infantry infantry)
    {
        if (_fireteamMembers.Contains(infantry))        
            _fireteamMembers.Remove(infantry);       
    }

    public void RemoveMember(IEnumerable<Infantry> infantry)
    {
        foreach (var item in infantry.Where(x => _fireteamMembers.Contains(x)))       
            _fireteamMembers.Remove(item);
    }
    #endregion

    private void Start()
    {
        if (Leader==null)
        {
            Leader = _fireteamMembers.PickRandom();
            fireteamMembers = _fireteamMembers.AsReadOnly();
        }
    }



    public void SendOrders()
    {
        //lookup divide una coleccion en base a un predicado, no solo se puede dividir en booleanas
        //tambien se puede dividir en strings o enums
        var split = _fireteamMembers.ToLookup(x => x == Leader);

        split[true].First().MoveTowardsTransition();

        foreach (var members in split[false]) members.FollowLeaderTransition();
    }

    public IEnumerable<Mechanic> GetMechanics() => _fireteamMembers.OfType<Mechanic>();

    public IEnumerable<Medic> GetMedics() => _fireteamMembers.OfType<Medic>();
    

}
