using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CapturePointManager : MonoSingleton<CapturePointManager> 
{
    Dictionary<Team,List<CapturePoint>> _capturePoints = new Dictionary<Team, List<CapturePoint>>();

    public ReadOnlyDictionary<Team, List<CapturePoint>> CapturePoints;

    protected override void SingletonAwake()
    {
        foreach (Team key in Enum.GetValues(typeof(Team))) _capturePoints.Add(key, new List<CapturePoint>());

        CapturePoints =  new ReadOnlyDictionary<Team,List<CapturePoint>>(_capturePoints);
    }
    public void AddZone(CapturePoint newZone)
    {
        foreach (Team key in _capturePoints.Keys)        
            if (_capturePoints[key].Contains(newZone))          
                return;

        _capturePoints[newZone.takenBy].Add(newZone);
        newZone.onPointOwnerChange.AddListener(() => ChangeKeyInDictionary(newZone));
    }

    public void ChangeKeyInDictionary(CapturePoint point)
    {
        foreach (Team key in _capturePoints.Keys.Where(x => point.takenBy != x))       
          if (_capturePoints[key].Contains(point))
          {
              _capturePoints[key].Remove(point);
              break;
          }

        _capturePoints[point.takenBy].Add(point);
        
    }

    public Team WhosWinning()
    {      
        if (_capturePoints[Team.Red].Count > _capturePoints[Team.Blue].Count)  return Team.Red;

        if (_capturePoints[Team.Red].Count < _capturePoints[Team.Blue].Count)  return Team.Blue;

        return Team.None;
    }   
}
