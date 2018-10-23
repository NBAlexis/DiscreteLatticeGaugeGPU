using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ECalc
{
    G4096_4D,
    G16_4D,
    G4096_3D,
    G16_3D,
}

public class CCalculator : MonoBehaviour
{
    public ECalc CalcType;
    public ComputeShader CmpShader;

    public bool IsSimulating() { return m_bSimulating; }
    private bool m_bSimulating = false;
    private int m_iEnergyStep = 0;
    private int m_iStep = 0;

    //==============================================
    // data on memory
    //==============================================
    private int m_iKernelSetGroupData = -1;
    private int m_iKernelInput = -1;
    private int m_iKernelOutput = -1;
    private int m_iKernelSetRandom = -1;
    private int m_iKernelInitialWhiteConfiguration = -1;
    private int m_iKernelGetSmallNumber = -1;
    private int m_iKernelGenerateConstants = -1;

    private int m_iKernelCalculate64 = -1;
    private int m_iKernelCalculate256 = -1;

    private int m_iKernelDisplay16 = -1;
    private int m_iKernelDisplay256 = -1;
    private int m_iKernelDisplay1024 = -1;

    private int m_iKernelCalcUsing = -1;
    private int m_iKernelDispUsing = -1;
    private int m_iCalcDiv = 1;
    private int m_iDispDiv = 1;

    private RenderTexture m_pConfiguration = null;
    private RenderTexture m_pDisplayConfiguration = null;
    private RenderTexture m_pRandom = null;

    private RenderTexture m_pMT = null;
    private RenderTexture m_pEI = null;
    private RenderTexture m_pIG = null;

    private ComputeBuffer m_pIntBuffer = null;
    private ComputeBuffer m_pFloatBuffer = null;

    // { n, 1 << n }
    private int[] m_iSiteNumber = null;

    //==============================================
    // Initial
    //==============================================
    public void Start()
    {
        m_iKernelSetGroupData = CmpShader.FindKernel("SetGroupTables");
        m_iKernelInput = CmpShader.FindKernel("SetConfigurationByInput");
        m_iKernelOutput = CmpShader.FindKernel("ReadConfiguration");
        m_iKernelSetRandom = CmpShader.FindKernel("SetRandom");
        m_iKernelInitialWhiteConfiguration = CmpShader.FindKernel("InitialWhiteConfiguration");
        m_iKernelGetSmallNumber = CmpShader.FindKernel("GetSmallData");
        m_iKernelGenerateConstants = CmpShader.FindKernel("GenerateConstants");

        m_iKernelCalculate64 = CmpShader.FindKernel("Calculate64");
        m_iKernelCalculate256 = CmpShader.FindKernel("Calculate256");

        m_iKernelDisplay16 = CmpShader.FindKernel("Display16");
        m_iKernelDisplay256 = CmpShader.FindKernel("Display256");
        m_iKernelDisplay1024 = CmpShader.FindKernel("Display1024");

        SetRandom();
    }

    public void Update()
    {
        if (m_bSimulating)
        {
            CmpShader.Dispatch(m_iKernelCalcUsing, m_iSiteNumber[2] / m_iCalcDiv, m_iSiteNumber[2] / m_iCalcDiv, 1);
            CmpShader.Dispatch(m_iKernelDispUsing, m_iSiteNumber[2] / m_iDispDiv, m_iSiteNumber[2] / m_iDispDiv, 1);

            ++m_iStep;

            if (null != m_txStep)
            {
                m_txStep.text = m_iStep.ToString();
            }
        }
    }

    //==============================================
    // Display
    //==============================================
    private Text m_txStep = null;
    private Text m_txEnergy = null;
    private Text m_txStartButton = null;
    private RawImage m_pResImage = null;

    public void InitialDispaly(Text step, Text energy, Text startButton, RawImage image)
    {
        m_txStep = step;
        m_txEnergy = energy;
        m_txStartButton = startButton;
        m_pResImage = image;
    }

    //==============================================
    // Input
    //==============================================
    public void SetSiteNumber(int[] iSiteNumber)
    {
        switch (CalcType)
        {
            case ECalc.G4096_4D:
                {
                    if (1 == iSiteNumber[0])
                    {
                        m_iKernelCalcUsing = m_iKernelCalculate64;
                        m_iCalcDiv = 4;
                        m_iKernelDispUsing = m_iKernelDisplay16;
                        m_iDispDiv = 4;
                    }
                    else
                    {
                        m_iKernelCalcUsing = m_iKernelCalculate256;
                        m_iCalcDiv = 8;
                        if (2 == iSiteNumber[0]) //4 sites, 4x4x4x4=16 x 16 sites
                        {
                            m_iKernelDispUsing = m_iKernelDisplay256;
                            m_iDispDiv = 16;
                        }
                        else
                        {
                            m_iKernelDispUsing = m_iKernelDisplay1024;
                            m_iDispDiv = 32;
                        }
                    }
                    CmpShader.SetInt("iSiteShift", iSiteNumber[0]);
                    m_iSiteNumber = iSiteNumber;

                    CmpShader.Dispatch(m_iKernelGenerateConstants, 1, 1, 1);
                    SetWhiteConfigure();
                }
                break;
        }
    }

    public void SetGroupTable(CGroupTable gt)
    {
        if (null == gt)
        {
            return;
        }

        m_pIntBuffer = new ComputeBuffer((gt.m_iM + gt.m_iN - 1) * (gt.m_iM + gt.m_iN - 1) + gt.m_IG.Length, 4);
        m_pFloatBuffer = new ComputeBuffer(gt.m_iM, 4);

        uint[] intData = new uint[(gt.m_iM + gt.m_iN - 1) * (gt.m_iM + gt.m_iN - 1) + gt.m_IG.Length];
        for (int i = 0; i < gt.m_iM + gt.m_iN - 1; ++i)
        {
            for (int j = 0; j < gt.m_iM + gt.m_iN - 1; ++j)
            {
                intData[i*(gt.m_iM + gt.m_iN - 1) + j] = (uint)gt.m_MI[i, j];
            }
        }
        for (int i = 0; i < gt.m_IG.Length; ++i)
        {
            intData[(gt.m_iM + gt.m_iN - 1) * (gt.m_iM + gt.m_iN - 1) + i] = (uint)gt.m_IG[i];
        }
        m_pIntBuffer.SetData(intData);

        float[] floatData = new float[gt.m_iM];
        for (int i = 0; i < gt.m_iM; ++i)
        {
            floatData[i] = gt.m_EI[i];
        }
        m_pFloatBuffer.SetData(floatData);

        CmpShader.SetInt("iM", gt.m_iM);
        CmpShader.SetInt("iN", gt.m_iN);
        CmpShader.SetInt("iIG", gt.m_IG.Length);

        int[] iMTTextureSize = CUtility.GetUpperTwoExp(gt.m_iM + gt.m_iN - 1);
        int[] iEITextureSize = CUtility.GetUpperTwoExp(gt.m_iM);
        int[] iIGTextureSize = CUtility.GetUpperTwoExp(gt.m_IG.Length);
        m_pMT = new RenderTexture(iMTTextureSize[1], iMTTextureSize[1], 1, RenderTextureFormat.RInt);
        m_pMT.enableRandomWrite = true;
        m_pMT.Create();
        m_pEI = new RenderTexture(iEITextureSize[1], 1, 1, RenderTextureFormat.RFloat);
        m_pEI.enableRandomWrite = true;
        m_pEI.Create();
        m_pIG = new RenderTexture(iIGTextureSize[1], 1, 1, RenderTextureFormat.RInt);
        m_pIG.enableRandomWrite = true;
        m_pIG.Create();

        CmpShader.SetTexture(m_iKernelSetGroupData, "MT", m_pMT);
        CmpShader.SetTexture(m_iKernelSetGroupData, "EI", m_pEI);
        CmpShader.SetTexture(m_iKernelSetGroupData, "IG", m_pIG);

        CmpShader.SetTexture(m_iKernelCalcUsing, "MT", m_pMT);
        CmpShader.SetTexture(m_iKernelCalcUsing, "EI", m_pEI);
        CmpShader.SetTexture(m_iKernelCalcUsing, "IG", m_pIG);

        CmpShader.SetBuffer(m_iKernelSetGroupData, "IntDataBuffer", m_pIntBuffer);
        CmpShader.SetBuffer(m_iKernelSetGroupData, "FloatDataBuffer", m_pFloatBuffer);
        
        CmpShader.Dispatch(m_iKernelSetGroupData, 4, 4, 1);

        m_pIntBuffer.Release();
        m_pIntBuffer = null;
        m_pFloatBuffer.Release();
        m_pFloatBuffer = null;

        //Group table reset, need to reset configuration
        SetWhiteConfigure();
    }

    public void SetConfigurate(short[] data)
    {

    }

    public void SetWhiteConfigure()
    {
        if (null != m_pConfiguration)
        {
            m_pConfiguration.Release();
            m_pConfiguration = null;
        }

        switch (CalcType)
        {
            case ECalc.G4096_4D:
                {
                    m_pConfiguration = new RenderTexture(m_iSiteNumber[2], m_iSiteNumber[2], 1, RenderTextureFormat.RGInt);
                    m_pConfiguration.enableRandomWrite = true;
                    m_pConfiguration.Create();
                    m_pDisplayConfiguration = new RenderTexture(m_iSiteNumber[2], m_iSiteNumber[2], 1, RenderTextureFormat.ARGB32);
                    m_pDisplayConfiguration.enableRandomWrite = true;
                    m_pDisplayConfiguration.Create();

                    CmpShader.SetTexture(m_iKernelInitialWhiteConfiguration, "Configuration", m_pConfiguration);
                    CmpShader.SetTexture(m_iKernelCalcUsing, "Configuration", m_pConfiguration);
                    CmpShader.SetTexture(m_iKernelInput, "Configuration", m_pConfiguration);
                    CmpShader.SetTexture(m_iKernelOutput, "Configuration", m_pConfiguration);
                    CmpShader.SetTexture(m_iKernelCalcUsing, "Random", m_pRandom);

                    CmpShader.SetTexture(m_iKernelDispUsing, "Configuration", m_pConfiguration);
                    CmpShader.SetTexture(m_iKernelDispUsing, "Display", m_pDisplayConfiguration);

                    CmpShader.Dispatch(m_iKernelInitialWhiteConfiguration, m_iSiteNumber[2] / 4, m_iSiteNumber[2] / 4, 1);

                    if (null != m_pResImage)
                    {
                        m_pResImage.material.SetTexture("_MainTex", m_pDisplayConfiguration);
                        m_pResImage.SetAllDirty();
                    }
                }
                break;
        }
    }

    public void SetRandom()
    {
        m_pRandom = new RenderTexture(2048, 2048, 1, RenderTextureFormat.RFloat);
        m_pRandom.enableRandomWrite = true;
        m_pRandom.Create();

        m_pFloatBuffer = new ComputeBuffer(2048 * 2048, 4);
        float[] fData = new float[2048 * 2048];
        for (int i = 0; i < 2048*2048; ++i)
        {
            fData[i] = Random.Range(0.0f, 1.0f);
        }
        m_pFloatBuffer.SetData(fData);
        CmpShader.SetBuffer(m_iKernelSetRandom, "FloatDataBuffer", m_pFloatBuffer);
        CmpShader.SetTexture(m_iKernelSetRandom, "Random", m_pRandom);
        CmpShader.Dispatch(m_iKernelSetRandom, 2048 / 32, 2048 /32, 1);

        m_pFloatBuffer.Release();
        m_pFloatBuffer = null;
    }

    //==============================================
    // Output
    //==============================================
    public uint[] ReadSmallData()
    {
        m_pIntBuffer = new ComputeBuffer(5, 4);
        m_pIntBuffer.SetData(new uint[]{ 0, 0, 0, 0, 0});
        CmpShader.SetBuffer(m_iKernelGetSmallNumber, "IntDataBuffer", m_pIntBuffer);
        CmpShader.Dispatch(m_iKernelGetSmallNumber, 1, 1, 1);
        uint[] dataOut = {0, 0, 0, 0, 0};
        m_pIntBuffer.GetData(dataOut);

        Debug.Log(string.Format("m={0},n={1},ig={2},site={3},step={4}", dataOut[0], dataOut[1], dataOut[2], dataOut[3], dataOut[4]));

        m_pIntBuffer.Release();
        m_pIntBuffer = null;

        return dataOut;
    }

    //==============================================
    // Simulate
    //==============================================

    public void StartSimulate(float fBetaT, float fBetaX, int iItera, int iEnergyStep)
    {
        if (m_bSimulating)
        {
            return;
        }

        m_iEnergyStep = iEnergyStep;
        CmpShader.SetInt("iIteration", iItera);
        CmpShader.SetInt("iRandom", Random.Range(0, 2048 * 2048 - 1));
        CmpShader.SetFloat("fBetaX", fBetaX);
        CmpShader.SetFloat("fBetaT", fBetaT);

        m_bSimulating = true;
        if (null != m_txStartButton)
        {
            m_txStartButton.text = "Pause Simulation";
        }
    }

    public void PauseSimulate()
    {
        if (!m_bSimulating)
        {
            return;
        }
        m_bSimulating = false;
        if (null != m_txStartButton)
        {
            m_txStartButton.text = "Start Simulation";
        }
    }
}
