using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Leaderboard of the alive units with the most damage dealt.
/// Tab shows and hides it. The list refreshes every refreshTime seconds.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    [SerializeField] private int leaderboardQuantity;
    [SerializeField] private GameObject _panel,_scoreFatherGO;
    [SerializeField] private UnitScore prefabUnitScore;
    private List<UnitScore> _scoreList = new List<UnitScore>();
    private Action ActualState = delegate { };

    private const float refreshTime = 0.5f;

    // Start is called before the first frame update
    private void Start() => ActualState = CreateScores;

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))      
            ActualState();    
    }

    private void CreateScores()
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

    private IEnumerator UpdateButtons()
    {

        WaitForSeconds wait = new WaitForSeconds(refreshTime);
        //IA2-LINQ
        while (_scoreList.Any())
        {

            //obtengo todas las unidades de combate vivas
            var col = TeamsManager.instance._teams
           .SelectMany(x => x.Value)
           .Where(x => x.IsAlive)
           .OfType<IMilitary>()
           .OrderByDescending(x => x.TotalDamageDealt)
           .Take(leaderboardQuantity)
           .ToList();
             
            for (int i = 0; i < col.Count; i++) _scoreList[i].SetOwner(col[i]);

            yield return wait;
        }
       
    }

    private void RemoveScores()
    {
        StopCoroutine(UpdateButtons());

        for (int i = 0; i < _scoreList.Count; i++) Destroy(_scoreList[i].gameObject);


        _scoreFatherGO.SetActive(false); _scoreList.Clear(); 

        ActualState = CreateScores;
    }
}
