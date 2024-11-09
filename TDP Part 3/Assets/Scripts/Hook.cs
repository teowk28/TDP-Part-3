using UnityEngine;

public class Hook : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tension = 0;
        isHooked = false;
        isActive = false;
        isReeling = false;
        player = FishManager.instance.player;
        line = FishManager.instance.player.GetComponent<LineRenderer>();
        owner = GetComponent<Fish>();
    }
    bool isActive;
    bool isHooked;
    bool isReeling;
    public int lineStrength = 1;            //default 1
    [SerializeField]
    [Range(0.0f, 1.0f)]
    public float reelStrength = 1;            //default 1, decide distance reeled for each percent of strength.
    [Range(0.0f, 5.0f)]
    public float baitReelStrength = 1;            //default 1, decide distance reeled for each percent of strength.
    float hp = 100.0f;                           //default 100
    public float maxHp = 100.0f;                 //default 100
    public float sinkRate = 0.3f;              //default 0.3f
    public float tension;

    static public LineRenderer line;
    public Transform fishPosition;
    public Vector3 hookPosition;
    Fish owner;
    static public GameObject player;
    //public Hook(Fish _og, Transform _fishPos, int _rs = 2, int _ls = 1, float _hp = 100)
    //{
    //    owner = _og;

    //    fishPosition = _fishPos;
    //    reelStrength = _rs;
    //    lineStrength = _ls;
    //    hp = _hp;
    //    maxHp = _hp;
    //    tension = 0;
    //    isHooked = false;
    //    isActive = false;
    //    isReeling = false;
    //}
    //==============================================
    //          State updating functions
    //==============================================
    public void Update()
    {
        if (!isActive) { return; }
        if (owner.state == Fish.EFishState.Reeled) 
        {
            line.startWidth = line.endWidth = 0.0f;
            return;
        }
        if (hp == 0) { LineBreak(); }
        if (!isHooked)
        {
            hookPosition += (new Vector3(0, -1, 0)) * sinkRate * Time.deltaTime;

            line.SetPosition(0, hookPosition);
            line.SetPosition(1, player.transform.position);

            owner.goalWaypoint = hookPosition;

            line.startColor = Color.black;
            line.endColor = Color.black;
            return;
        }
        if(owner.state != Fish.EFishState.Escape) { 
            //line position
            line.SetPosition(0, fishPosition.position);
            line.SetPosition(1, player.transform.position);
        }
        //set line color based on remaining strength before breaking
        float hpPercent = hp / maxHp;
        ColorUtility.TryParseHtmlString("#DE3416", out Color tensionClr);
        tensionClr.r *= hpPercent;
        tensionClr.g *= hpPercent;
        tensionClr.b *= hpPercent;

        tensionClr = Color.white - tensionClr;

        line.startColor = tensionClr;
        line.endColor = tensionClr;
    }
    public void Hooked()
    {
        line.startWidth = line.endWidth = 0.3f;
        hp = maxHp;
        tension = 0;
        isHooked = true;
    }
    public void Reset()
    {
        line.startWidth = line.endWidth = 0.0f;
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
        owner.state = Fish.EFishState.Lured;
        owner.waitTimer = owner.baitWaitTime;
        line.startWidth = line.endWidth = 0.1f;
        line.startColor = Color.black;
        line.endColor = Color.black;
    }


    //==============================================
    //              Reel functions
    //==============================================

    public void Reel(float _strength)
    {
        switch (owner.state)
        {
            case Fish.EFishState.Idle:
            case Fish.EFishState.BaitedFail:
            case Fish.EFishState.HookedFail:
            case Fish.EFishState.Reeled:
            case Fish.EFishState.Escape:
                return;
            case Fish.EFishState.Lured:
                break;
            case Fish.EFishState.Hooked:
                break;
            default:
                return;
        }

        if (isHooked)
            ReelByStrength(_strength);
        else
            ReelBait(_strength);
    }

    //Reel when not hooked
    public void ReelBait(float _strength)      //1.0 to -1.0f
    {
        if (_strength < 0) { return; }  //no negative strength behaviour
        float reelDist = _strength * baitReelStrength;
        //set fish position
        Vector3 lookDir = (player.transform.position - fishPosition.position).normalized;
        hookPosition += lookDir.normalized * reelDist * Time.deltaTime;
        if (owner.isAtBait)
        {
            owner.state = Fish.EFishState.Hooked;
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
        fishPosition.position += lookDir.normalized * reelDist * Time.deltaTime;
        fishPosition.rotation.SetLookRotation(lookDir);
    }

    //Reel Resist when hooked
    public void ResistReel(bool _resist)
    {
        if (!_resist || !isHooked) { return; }
        float reelDist = owner.resistSpeed * reelStrength;
        tension += owner.pullStrength;
        Vector3 lookDir = (owner.goalWaypoint - fishPosition.position).normalized;
        fishPosition.position += lookDir.normalized * reelDist * Time.deltaTime;
        fishPosition.rotation.SetLookRotation(lookDir);
    }

    public void LineBreak()
    {
        owner.state = Fish.EFishState.HookedFail;
        this.Reset();
    }
}
