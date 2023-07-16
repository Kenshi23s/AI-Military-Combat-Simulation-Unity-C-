using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CapturePointManager : MonoSingleton<CapturePointManager> 
{
    Dictionary<MilitaryTeam, List<CapturePoint>> _capturePoints = new Dictionary<MilitaryTeam, List<CapturePoint>>();

    public ReadOnlyDictionary<MilitaryTeam, List<CapturePoint>> CapturePoints;

    protected override void SingletonAwake()
    {
        foreach (MilitaryTeam key in Enum.GetValues(typeof(MilitaryTeam))) _capturePoints.Add(key, new List<CapturePoint>());

        CapturePoints =  new ReadOnlyDictionary<MilitaryTeam, List<CapturePoint>>(_capturePoints);
    }
    public void AddZone(CapturePoint newZone)
    {
        foreach (MilitaryTeam key in _capturePoints.Keys)        
            if (_capturePoints[key].Contains(newZone))          
                return;

        _capturePoints[newZone.takenBy].Add(newZone);
        newZone.onPointOwnerChange.AddListener(() => ChangeKeyInDictionary(newZone));
    }

    public void ChangeKeyInDictionary(CapturePoint point)
    {
        foreach (MilitaryTeam key in _capturePoints.Keys.Where(x => point.takenBy != x))       
          if (_capturePoints[key].Contains(point))
          {
              _capturePoints[key].Remove(point);
              break;
          }

        _capturePoints[point.takenBy].Add(point);
        
    }

    public MilitaryTeam WhosWinning()
    {      
        if (_capturePoints[MilitaryTeam.Red].Count > _capturePoints[MilitaryTeam.Blue].Count)  return MilitaryTeam.Red;

        if (_capturePoints[MilitaryTeam.Red].Count < _capturePoints[MilitaryTeam.Blue].Count)  return MilitaryTeam.Blue;

        return MilitaryTeam.None;
    }   
}
