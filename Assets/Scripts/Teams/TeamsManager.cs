using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Team
{
    Red,
    Blue,
    None
}
public interface InitializeUnit
{
    void InitializeUnit(Team newTeam);
}

public class TeamsManager : MonoSingleton<TeamsManager>
{



    #region TeamsDictionary
    Dictionary<Team, List<Entity>> _teams = new Dictionary<Team, List<Entity>>();




    #region MemberAdd

    public void AddToTeam(Team key,Entity value)
    {
        if (!_teams[key].Contains(value))
        {
            _teams[key].Add(value);
        }
       
    }

    public void AddToTeam(Team key, IEnumerable<Entity> values)
    {
        foreach (var item in values.Where(x=> !_teams[key].Contains(x)))
        {            
            _teams[key].Add(item);           
        }
       

    }

    public void RemoveFromTeam(Team key, Entity value)
    {
        if (_teams[key].Contains(value))
        {
            _teams[key].Remove(value);
        }
    }

    public void RemoveFromTeam(Team key, IEnumerable<Entity> values)
    {
        foreach (var item in values.Where(x => _teams[key].Contains(x)))
        {
            _teams[key].Remove(item);
        }
    }
    #endregion
    #endregion

    protected override void SingletonAwake()
    {
        //inicializo las listas del diccionario
        foreach (Team item in Enum.GetValues(typeof(Team)))
        {
            _teams.Add(item, new List<Entity>());
        }
        
    }

    public IEnumerable<Fireteam> GetAllyFireteams(Team team)
    {
        return _teams[team].OfType<Infantry>().Select(x => x.myFireteam).Where(x => x !=null).Distinct();
           
    }

    //public IEnumerable<Infantry> GetAllTanks(Team team)
    //{
    //    return _teams[team].OfType<Infantry>().Select(x => x.myFireteam).Distinct();

    //}

    public IEnumerable<Plane> GetTeamPlanes(Team team)
    {
        return _teams[team]
            .OfType<Plane>()
            .Where(x => x.actualState != PlaneStates.ABANDONED);
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
