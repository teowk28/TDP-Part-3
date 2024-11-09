/*==================================================
 * Script: Fish
 * Description:
 *  Handle State machine behaviour for fishes
 ===================================================*/



using UnityEngine;

public class Fish : MonoBehaviour
{
    
    public enum EFishState
    {
        Idle,           //patrol state
        Lured,          //optimized and removed baited, go directly to lured
        BaitedFail,
        HookedFail,     //funnel for escape checking
        Hooked,         //hooked and resist implementation
        Reeled,          //success state, disappear or smth
        Escape
    };

    public enum EFishStruggle 
    {
        Reckless,
        Cautious,
        Rest
    };

    [SerializeField] public SpriteRenderer         render;
    [SerializeField] public EFishState      state = EFishState.Idle;
    [SerializeField] public EFishStruggle   resistBehaviour = EFishStruggle.Reckless;
    [SerializeField][Range(0.0f, 10.0f)] 
    public float                                   maxStamina = 5;
    [SerializeField] public float                  stamina;
    [SerializeField] public float           pullStrength = 1.0f;
    [SerializeField] public float                  speed = 3;
    [SerializeField] [Range(0.0f, 1.0f)] 
    public float                  resistSpeed = 3;
    [SerializeField][Range(0.0f, 1.0f)]
    float                                   resistBehaviourWeight = 1.0f;    //closer to 1.0 for pure runaway, closer to 0.0 for tsun behaviour
    public Vector3                          goalWaypoint;
    public Hook                             myHook;
    public float waitTimer;
    [SerializeField] public float                  baitWaitTime = 5.0f;
    public bool isAtBait;
    public bool wasHooked;
    [SerializeField][Range(0.0f, 1.0f)] 
    float                                   desperation = 0.8f;             //determind the percent stamina before struggling again

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stamina = maxStamina;
        isAtBait = false;
        wasHooked = false;
        Hook.player = FishManager.instance.player;
        Hook.line = FishManager.instance.player.GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case EFishState.Idle:
                Debug.Log("Idle");
                IdleState();
                break;
            case EFishState.Lured:
                Debug.Log("Lured");
                LuredState();
                break;
            case EFishState.BaitedFail:
                Debug.Log("BaitedFail");
                BaitedFailState();
                break;
            case EFishState.Hooked:
                Debug.Log("Hooked");
                HookedState();
                break;
            case EFishState.HookedFail:
                Debug.Log("HookedFail");
                HookedFailState();
                break;
            case EFishState.Reeled:
                Debug.Log("Reeled");
                ReeledState();
                break;
            case EFishState.Escape:
                Debug.Log("Escape");
                EscapeState();
                break;
            default:
                break;
        }
        if (state != EFishState.Reeled && render.color.a < 1.0f) 
        {
            Color clr = render.color;
            clr.a += Time.deltaTime;
            render.color = clr;
        }
        transform.position -= new Vector3(0,0,transform.position.z);
    }

    public void Reset()
    {
        myHook.Reset();
        stamina = maxStamina;
        isAtBait = false;
        wasHooked = false;
        render.color = Color.white;
        state = EFishState.Idle;
        transform.position = FishManager.instance.GetSpawnWaypoint();
    }

    bool MoveToPoint(Vector3 goal) 
    {
        Vector3 lookDir = (goalWaypoint - this.transform.position);
        lookDir.z = 0;
        lookDir = lookDir.normalized;
        this.transform.position += lookDir * speed * Time.deltaTime;
        transform.rotation.SetLookRotation(lookDir);
        return IsAtPos(goalWaypoint);
    }
    bool IsAtGoal() 
    {
        return IsAtPos(goalWaypoint);
    }
    
    bool IsAtPos(Vector3 _pos) 
    {
        float xyMag = (transform.position.x - _pos.x) * (transform.position.x - _pos.x) 
            + (transform.position.y - _pos.y) * (transform.position.y - _pos.y);
        return  xyMag  < 0.001f ;
    }

    void IdleState()
    {
        if (MoveToPoint(goalWaypoint)) 
        {
            goalWaypoint = FishManager.instance.GetGoalWaypoint(goalWaypoint);
        }
    }

    void LuredState()
    {
        if (MoveToPoint(goalWaypoint) && !isAtBait)
        {
            isAtBait = true;
            waitTimer = baitWaitTime;
        }
        if (isAtBait) 
        {
            waitTimer -= Time.deltaTime;
        }
        if (waitTimer < 0)
        {
            state = EFishState.BaitedFail;    //exit state
        }
    }

    void BaitedFailState()
    {
        FishManager.instance.isCurrentlyFishing = false;
        if (wasHooked) 
        {
            goalWaypoint = FishManager.instance.GetEscapeWaypoint();
            state = EFishState.Escape;
            return;
        }
        state = EFishState.Idle;
        goalWaypoint = FishManager.instance.GetGoalWaypoint();
    }

    void HookedState()
    {
        if (!wasHooked) { wasHooked = true; }
        switch (resistBehaviour) 
        {
            case EFishStruggle.Reckless:
                myHook.ResistReel(true);
                stamina -= Time.deltaTime * 1.5f;
                if (stamina < maxStamina * .1f) 
                {
                    resistBehaviour = EFishStruggle.Rest;
                }
                break;
            case EFishStruggle.Cautious:
                if (myHook.tension > 0.5f)
                {
                    myHook.ResistReel(true);
                    stamina -= Time.deltaTime * 1.5f;
                }
                else 
                {
                    myHook.ResistReel(false);
                    stamina -= Time.deltaTime;
                }
                if (stamina < maxStamina * .1f)
                {
                    resistBehaviour = EFishStruggle.Rest;
                }
                break;
            case EFishStruggle.Rest:
                myHook.ResistReel(false);
                stamina += Time.deltaTime;
                if (stamina > maxStamina * desperation)
                {   //recovered
                    float x = Random.Range(0, 1.0f);

                    if (x > resistBehaviourWeight)
                    {
                        resistBehaviour = EFishStruggle.Reckless;
                    }
                    else
                    {
                        resistBehaviour = EFishStruggle.Cautious;
                    }
                }
                break;
            default:
                break;
        }
        if (IsAtPos(Hook.player.transform.position)) {
            state = EFishState.Reeled;
        }if (IsAtPos(goalWaypoint)) {
            state = EFishState.HookedFail;
        }

    }

    void HookedFailState()
    {
        state = EFishState.BaitedFail;
        FishManager.instance.isCurrentlyFishing = false;
    }
    void ReeledState()
    {   //disappear and destory
        Color clr = render.color;
        clr.a -= Time.deltaTime * 0.18f;
        render.color = clr;
        if (clr.a <= 0.0f) 
        {
            Reset();
            FishManager.instance.isCurrentlyFishing = false;
        }
    }

    void EscapeState() 
    {
        if (MoveToPoint(goalWaypoint)) 
        {
            state = EFishState.Reeled;  //reuse reeled state to dissappear.       
        }
        
    }
}
