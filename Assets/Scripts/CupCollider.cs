using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CupCollider
{
    public Vector3 pos;
    public Vector3 right;
    public Vector3 up;
    public Vector3 scale;

    public CupCollider(){
    }
    
    public void setPosition(Vector3 p){
        pos = p;
    }

    public void setRight(Vector3 r){
        right = r;
    }

    public void setUp(Vector3 u){
        up = u;
    }

    public void setScale(Vector3 s){
        scale = s;
    }
}
