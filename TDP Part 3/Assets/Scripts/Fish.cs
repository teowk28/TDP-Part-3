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
    [SerializeField] public float                   pullStrength = 1.0f;
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
    public bool isBaitFail;
    [SerializeField][Range(0.0f, 1.0f)] 
    float                                   desperation = 0.8f;             //determind the percent stamina before struggling again

    public class DebugColor 
    {
        public float alpha = 1.0f;
        public Color Idle = Color.white,           //patrol state
        Lured = Color.green,          //optimized and removed baited, go directly to lured
        BaitedFail = Color.red,
        HookedFail = Color.red,     //funnel for escape checking
        Hooked = Color.yellow,         //hooked and resist implementation
        HookedResist = Color.yellow,         //hooked and resist implementation
        HookedRest = Color.cyan,         //hooked and resist implementation
        Reeled = Color.green,          //success state, disappear or smth
        Escape = Color.black;
    };

    [SerializeField] DebugColor fish_colors = new DebugColor();
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
                if(FishManager.instance.debugColorSwitch)
                    GetComponent<SpriteRenderer>().color = fish_colors.Idle - new Color(0,0,0,1 - GetComponent<SpriteRenderer>().color.a);
                break;
            case EFishState.Lured:
                Debug.Log("Lured");
                LuredState();
                if (FishManager.instance.debugColorSwitch)
                    GetComponent<SpriteRenderer>().color = fish_colors.Lured - new Color(0, 0, 0,1- GetComponent<SpriteRenderer>().color.a);
                break;
            case EFishState.BaitedFail:
                Debug.Log("BaitedFail");
                BaitedFailState();
                if (FishManager.instance.debugColorSwitch)
                    GetComponent<SpriteRenderer>().color = fish_colors.BaitedFail - new Color(0, 0, 0,1- GetComponent<SpriteRenderer>().color.a);
                break;
            case EFishState.Hooked:
                Debug.Log("Hooked");
                HookedState();
                if (FishManager.instance.debugColorSwitch)
                {
                    Color x = (resistBehaviour == EFishStruggle.Rest)? fish_colors.HookedRest  :fish_colors.HookedResist
                        - new Color(0, 0, 0, 1 - GetComponent<SpriteRenderer>().color.a);
                    GetComponent<SpriteRenderer>().color = x - new Color(0, 0, 0, 1 - GetComponent<SpriteRenderer>().color.a);
                } 
                break;
            case EFishState.HookedFail:
                Debug.Log("HookedFail");
                HookedFailState();
                if (FishManager.instance.debugColorSwitch)
                    GetComponent<SpriteRenderer>().color = fish_colors.HookedFail - new Color(0, 0, 0, 1 - GetComponent<SpriteRenderer>().color.a);
                break;
            case EFishState.Reeled:
                Debug.Log("Reeled");
                ReeledState();
                if (FishManager.instance.debugColorSwitch)
                    GetComponent<SpriteRenderer>().color = fish_colors.Reeled - new Color(0, 0, 0, 1 - GetComponent<SpriteRenderer>().color.a);
                break;
            case EFishState.Escape:
                Debug.Log("Escape");
                EscapeState();
                if (FishManager.instance.debugColorSwitch)
                    GetComponent<SpriteRenderer>().color = fish_colors.Escape - new Color(0, 0, 0, 1 - GetComponent<SpriteRenderer>().color.a);
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

    public void Reset(bool resetPos = false)
    {
        myHook.Reset();
        stamina = maxStamina;
        isAtBait = false;
        wasHooked = false;
        isBaitFail = false;
        render.color = Color.white;
        state = EFishState.Idle;
        transform.position =(resetPos)? FishManager.instance.GetSpawnWaypoint(): transform.position;
        FishManager.instance.isCurrentlyFishing = false;
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
    
    bool MoveNearPoint(Vector3 goal, float _buffer) 
    {
        Vector3 lookDir = (goalWaypoint - this.transform.position);
        lookDir.z = 0;
        lookDir = lookDir.normalized;
        this.transform.position += lookDir * speed * Time.deltaTime;
        transform.rotation.SetLookRotation(lookDir);
        return IsNearPos(goalWaypoint,_buffer);
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
    
    bool IsNearPos(Vector3 _pos, float _buffer) 
    {
        float xyMag = (transform.position.x - _pos.x) * (transform.position.x - _pos.x) 
            + (transform.position.y - _pos.y) * (transform.position.y - _pos.y);
        return  xyMag  < _buffer ;
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
        if (MoveNearPoint(goalWaypoint, 0.01f) && !isAtBait)
        {
            isAtBait = true;
            waitTimer = baitWaitTime;
        }
        if (waitTimer < 0)
        {
            state = EFishState.BaitedFail;    //exit state
        }
        if (isAtBait) 
        {
            waitTimer -= Time.deltaTime;
        }
        
    }

    void BaitedFailState()
    {
        FishManager.instance.isCurrentlyFishing = true;
        if (wasHooked) 
        {
            goalWaypoint = FishManager.instance.GetEscapeWaypoint();
            state = EFishState.Escape;
            return;
        }
        if (isBaitFail) 
        {
            if (MoveToPoint(goalWaypoint)) 
            {
                FishManager.instance.isCurrentlyFishing = false;
                   isBaitFail = false;
                state = EFishState.Idle;
                Reset();
            }
        }
        goalWaypoint = FishManager.instance.GetGoalWaypoint();

        isBaitFail = true;
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
                    const float max = 9000.0f;
                    float x = Random.Range(0, max);

                    if (x/max < resistBehaviourWeight)
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

        if (IsNearPos(Hook.player.transform.position, 0.01f)) {
            state = EFishState.Reeled;
        }if (IsNearPos(goalWaypoint, 0.01f)) {
            state = EFishState.HookedFail;
        }

    }

    void HookedFailState()
    {
        state = EFishState.BaitedFail;
        wasHooked = true;
        FishManager.instance.isCurrentlyFishing = false;
        NewHook.instance.ResetPos();
    }
    void ReeledState()
    {   //disappear and destory
        FishManager.instance.isCurrentlyFishing = true;
        Color clr = render.color;
        clr.a -= Time.deltaTime * 0.18f;
        render.color = clr;
        if (clr.a <= 0.0f) 
        {
            Reset();
            NewHook.instance.ResetPos();
            FishManager.instance.isCurrentlyFishing = false;
        }
    }

    void EscapeState()
    {
        FishManager.instance.isCurrentlyFishing = true;
        if (MoveToPoint(goalWaypoint)) 
        {
            state = EFishState.Reeled;  //reuse reeled state to dissappear.       
        }
        
    }
}
