using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

public class TestFixedPoint : MonoBehaviour
{
    public enum MoveEnum
    {
        Fp,
        Float
    }

    public MoveEnum moveType;

    public GameObject go;

    public Vector3 offset;

    
    
    public void Start()
    {
        fp3 position1 = fp3.zero;

        fp3 position2 = new fp3(0, 0, 5);
        
        Debug.Log(fpmath.distance(position1,position2));
    }
    

    void Update()
    {
        switch (moveType)
        {
            case MoveEnum.Fp:

                FpMove();
                
                break;
            
            case MoveEnum.Float:

                FloatMove();
                
                break;
        }
    }

    [ContextMenu("float")]
    public void Create()
    {
        go.transform.position =  TsUtil.TransformPoint(transform.position, 
            transform.rotation.eulerAngles, Vector3.one, offset);
    }
    
    [ContextMenu("fp")]
    public void Create111()
    {
        go.transform.position =  (TsUtil.TransformPoint(transform.position.ToFp3(), 
            transform.rotation.eulerAngles.ToFp3(), new fp3(1,1,1), offset.ToFp3())).ToVector3();
    }

    public void FloatMove()
    {
        Vector2 moveDir = Vector2.zero;
        
        if (Input.GetKey(KeyCode.W))
        {
            moveDir += new Vector2(0, 1);
        }
        
        if (Input.GetKey(KeyCode.A))
        {
            moveDir += new Vector2(-1, 0);
        }
        
        if (Input.GetKey(KeyCode.S))
        {
            moveDir += new Vector2(0, -1);
        }
        
        if (Input.GetKey(KeyCode.D))
        {
            moveDir += new Vector2(1, 0);
        }

        if (moveDir == Vector2.zero)
        {
            return;
        }
        
        var tempValue  = Quaternion.LookRotation(new Vector3(moveDir.x,0,moveDir.y),Vector3.up);

        transform.rotation = Quaternion.Slerp(transform.rotation, tempValue, 5 * Time.deltaTime);
    }
    
    public void FpMove()
    {
        fp2 moveDir = fp2.zero;
        
        if (Input.GetKey(KeyCode.W))
        {
            moveDir += new fp2(0, 1);
        }
        
        if (Input.GetKey(KeyCode.A))
        {
            moveDir += new fp2(-1, 0);
        }
        
        if (Input.GetKey(KeyCode.S))
        {
            moveDir += new fp2(0, -1);
        }
        
        if (Input.GetKey(KeyCode.D))
        {
            moveDir += new fp2(1, 0);
        }
        
        if ((moveDir == fp2.zero).Bool2ToBool())
        {
            return;
        }
        
        var tempValue  = fpmath1.LookRotation(new fp3(moveDir.x,0,moveDir.y),fpmath1.up());

        fpquaternion fpquaternion = fpmath1.slerp(transform.rotation, tempValue, (fp)(5 * Time.deltaTime));
        
        Debug.LogError(fpquaternion.ToEulerAngles());

        transform.rotation = fpquaternion;
    }
}
