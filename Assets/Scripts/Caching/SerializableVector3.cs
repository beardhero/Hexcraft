 using UnityEngine;
 using System;
 using System.Collections;
 
 /// <summary>
 /// Since unity doesn't flag the Vector3 as serializable, we
 /// need to create our own version. This one will automatically convert
 /// between Vector3 and SerializableVector3
 /// </summary>
 [System.Serializable]
 public struct SerializableVector3
 {
    public float x {get;set;}
    public float y {get;set;}
    public float z {get;set;}
    public float magnitude, sqrMagnitude;

    public SerializableVector3(float rX, float rY, float rZ)
    {
        x = rX;
        y = rY;
        z = rZ;
        sqrMagnitude = x*x + y*y + z*z;
        magnitude = Mathf.Sqrt(sqrMagnitude);
    }

    public Vector3 toVector(SerializableVector3 v)
    {
      return new Vector3(v.x, v.y, v.z);
    }

    public Vector3 ToVector3(){
      return new Vector3(this.x, this.y, this.z);
    }

    public override string ToString()
    {
        return String.Format("[{0}, {1}, {2}]", x, y, z);
    }

    public void Normalize()
    {
      if (sqrMagnitude == 0)
      {
        return;
      }
      x /= magnitude;
      y /= magnitude;
      z /= magnitude;
    }

    [Newtonsoft.Json.JsonIgnore] public SerializableVector3 normalized {get{
      x /= magnitude;
      y /= magnitude;
      z /= magnitude;
      return this;
    } set{}}
     
     

    public static SerializableVector3 operator +(SerializableVector3 t, SerializableVector3 o)
    {
    return new SerializableVector3(t.x+o.x, t.y+o.y, t.z+o.z);
    }

    public static SerializableVector3 operator /(SerializableVector3 t, int o)
    {
    return new SerializableVector3(t.x/o, t.y/o, t.z/o);
    }
    public static SerializableVector3 operator /(SerializableVector3 t, float o)
    {
    return new SerializableVector3(t.x/o, t.y/o, t.z/o);
    }

    public static SerializableVector3 operator -(SerializableVector3 t, SerializableVector3 o)
    {
    return new SerializableVector3(t.x-o.x, t.y-o.y, t.z-o.z);
    }

    public static SerializableVector3 operator *(SerializableVector3 t, float o)
    {
    return new SerializableVector3(t.x*o, t.y*o, t.z*o);
    }



    public static implicit operator Vector3(SerializableVector3 rValue)
    {
        return new Vector3(rValue.x, rValue.y, rValue.z);
    }

    public static implicit operator SerializableVector3(Vector3 rValue)
    {
        return new SerializableVector3(rValue.x, rValue.y, rValue.z);
    }
 }