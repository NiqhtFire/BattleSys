using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class GameManager : Manager<GameManager>
{
    public EntitiesDatabaseSO entitiesDatabase;

    public Transform team1Parent;
    public Transform team2Parent;

    public Action OnRoundStart;
    public Action<Team> OnRoundEnd;
    public Action<BaseEntity> OnUnitDied;
    public Action<int, int> OnLevelChanged;

    List<BaseEntity> team1Entities = new List<BaseEntity>();
    public List<BaseEntity> team2Entities = new List<BaseEntity>();

    int unitsPerTeam = 6;

    private bool isGameStarted;
    public bool IsGameStarted => isGameStarted;

    private void Start()
    {
        ChangeLevel();
    }

    private void InstantiateEnemies()
    {
        for (int i = 0; i < unitsPerTeam; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, entitiesDatabase.allEntities.Count);
            BaseEntity newEntity = Instantiate(entitiesDatabase.allEntities[randomIndex].prefab, team2Parent);

            team2Entities.Add(newEntity);

            newEntity.Setup(Team.Team2, GridManager.Instance.GetFreeNode(Team.Team2));
        }
    }

    public void OnEntityBought(EntitiesDatabaseSO.EntityData entityData)
    {
        BaseEntity newEntity = Instantiate(entityData.prefab, team1Parent);
        newEntity.gameObject.name = entityData.name;
        team1Entities.Add(newEntity);

        newEntity.Setup(Team.Team1, GridManager.Instance.GetFreeNode(Team.Team1));
    }

    public List<BaseEntity> GetEntitiesAgainst(Team against)
    {
        if (against == Team.Team1)
            return team2Entities;
        else
            return team1Entities;
    }

    public void UnitDead(BaseEntity entity)
    {
        team1Entities.Remove(entity);
        team2Entities.Remove(entity);

        OnUnitDied?.Invoke(entity);

        Destroy(entity.gameObject);

        if(team2Entities.Count <= 0)
        {
            print("You Win!");
            OnRoundEndF(Team.Team1);
        }
        else if(team1Entities.Count <= 0)
        {
            print("Enemy Win! :(");
            OnRoundEndF(Team.Team2);
        }

    }

    private void OnRoundEndF(Team winnerTeam){
        if(winnerTeam == Team.Team1){
            GridManager.Instance.Graphs[GridManager.Instance.CurrentGraph].ClearOccopied();
        }else{
            //ENEMY WIN
        }
        OnRoundEnd?.Invoke(winnerTeam);
        isGameStarted = false;
    }
    
    #region temp
    
    private int levelCount = 2;

    public void ChangeLevel(){
        if(isGameStarted)
            return;
        var prelevel = GridManager.Instance.CurrentGraph;
        GridManager.Instance.Graphs[GridManager.Instance.CurrentGraph].ClearOccopied();            
        var currlvl = (GridManager.Instance.CurrentGraph + 1) % levelCount;
        while(team2Entities.Count > 0){
            var a = team2Entities[0];
            team2Entities.RemoveAt(0);
            a.RemoveListeners();
            Destroy(a.gameObject);
        }
        GridManager.Instance.CurrentGraph = currlvl;
        GridManager.Instance.Graphs[GridManager.Instance.CurrentGraph].ClearOccopied();            
        Camera.main.transform.position = new Vector3(currlvl * 7.5f, 1.9f, -10);
        OnLevelChanged?.Invoke(prelevel, currlvl);
        InstantiateEnemies();
    }

    #endregion

    public void DebugFight()
    {
        OnRoundStart?.Invoke();
        isGameStarted = true;
    }
}

public enum Team
{
    Team1,
    Team2
}
