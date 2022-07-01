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
    
        float[] position = new float[pcd.points * 3];

        int color_offset = 0;

        pcd.offset.TryGetValue("rgb", out color_offset);

        if(color_offset == 0)
            pcd.offset.TryGetValue("rgba", out color_offset);

        byte[] color;
        bool colorBool = true;
        if(color_offset > 0)
        {
            colorBool = true;
            color = new byte[pcd.points * 3];
        }

        color = new byte[pcd.points * 3];

        if(pcd.data.Equals("ascii"))
        {
            Debug.Log("ASCII NYI");
            return;
        }

        if(pcd.data.Equals("binary"))
        {
            int row = 0;
            byte[] data = new byte[bytes.Length - pcd.headerLen];

            Array.Copy(bytes, 0, data, 0, data.Length);

            for(int p = 0; p < pcd.points; row += pcd.rowSize, p++)
            {
                byte[] pos = new byte[sizeof(float)];
                Array.Copy(data, row + pcd.offset["x"], pos, 0, sizeof(float));
                Array.Reverse(pos);
                position[p * 3 + 0] = BitConverter.ToSingle(pos, 0);

                Array.Copy(data, row + pcd.offset["y"], pos, 0, sizeof(float));
                Array.Reverse(pos);
                position[p * 3 + 1] = BitConverter.ToSingle(pos, 0);

                Array.Copy(data, row + pcd.offset["z"], pos, 0, sizeof(float));
                Array.Reverse(pos);
                position[p * 3 + 2] = BitConverter.ToSingle(pos, 0);
                
                if(colorBool)
                {
                    color[p * 3 + 2] = data[row + color_offset + 0];
                    color[p * 3 + 1] = data[row + color_offset + 1];
                    color[p * 3 + 0] = data[row + color_offset + 2];
                }

                pcd.position = position;
                pcd.color = color;
            }
        }
        ready = true;
        callback.Invoke(pcd);

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
    internal float[] position;
    internal byte[] color;

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
