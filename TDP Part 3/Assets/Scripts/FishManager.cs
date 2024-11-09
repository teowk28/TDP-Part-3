using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FishManager : MonoBehaviour
{
    enum EFishState
    { 
        Idle,           //patrol state
        Lured,          //optimized and removed baited, go directly to lured
        BaitedFail,
        HookedFail,     //funnel for escape checking
        Hooked,         //hooked and resist implementation
        Reeled          //success state, disappear or smth
    };

    [SerializeField] GameObject[]   wayPoints;
    [SerializeField] GameObject[]   spawnPoints;
    [SerializeField] GameObject[]   escapePoints;
    [SerializeField] Fish[]         fishPool;       //object pull fishes
    [SerializeField] public GameObject player;

    int                             fishinIndex;    //index of fish currently fishing
    public bool                     isCurrentlyFishing;

    public static FishManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        { Destroy(this); }
        instance = this;
    }

    static FishManager GetInstance() 
    {
        return instance;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fishinIndex = 1;
        isCurrentlyFishing = false;
        foreach (Fish fish in fishPool) 
        {
            fish.Reset();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            if (!isCurrentlyFishing)
                CastLine();
            else
            {
                GetFishFishing().myHook.Reel(1.0f);
            }
        }
        else { GetFishFishing().myHook.Reel(-1.0f); }
    }

    public void CastLine() 
    {
        if (isCurrentlyFishing) { isCurrentlyFishing = false;return; }
        fishinIndex = Random.Range(1, fishPool.Length);
        Debug.Log(fishPool.Length);
        fishPool[fishinIndex-1].myHook.StartFishing(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        isCurrentlyFishing = true;
    }

    public Vector3 GetGoalWaypoint()
    {
        return GetGoalWaypoint(Vector3.zero);
    }
    public Vector3 GetGoalWaypoint(Vector3 _prev) 
    {
        Vector3 retval;
        retval = _prev;
        while (retval == _prev) 
        {
            int rand = (int)Mathf.Floor(Random.Range(1, wayPoints.Length));
            retval = wayPoints[rand-1].transform.position;
        }
        return retval;
    }
    
    public Vector3 GetEscapeWaypoint() 
    {
        Vector3 retval;
        int rand = (int)Mathf.Floor(Random.Range(1, escapePoints.Length));
        retval = escapePoints[rand-1].transform.position;
        return retval;
    }
    
    public Vector3 GetSpawnWaypoint() 
    {
        Vector3 retval;
        int rand = (int)Mathf.Floor(Random.Range(1, spawnPoints.Length));
        retval = spawnPoints[rand-1].transform.position;
        return retval;
    }

    public Fish GetFishFishing() 
    {
        return fishPool[fishinIndex - 1];
    }
}
