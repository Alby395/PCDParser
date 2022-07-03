using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

public static class PCDParser
{
    public static bool ready
    {
        get;
        private set;
    }

    public static void ParsePCD(byte[] bytes, Action<PCDData> callback)
    {
        ready = false;
        Thread t = new Thread(() => ParsePCDThread(bytes, callback));

        t.Start();
    }


    private static void ParsePCDThread(byte[] bytes, Action<PCDData> callback)
    {
        PCDData pcd = ParseHeader(bytes);
        Debug.Log(pcd.str);

        List<Vector3> position = new List<Vector3>();
/*
        Debug.Log("------ " + pcd.headerLen + " -----------");

        for(int x = 0; x < pcd.headerLen + 6; x++)
        {
            if(x == pcd.headerLen)
                Debug.Log("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");

            Debug.Log(x + ": " + bytes[x]);
            
        }
*/
        int color_offset = 0;

        pcd.offset.TryGetValue("rgb", out color_offset);

        if(color_offset == 0)
            pcd.offset.TryGetValue("rgba", out color_offset);

        List<Color> color = new List<Color>();
        bool colorBool = color_offset > 0;

        if(pcd.data.Equals("ascii"))
        {
            Debug.Log("ASCII NYI");
            return;
        }

        if(pcd.data.Equals("binary"))
        {
            int row = 0;
            byte[] data = new byte[bytes.Length - pcd.headerLen];

            Vector3 positionTmp = new Vector3();
            Color colorTmp = new Color();
            colorTmp.a = 1f;
            Array.Copy(bytes, pcd.headerLen, data, 0, data.Length);

            for(int p = 0; p < pcd.points; row += pcd.rowSize, p++)
            {
                byte[] pos = new byte[sizeof(float)];
                Array.Copy(data, row + pcd.offset["x"], pos, 0, sizeof(float));
                //Array.Reverse(pos);
                positionTmp.x = BitConverter.ToSingle(pos, 0);
                
                Array.Copy(data, row + pcd.offset["y"], pos, 0, sizeof(float));
                //Array.Reverse(pos);
                positionTmp.y = BitConverter.ToSingle(pos, 0);

                Array.Copy(data, row + pcd.offset["z"], pos, 0, sizeof(float));
                //Array.Reverse(pos);
                positionTmp.z = BitConverter.ToSingle(pos, 0);
                
                if(colorBool)
                {
                    colorTmp.b = ((float) data[row + color_offset + 0])/255f;
                    colorTmp.g = ((float) data[row + color_offset + 1])/255f;
                    colorTmp.r = ((float) data[row + color_offset + 2])/255f;
                }

                position.Add(positionTmp);
                color.Add(colorTmp);
                if (!float.IsInfinity(positionTmp.x) && !float.IsInfinity(positionTmp.y) && !float.IsInfinity(positionTmp.z) && !float.IsNaN(positionTmp.x) && !float.IsNaN(positionTmp.y) && !float.IsNaN(positionTmp.z))
                {
                    
                    Debug.Log("(" + positionTmp.x + " - " + positionTmp.y + " - " + positionTmp.z + ")");
                }
            }
            /*
            for(int i = 0; i < position.Count; i++)
                Debug.Log(i + ": (" + position[i].x + " - " + position[i].y + " - " + position[i].z + ")");
*/
            pcd.position = position.ToArray();
            pcd.color = color.ToArray();
        }
        /*
        for(int i = 0; i < pcd.position.Length; i++)
        {
            if (float.IsInfinity(pcd.position[i].x) || float.IsInfinity(pcd.position[i].y) || float.IsInfinity(pcd.position[i].z) || float.IsNaN(pcd.position[i].x) || float.IsNaN(pcd.position[i].y) || float.IsNaN(pcd.position[i].z))
            {
                Debug.Log(pcd.position[i]);
            }      
        }
        */
        pcd.points = pcd.position.Length;
        ready = true;
        callback.Invoke(pcd);

    }

    private static PCDData ParseHeader(byte[] bytes)
    {
        PCDData pcd = new PCDData();
        
        string headerText = "";

        int max = bytes.Length;

        // Find DATA field (end of header)
        Regex rg = new Regex("[\r\n]DATA\\s(\\S*)\\s");
        Match m;
        int j = 0;
        do{
            headerText += Convert.ToChar(bytes[j++]);
            m = rg.Match(headerText);
        }while(j < max && !m.Success);
        
        pcd.data = m.Groups[1].Value;
        pcd.headerLen = m.Index + m.Groups[0].Length;
        pcd.str = headerText.Substring(0, pcd.headerLen);
        
        // Remove comments
        pcd.str = Regex.Replace(pcd.str, "\\#.*\\n", "");
        
        // Retrieve value from fields
        m = Regex.Match(pcd.str, "VERSION (.*)");
        if(m.Success)
        {
            Debug.Log(m.Groups[1].Value);
            pcd.version = float.Parse("0" + m.Groups[1].Value);
        }
            
        m = Regex.Match(pcd.str, "FIELDS (.*)");
        if(m.Success)
        {
            pcd.fields = m.Groups[1].Value.Split(' ');
        }
        m = Regex.Match(pcd.str, "SIZE (.*)");
        if(m.Success)
        {
            string[] positionTmp = m.Groups[1].Value.Split(' ');

            int[] val = new int[positionTmp.Length];

            for(int i = 0; i < positionTmp.Length; i++)
            {
                val[i] = int.Parse(positionTmp[i]);
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
            string[] positionTmp = m.Groups[1].Value.Split(' ');

            int[] val = new int[positionTmp.Length];

            for(int i = 0; i < positionTmp.Length; i++)
            {
                val[i] = int.Parse(positionTmp[i]);
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
    internal Vector3[] position;
    internal Color[] color;

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
