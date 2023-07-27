using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CapturePointManager : MonoSingleton<CapturePointManager> 
{
    List<CapturePoint> _capturePoints = new List<CapturePoint>();

    public ReadOnlyCollection<CapturePoint> CapturePoints { get; private set; }

    protected override void SingletonAwake()
    {
        CapturePoints = _capturePoints.AsReadOnly();

    }

    public void Add(CapturePoint capturePoint) 
    {
        if (!_capturePoints.Contains(capturePoint))
            _capturePoints.Add(capturePoint);
    }

    public void Remove(CapturePoint capturePoint)
    {
        if (_capturePoints.Contains(capturePoint))
            _capturePoints.Remove(capturePoint);
    }

    public MilitaryTeam WhosWinning()
    {
        int blue = 0;
        int red = 0;

        foreach (var item in _capturePoints)
        {
            switch (item.CapturedBy)
            {
                case MilitaryTeam.Blue:
                    blue++;
                    break;
                case MilitaryTeam.Red:
                    red++;
                    break;
            }
        }

        if (blue > red)
            return MilitaryTeam.Blue;

        if (red > blue)
            return MilitaryTeam.Red;

        return MilitaryTeam.None;
    }   
}
