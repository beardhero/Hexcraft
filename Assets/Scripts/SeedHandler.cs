using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SeedHandler : MonoBehaviour
{
    public static byte[] StringToBytes(string s)
    {
        return Encoding.ASCII.GetBytes(s);
    }
    public static string BytesToString(byte[] b)
    {
        return Encoding.ASCII.GetString(b);
    }
}
