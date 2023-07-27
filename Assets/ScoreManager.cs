using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{

    [SerializeField] int leaderboardQuantity;
    [SerializeField] GameObject _panel,_scoreFatherGO;
    [SerializeField] UnitScore prefabUnitScore;
    List<UnitScore> _scoreList = new List<UnitScore>();

    Action ActualState = delegate { };
    

    // Start is called before the first frame update
    void Start() => ActualState = CreateScores;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))      
            ActualState();    
    }

    void CreateScores()
    {
        _scoreFatherGO.SetActive(true);
        for (int i = 0; i < leaderboardQuantity; i++)
        {
            var unitScore = Instantiate(prefabUnitScore, _panel.transform);
            _scoreList.Add(unitScore);
        }    
        StartCoroutine(UpdateButtons());
        ActualState = RemoveScores;
    }

    IEnumerator UpdateButtons()
    {
        //IA2-LINQ
        while (_scoreList.Any())
        {
            var col = TeamsManager.instance._teams
           .SelectMany(x => x.Value)
           .Where(x => x.IsAlive)
           .OfType<IMilitary>()
           .OrderByDescending(x => x.TotalDamageDealt)
           .Take(leaderboardQuantity)
           .ToList();
             
            for (int i = 0; i < col.Count; i++) _scoreList[i].SetOwner(col[i]);

            yield return null;
        }
       
    }



    void RemoveScores()
    {
        StopCoroutine(UpdateButtons());

        for (int i = 0; i < _scoreList.Count; i++) Destroy(_scoreList[i].gameObject);


        _scoreFatherGO.SetActive(false); _scoreList.Clear(); 

        ActualState = CreateScores;
    }

}
