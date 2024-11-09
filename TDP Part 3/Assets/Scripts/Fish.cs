/*==================================================
 * Script: Fish
 * Description:
 *  Handle State machine behaviour for fishes
 ===================================================*/



using UnityEngine;

public class Fish : MonoBehaviour
{
    public class Hook
    {
        bool isActive;
        bool isHooked;
        bool isReeling;
        public int lineStrength;            //default 1
        public int reelStrength;            //default 1, decide distance reeled for each percent of strength.
        float hp;                           //default 100
        public float maxHp;                 //default 100
        public float sinkRate;              //default 100
        public float tension;

        static public LineRenderer line;
        public Transform fishPosition;
        public Vector3 hookPosition;
        Fish owner;
        static public GameObject player;
        public Hook(Fish _og, Transform _fishPos, int _rs = 2, int _ls = 1, float _hp = 100)
        {
            owner = _og;

            fishPosition = _fishPos;
            reelStrength = _rs;
            lineStrength = _ls;
            hp = _hp;
            maxHp = _hp;
            tension = 0;
            isHooked = false;
            isActive = false;
            isReeling = false;
        }
        //==============================================
        //          State updating functions
        //==============================================
        public void Update() 
        {
            if (!isHooked) 
            {
                hookPosition -= (new Vector3(0, -1, 0)) * sinkRate;

                line.SetPosition(0, hookPosition);
                line.SetPosition(1, player.transform.position);

                line.startColor = Color.black;
                line.endColor = Color.black;
                return;
            }

            //line position
            line.SetPosition(0, fishPosition.position);
            line.SetPosition(1, player.transform.position);

            //set line color based on remaining strength before breaking
            float hpPercent = 1.0f - hp / maxHp;
            ColorUtility.TryParseHtmlString("#DE3416", out Color tensionClr);
            tensionClr.r *= hpPercent;
            tensionClr.g *= hpPercent;
            tensionClr.b *= hpPercent;

            line.startColor = tensionClr;
            line.endColor = tensionClr;
        }
        public void Hooked()
        {
            line.startWidth = line.endWidth = 1;
            hp = maxHp;
            tension = 0;
            isHooked = true;
        }
        void Reset()
        {
            line.startWidth = line.endWidth = 0;
            hp = maxHp;
            tension = 0;
            isHooked = false;
            isActive = false;
            isReeling = false;
        }

        public void StartFishing(Vector3 hookPos) 
        {
            hookPosition = hookPos;
            isActive = true;
            owner.goalWaypoint = hookPosition;
            owner.state = EFishState.Lured;
            owner.waitTimer = owner.baitWaitTime;
        }


        //==============================================
        //              Reel functions
        //==============================================

        public void Reel(float _strength) 
        {
            if (isHooked)
                ReelByStrength(_strength);
            else
                ReelBait(_strength);
        }

        //Reel when not hooked
        public void ReelBait(float _strength)      //1.0 to -1.0f
        {
            if (_strength < 0) { return; }  //no negative strength behaviour
            float reelDist = _strength * reelStrength;
            //set fish position
            Vector3 lookDir = (player.transform.position - fishPosition.position).normalized;
            hookPosition += lookDir.normalized * reelDist;
            if (owner.isAtBait) 
            {
                owner.state = EFishState.Hooked;
                isHooked = true;
            }
        }

        //Reel when hooked
        public void ReelByStrength(float _strength)     //1.0 to -1.0f
        {
            tension += _strength;
            if (tension < 0) { tension = 0; }
            isReeling = true;
            if (tension > lineStrength)
            {
                hp -= (tension - lineStrength) * Time.deltaTime;    //hp reduce by the stress on tesion that is higher than line strength
                if (hp <= 0) { LineBreak(); return; }
            }

            float reelDist = _strength * reelStrength;
            //set fish position
            Vector3 lookDir = (player.transform.position - fishPosition.position).normalized;
            fishPosition.position += lookDir.normalized * reelDist;
            fishPosition.rotation.SetLookRotation(lookDir);
        }

        //Reel Resist when hooked
        public void ResistReel(bool _resist)
        {
            if (!_resist || !isHooked) { return; }
            float reelDist = owner.pullStrength * reelStrength;
            tension += owner.pullStrength;
            Vector3 lookDir = (owner.goalWaypoint - fishPosition.position).normalized;
            fishPosition.position += lookDir.normalized * reelDist;
            fishPosition.rotation.SetLookRotation(lookDir);
        }

        public void LineBreak()
        {
            owner.state = Fish.EFishState.HookedFail;
            this.Reset();
        }
    }

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

    [SerializeField] SpriteRenderer         render;
    [SerializeField] public EFishState      state = EFishState.Idle;
    [SerializeField] public EFishStruggle   resistBehaviour = EFishStruggle.Reckless;
    [SerializeField][Range(0.0f, 10.0f)] 
    float                                   maxStamina = 5;
    [SerializeField] float                  stamina;
    [SerializeField] public float           pullStrength = 1.0f;
    [SerializeField] float                  speed = 3;
    [SerializeField][Range(0.0f, 1.0f)]
    float                                   resistBehaviourWeight = 1.0f;    //closer to 1.0 for pure runaway, closer to 0.0 for tsun behaviour
    public Vector3                          goalWaypoint;
    public Hook                             myHook;
    float                                   waitTimer;
    [SerializeField] float                  baitWaitTime = 5.0f;
    bool                                    isAtBait;
    bool                                    wasHooked;
    [SerializeField][Range(0.0f, 1.0f)] 
    float                                   desperation = 0.8f;             //determind the percent stamina before struggling again

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        myHook = new Hook(this, transform);
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
    }

    public void Reset()
    {
        myHook = new Hook(this, transform);
        stamina = maxStamina;
        isAtBait = false;
        wasHooked = false;
        render.color = Color.white;
        state = EFishState.Idle;
        transform.position = FishManager.instance.GetSpawnWaypoint();
    }

    bool MoveToPoint(Vector3 goal) 
    {
        Vector3 lookDir = (goalWaypoint - this.transform.position).normalized;
        this.transform.position += lookDir * speed * Time.deltaTime;
        transform.rotation.SetLookRotation(lookDir);
        return (transform.position - goalWaypoint).magnitude < 0.01f ;
        }
    bool IsAtGoal() 
    {
        return (transform.position - goalWaypoint).magnitude < 0.01f ;
    }
    
    bool IsAtPos(Vector3 _pos) 
    {
        return (transform.position - _pos).magnitude < 0.01f ;
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
        clr.a -= Time.deltaTime * 0.1f;
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
