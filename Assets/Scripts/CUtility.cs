using System.Collections;
using System.Collections.Generic;
using System.IO;
using LuaInterface;
using UnityEngine;

public class CGroupTable
{
    public enum GroupTableType
    {
        Lua,
        GTFile,
    }

    public string m_sFileName = "";
    public GroupTableType m_eType = GroupTableType.Lua;

    public short m_iM;
    public short m_iN; //m + n - 1 < 4096
    public short[,] m_MI; //max is 4096 x 4096
    public float[] m_EI;
    public short[] m_IG;

    public string GetGroupTableDesc()
    {
        string sRet = "";
        sRet += string.Format("m={0},n={1}\n", m_iM, m_iN);
        for (int i = 0; i < m_iM + m_iN - 1; ++i)
        {
            string sLine = "";
            for (int j = 0; j < m_iM + m_iN - 1; ++j)
            {
                sLine = sLine + m_MI[i, j].ToString() + " ";
            }
            sRet = sRet + sLine + "\n";
        }
        string sLine2 = "EI: ";
        for (int i = 0; i < m_iM; ++i)
        {
            sLine2 = sLine2 + m_EI[i].ToString() + " ";
        }
        sRet = sRet + sLine2 + "\n";
        sLine2 = "IG: ";
        for (int i = 0; i < m_IG.Length; ++i)
        {
            sLine2 = sLine2 + m_IG[i].ToString() + " ";
        }
        sRet = sRet + sLine2 + "\n";
        return sRet;
    }

    public byte[] GetGroupTableData()
    {
        using (BinaryWriter brw =
            new BinaryWriter(new MemoryStream()))
        {
            brw.Write(m_iM);
            brw.Write(m_iN);
            brw.Write((short)m_IG.Length);
            for (int i = 0; i < m_iM + m_iN - 1; ++i)
            {
                for (int j = 0; j < m_iM + m_iN - 1; ++j)
                {
                    brw.Write(m_MI[i, j]);
                }
            }
            for (int i = 0; i < m_iM; ++i)
            {
                brw.Write(m_EI[i]);
            }
            for (int i = 0; i < m_IG.Length; ++i)
            {
                brw.Write(m_IG[i]);
            }
            brw.Flush();

            int iLength = (int)brw.BaseStream.Length;
            using (BinaryReader binReader =
                new BinaryReader(brw.BaseStream))
            {
                binReader.BaseStream.Position = 0;
                byte[] bytes = binReader.ReadBytes(iLength);
                brw.BaseStream.Read(bytes, 0, iLength);
                return bytes;
            }
        }
    }

    public void FillWithByte(string sFileName, byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        {
            ms.Position = 0;
            BinaryReader bwr = new BinaryReader(ms);
            m_iM = bwr.ReadInt16();
            m_iN = bwr.ReadInt16();
            short gnumber = bwr.ReadInt16();
            m_MI = new short[m_iM + m_iN - 1, m_iM + m_iN - 1];
            m_EI = new float[m_iM];
            m_IG = new short[gnumber];

            for (int i = 0; i < m_iM + m_iN - 1; ++i)
            {
                for (int j = 0; j < m_iM + m_iN - 1; ++j)
                {
                    m_MI[i, j] = bwr.ReadInt16();
                }
            }
            for (int i = 0; i < m_iM; ++i)
            {
                m_EI[i] = bwr.ReadSingle();
            }
            for (int i = 0; i < gnumber; ++i)
            {
                m_IG[i] = bwr.ReadInt16();
            }

            ms.Close();
            m_eType = GroupTableType.GTFile;

        }
    }
}

public static class CUtility
{
    public static int[] GetUpperTwoExp(int iNumber)
    {
        int iRet = 1;
        while ((1 << iRet) < iNumber)
        {
            iRet++;
        }
        return new [] {iRet, (1 << iRet)};
    }

    public static CGroupTable BuildGroupTable(string sLuaFileName)
    {
        string sLuaCode = File.ReadAllText(sLuaFileName);
        LuaScriptMgr mgr = new LuaScriptMgr();
        mgr.DoString(sLuaCode);

        LuaFunction func1 = mgr.GetLuaFunction("GetGroupElementNumber");
        if (null == func1)
        {
            CManager.ShowMessage("GetGroupElementNumber function cannot been found.");
            mgr.Destroy();
            return null;
        }
        object[] r1 = func1.Call();
        if (2 != r1.Length || !(r1[0] is double && r1[1] is double) )
        {
            string sRetInfo = r1.Length + " values:";
            for (int i = 0; i < r1.Length; ++i)
            {
                sRetInfo += (null == r1[i] ? "null" : r1[i].GetType().Name) + ",";
            }
            CManager.ShowMessage("GetGroupElementNumber must return two integers, now return is " + sRetInfo);
            mgr.Destroy();
            return null;
        }

        int m = Mathf.RoundToInt((float)(double)r1[0]);
        int n = Mathf.RoundToInt((float)(double)r1[1]);
        Debug.Log(string.Format("table m:{0} and n:{1}", m, n));
        if (m + n - 1 >= 4096)
        {
            CManager.ShowMessage(string.Format("mus thave m + n - 1 < 4096, but now m={0}, n={1}", m, n));
            mgr.Destroy();
            return null;
        }

        LuaFunction func2 = mgr.GetLuaFunction("GetIGTableNumber");
        if (null == func2)
        {
            CManager.ShowMessage("GetIGTableNumber function cannot been found.");
            mgr.Destroy();
            return null;
        }
        object[] r2 = func2.Call();
        if (1 != r2.Length || !(r2[0] is double))
        {
            string sRetInfo = r2.Length + " values:";
            for (int i = 0; i < r2.Length; ++i)
            {
                sRetInfo += (null == r2[i] ? "null" : r2[i].GetType().Name) + ",";
            }
            CManager.ShowMessage("GetIGTableNumber must return one integer, now return is " + sRetInfo);
            mgr.Destroy();
            return null;
        }
        int ig = Mathf.RoundToInt((float)(double)r2[0]);

        CGroupTable gt = new CGroupTable
        {
            m_sFileName = "",
            m_eType = CGroupTable.GroupTableType.Lua,

            m_iM = (short)m,
            m_iN = (short)n,
            m_MI = new short[m + n - 1, m + n - 1],
            m_EI = new float[m],
            m_IG = new short[ig],
        };

        //Fill MT
        LuaFunction func3 = mgr.GetLuaFunction("GetMTTable");
        if (null == func3)
        {
            CManager.ShowMessage("GetMTTable function cannot been found.");
            mgr.Destroy();
            return null;
        }
        for (int i = 0; i < m + n - 1; ++i)
        {
            for (int j = 0; j < m + n - 1; ++j)
            {
                object[] r3 = func3.Call2(i + 1, j + 1);
                if (1 != r3.Length || !(r3[0] is double))
                {
                    string sRetInfo = r3.Length + " values:";
                    for (int k = 0; k < r3.Length; ++i)
                    {
                        sRetInfo += (null == r3[k] ? "null" : r3[k].GetType().Name) + ",";
                    }
                    CManager.ShowMessage("GetMTTable must return one integer, now return is " + sRetInfo);
                    mgr.Destroy();
                    return null;
                }
                
                gt.m_MI[i, j] = (short)(Mathf.RoundToInt((float)(double)r3[0]) - 1);
            }
        }

        //Fill EI
        LuaFunction func4 = mgr.GetLuaFunction("GetETTable");
        if (null == func4)
        {
            CManager.ShowMessage("GetETTable function cannot been found.");
            mgr.Destroy();
            return null;
        }
        for (int i = 0; i < m; ++i)
        {
            object[] r4 = func4.Call(i + 1);
            if (1 != r4.Length || !(r4[0] is double))
            {
                string sRetInfo = r4.Length + " values:";
                for (int k = 0; k < r4.Length; ++i)
                {
                    sRetInfo += (null == r4[k] ? "null" : r4[k].GetType().Name) + ",";
                }
                CManager.ShowMessage("GetETTable must return one float, now return is " + sRetInfo);
                mgr.Destroy();
                return null;
            }
            gt.m_EI[i] = (float)(double)r4[0];
        }

        //Fill IG
        LuaFunction func5 = mgr.GetLuaFunction("GetIGTable");
        if (null == func5)
        {
            CManager.ShowMessage("GetIGTable function cannot been found.");
            mgr.Destroy();
            return null;
        }
        for (int i = 0; i < ig; ++i)
        {
            object[] r5 = func5.Call(i + 1);
            if (1 != r5.Length || !(r5[0] is double))
            {
                string sRetInfo = r5.Length + " values:";
                for (int k = 0; k < r5.Length; ++i)
                {
                    sRetInfo += (null == r5[k] ? "null" : r5[k].GetType().Name) + ",";
                }
                CManager.ShowMessage("GetIGTable must return one integer, now return is " + sRetInfo);
                mgr.Destroy();
                return null;
            }
            gt.m_IG[i] = (short)(Mathf.RoundToInt((float)(double)r5[0]) - 1);
        }
        mgr.Destroy();
        return gt;
    }

    public static void FillExponentTexture(int iSize, RenderTexture rt)
    {
        
    }
}
