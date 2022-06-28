using System;
using System.IO;
using UnityEngine;

public class Test: MonoBehaviour
{
    public string path;

    private void Start()
    {
        byte[] b = File.ReadAllBytes(path);

        PCDParser.ParsePCD(b);
    }
}