using System;
using System.Numerics;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewHook : MonoBehaviour
{
    bool isThrown;
    bool isReeling;
    public float angle;
    public float power;

    public float trajectoryTime;

    public UnityEngine.Vector2 defOffset;

    UnityEngine.Vector2 hookShot;
    UnityEngine.Vector3 rodPos;
    [SerializeField] public GameObject player;
    float gravity = -0.98f;
    float wGravity = -0.5f;

    LineRenderer line;
    LineRenderer indicator;

    public static NewHook instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        { Destroy(this); }
        instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isThrown = false;
        isReeling = false;
        angle = 30.0f;
        power = 2.5f;
        trajectoryTime = 2.0f;
        line = player.GetComponent<LineRenderer>();
        indicator = gameObject.GetComponent<LineRenderer>();
        rodPos = player.transform.position;
        rodPos.x += defOffset.x;
        rodPos.y += defOffset.y;
        ResetPos();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            
            if (!isThrown)
            {isThrown = true;
            float angleRad = (180.0f - angle) * Mathf.PI / 180.0f;
            hookShot.x = power * Mathf.Cos(angleRad);
            hookShot.y = power * Mathf.Sin(angleRad);
            line.startWidth = line.endWidth = 0.1f;
            line.SetPosition(0, gameObject.transform.position);
            line.SetPosition(1, rodPos);}
        }

        if (Input.GetKey(KeyCode.P))
        {
            if (isThrown && !isReeling)
            {
                isReeling = true;
                hookShot.x = player.transform.position.x - gameObject.transform.position.x;
                hookShot.y = player.transform.position.y - gameObject.transform.position.y;
                hookShot.Normalize();
                hookShot *= 1.5f;
            }
        }
        if (Input.GetKeyUp(KeyCode.P))
        {
            isReeling = false;
            hookShot.x = 0.0f;
            hookShot.y = 0.0f;
        }

        if (Input.GetMouseButtonDown((int)MouseButton.Right))
        {
            line.startWidth = line.endWidth = 0.0f;
            ResetPos();
            isThrown = false;
        }
        
        if (Input.GetKey(KeyCode.W))
        {
            angle += 20.0f * Time.deltaTime;
            if (angle >= 50.0f) angle = 50.0f;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            angle -= 20.0f * Time.deltaTime;
            if (angle <= 25.0f) angle = 25.0f;
        }

        if (Input.GetKey(KeyCode.A))
        {
            power += 5.0f * Time.deltaTime;
            if (power >= 7.0f) power = 7.0f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            power -= 5.0f * Time.deltaTime;
            if (power <= 1.0f) power = 1.0f;
        }

        // if (!isThrown)
        // {
        //     //indicator.SetPosition(0, gameObject.transform.position);
        //     //UnityEngine.Vector3 indPos;// = gameObject.transform.position;
        //     float angleRad = (180.0f - angle) * Mathf.PI / 180.0f;
        //     hookShot.x = power * Mathf.Cos(angleRad);
        //     hookShot.y = power * Mathf.Sin(angleRad);
        //     // indPos = hookShot.normalized;
        //     // indPos *= power / 10.0f;
        //     // indPos.x += gameObject.transform.position.x;
        //     // indPos.y += gameObject.transform.position.y;
        //     // indicator.SetPosition(1, indPos);
        //     // indicator.startWidth = 0.4f;
        //     // indicator.endWidth = 0.1f;

        //     int size = (int)(trajectoryTime / Time.deltaTime);

        //     UnityEngine.Vector3[] points = new UnityEngine.Vector3[size];
        //     UnityEngine.Vector2 hookEmu = hookShot;
        //     for (int i = 0; i < size; ++i)
        //     {
        //         hookEmu.y += gravity * Time.deltaTime;
        //         points[i].x = gameObject.transform.position.x + i * hookEmu.x * Time.deltaTime;
        //         points[i].y = gameObject.transform.position.y + hookEmu.y * Time.deltaTime;
        //     }
        //     indicator.positionCount = size;
        //     indicator.SetPositions(points);
        //     indicator.startWidth = indicator.endWidth = 0.1f;
        // }
        UnityEngine.Vector3 movePos = gameObject.transform.position;

        if (isThrown)
        {
            

            UnityEngine.Vector3 posCheck = Camera.main.WorldToViewportPoint(movePos);

            if (!FishManager.instance.isCurrentlyFishing)
            {
                if (posCheck.x < 0.0f)
                {
                    print("Out of view");
                    line.startWidth = line.endWidth = 0.0f;
                    ResetPos();
                    isThrown = false;
                    return;
                }

                if (!isReeling)
                {
                    if (movePos.y < 2.0f) 
                    {
                        if (hookShot.x <= 0.0f) hookShot.x = 0.0f;
                        hookShot.y = wGravity;
                        if (movePos.y <= -3.5f) hookShot.y = 0.0f;

                        if (!FishManager.instance.isCurrentlyFishing)
                        {
                            FishManager.instance.CastLine(movePos);
                        }
                    }
                    else hookShot.y += gravity * Time.deltaTime;
                }
                else
                {
                    if (movePos.x >= rodPos.x)
                    {
                        hookShot.x = 0.0f;
                        movePos.x = rodPos.x;
                    }
                }
                

                movePos.x += hookShot.x * Time.deltaTime;
                movePos.y += hookShot.y * Time.deltaTime;
                gameObject.transform.position = movePos;
                line.SetPosition(0, gameObject.transform.position);
                line.SetPosition(1, rodPos);
            }
        }

        //check for original position
        // if (movePos.x == rodPos.x && movePos.y == rodPos.y)
        // {
        //     isReeling = false;
        //     isThrown = false;
        //     ResetPos();
        // }
        // if ((movePos.x <= rodPos.x + 0.1f && movePos.x >= rodPos.x - 0.1f) && (movePos.y <= rodPos.y + 0.1f && movePos.y >= rodPos.y - 0.1f) && isReeling)
        // {
        //     isReeling = false;
        //     isThrown = false;
        //     ResetPos();
        // }
    }

    public void ResetPos()
    {
        rodPos = player.transform.position;
        rodPos.x += defOffset.x;
        rodPos.y += defOffset.y;
        Debug.LogError(rodPos);
        gameObject.transform.position = rodPos;
        isReeling = false;
        isThrown = false;
    }
}
