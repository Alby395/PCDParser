using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

public static class PCDParser
{

    public static void ParsePCD(byte[] bytes)
    {
        Thread t = new Thread(ParsePCDThread);

        t.Start(bytes);
    }


    private static void ParsePCDThread(object obj)
    {
        byte[] bytes = (byte[]) obj;

        PCDData pcd = ParseHeader(bytes);
        
        Debug.Log(pcd.ToString());
    }

    private static PCDData ParseHeader(byte[] bytes)
    {
        PCDData pcd = new PCDData();
        
        string headerText = System.Text.Encoding.Default.GetString(bytes, 0, 1024);

        int max = bytes.Length;

        // Find DATA field (end of header)
        Match m = Regex.Match(headerText, "[\r\n]DATA\\s(\\S*)\\s");
        
        pcd.data = m.Groups[1].Value;
        pcd.headerLen = m.Index + m.Groups[0].Length;
        pcd.str = headerText.Substring(0, pcd.headerLen);
        Debug.Log("START");
        // Remove comments
        pcd.str = Regex.Replace(pcd.str, "\\#.*\\n", "");
        Debug.Log("1");
        // Retrieve value from fields
        m = Regex.Match(pcd.str, "VERSION (.*)");
        if(m.Success)
        {
            Debug.Log("SUCCESS");
            Debug.Log(m.Groups[1].Value);
            pcd.version = float.Parse("0" + m.Groups[1].Value);
        }
            
        Debug.Log("2");
        m = Regex.Match(pcd.str, "FIELDS (.*)");
        if(m.Success)
        {
            pcd.fields = m.Groups[1].Value.Split(' ');
        }
        m = Regex.Match(pcd.str, "SIZE (.*)");
        if(m.Success)
        {
            string[] tmp = m.Groups[1].Value.Split(' ');

            int[] val = new int[tmp.Length];

            for(int i = 0; i < tmp.Length; i++)
            {
                val[i] = int.Parse(tmp[i]);
            }

            pcd.size = val;
        }

        m = Regex.Match(pcd.str, "TYPE (.*)");
        if(m.Success)
        {
            pcd.type = m.Groups[1].Value.Split(' ');
        }

        m = Regex.Match(pcd.str, "COUNT (.*)");
        if(m.Success)
        {
            string[] tmp = m.Groups[1].Value.Split(' ');

            int[] val = new int[tmp.Length];

            for(int i = 0; i < tmp.Length; i++)
            {
                val[i] = int.Parse(tmp[i]);
            }

            pcd.count = val;
        }

        m = Regex.Match(pcd.str, "WIDTH (.*)");
        if(m.Success)
        {
            pcd.width = int.Parse(m.Groups[1].Value);
        }

        m = Regex.Match(pcd.str, "HEIGHT (.*)");
        if(m.Success)
        {
            pcd.height = int.Parse(m.Groups[1].Value);
        }

        m = Regex.Match(pcd.str, "VIEWPOINT (.*)");
        if(m.Success)
        {
            pcd.viewpoint = m.Groups[1].Value;
        }

        m = Regex.Match(pcd.str, "POINTS (.*)");
        if(m.Success)
        {
            pcd.points = int.Parse(m.Groups[1].Value);
        }
        else
        {
            pcd.points = pcd.width * pcd.height;
        }

        /*
        if (header.count === null) {
            header.count = []
            for (i = 0; i < header.fields; i++) {
            header.count.push(1)
            }
        }
        */
        int sizeSum = 0;
        pcd.offset = new Dictionary<string, int>();

        for(int i = 0; i < pcd.fields.Length; i++)
        {
            
            if(pcd.data.Equals("ascii"))
            {
                pcd.offset.Add(pcd.fields[i], i);
            }
            else if(pcd.data.Equals("binary"))
            {
                pcd.offset.Add(pcd.fields[i], sizeSum);
                sizeSum += pcd.size[i];
            }
            else if(pcd.data.Equals("binary_compressed"))
            {
                pcd.offset.Add(pcd.fields[i], sizeSum);
                sizeSum += pcd.size[i] * pcd.points;
            }
        }

        pcd.rowSize = sizeSum;
        Debug.Log(pcd.ToString());
        return pcd;
    }
}

public struct PCDData
{
    public string str;
    public int headerLen;
    public string data;
    internal float version;
    internal string[] fields;
    internal int[] size;
    internal string[] type;
    internal int[] count;
    internal int width;
    internal int height;
    internal string viewpoint;
    internal int points;
    internal Dictionary<string, int> offset;
    internal int rowSize;

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Version " + version);
        builder.AppendLine("field");

        foreach(string val in fields)
        {
            builder.Append(" " + val);
        }

        return builder.ToString();
    }
}
