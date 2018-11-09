using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum ECalc
{
    G4096_4D,
    G16_4D,
    G4096_3D,
    G16_3D,
}

public enum ESimulateType
{
    Fixed,
    Cycle,
}


public class CCalculator : MonoBehaviour
{
    public ECalc CalcType;
    public ComputeShader CmpShader;
    private CManager m_pManager = null;
    private ESimulateType m_eSimType = ESimulateType.Fixed;

    public bool IsSimulating() { return m_bSimulating; }
    private bool m_bSimulating = false;
    private int m_iEnergyStep = 0;
    private int m_iStep = 0;
    private int m_iStopStep = 0;

    //==============================================
    // data on memory
    //==============================================
    private int m_iKernelSetGroupData = -1;
    private int m_iKernelOutput = -1;
    private int m_iKernelInitialWhiteConfiguration = -1;
    private int m_iKernelGetSmallNumber = -1;
    private int m_iKernelGenerateConstants = -1;
    private int m_iKernelGetEnergyOut = -1;
    private int m_iKernelResetEnergyHistory = -1;

    private int m_iKernelCalculate16 = -1;
    private int m_iKernelCalculate256 = -1;

    private int m_iKernelDisplay16 = -1;
    private int m_iKernelDisplay64 = -1;
    private int m_iKernelDisplay256 = -1;
    private int m_iKernelDisplay1024 = -1;

    private int m_iKernelEnergy16 = -1;
    private int m_iKernelEnergy256= -1;
    private int m_iKernelEnergy1024 = -1;

    private int m_iKernelCalcUsing = -1;
    private int m_iKernelDispUsing = -1;
    private int m_iKernelEnergyUsing = -1;
    private int m_iCalcDiv = 1;
    private int m_iDispDiv = 1;
    private int m_iEnergyDiv = 1;

    private RenderTexture m_pConfiguration = null;
    private RenderTexture m_pDisplayConfiguration = null;
    private RenderTexture m_pEnergyTable = null;

    private RenderTexture m_pMT = null;
    private RenderTexture m_pEI = null;
    private RenderTexture m_pIG = null;

    private ComputeBuffer m_pIntBuffer = null;
    private ComputeBuffer m_pFloatBuffer = null;
    private ComputeBuffer m_pWorkingIntBuffer = null;
    private ComputeBuffer m_pWorkingFloatBuffer = null;

    // { n, 1 << n }
    private int[] m_iSiteNumber = null;

    //==============================================
    // Initial
    //==============================================
    public void Start()
    {
        m_iKernelSetGroupData = CmpShader.FindKernel("SetGroupTables");
        m_iKernelOutput = CmpShader.FindKernel("ReadConfiguration");
        m_iKernelInitialWhiteConfiguration = CmpShader.FindKernel("InitialWhiteConfiguration");
        m_iKernelGetSmallNumber = CmpShader.FindKernel("GetSmallData");
        m_iKernelGenerateConstants = CmpShader.FindKernel("GenerateConstants");
        m_iKernelGetEnergyOut = CmpShader.FindKernel("GetEnergyOut");
        m_iKernelResetEnergyHistory = CmpShader.FindKernel("ResetEnergyHistory");

        m_iKernelCalculate16 = CmpShader.FindKernel("Calculate16");
        m_iKernelCalculate256 = CmpShader.FindKernel("Calculate256");

        m_iKernelDisplay16 = CmpShader.FindKernel("Display16");
        m_iKernelDisplay64 = CmpShader.FindKernel("Display64");
        m_iKernelDisplay256 = CmpShader.FindKernel("Display256");
        m_iKernelDisplay1024 = CmpShader.FindKernel("Display1024");

        m_iKernelEnergy16 = CmpShader.FindKernel("CalculateEnergy16");
        m_iKernelEnergy256 = CmpShader.FindKernel("CalculateEnergy256");
        m_iKernelEnergy1024 = CmpShader.FindKernel("CalculateEnergy1024");

        m_pWorkingIntBuffer = new ComputeBuffer(128, 4);
        m_pWorkingIntBuffer.SetData(new uint[128]);
        m_pWorkingFloatBuffer = new ComputeBuffer(128, 4);
        m_pWorkingFloatBuffer.SetData(new float[128]);

        CmpShader.SetBuffer(m_iKernelOutput, "WorkingIntDataBuffer", m_pWorkingIntBuffer);
        CmpShader.SetBuffer(m_iKernelGenerateConstants, "WorkingIntDataBuffer", m_pWorkingIntBuffer);
        CmpShader.SetBuffer(m_iKernelGetEnergyOut, "WorkingIntDataBuffer", m_pWorkingIntBuffer);

        CmpShader.SetBuffer(m_iKernelGetEnergyOut, "WorkingFloatDataBuffer", m_pWorkingFloatBuffer);
        CmpShader.SetBuffer(m_iKernelResetEnergyHistory, "WorkingFloatDataBuffer", m_pWorkingFloatBuffer);
    }

    public void Update()
    {
        if (m_bSimulating)
        {
            switch (m_eSimType)
            {
                #region Fixed
                case ESimulateType.Fixed:
                    {
                        CmpShader.SetFloat("fRandomSeed", Random.Range(0.0f, 1.0f));
                        CmpShader.SetInt("iLatticeStartX", Random.Range(0, m_iSiteNumber[2] - 1));
                        CmpShader.SetInt("iLatticeStartY", Random.Range(0, m_iSiteNumber[2] - 1));

                        CmpShader.Dispatch(m_iKernelCalcUsing, m_iSiteNumber[2] / m_iCalcDiv, m_iSiteNumber[2] / m_iCalcDiv, 1);
                        CmpShader.Dispatch(m_iKernelEnergyUsing, m_iSiteNumber[2] / m_iEnergyDiv, m_iSiteNumber[2] / m_iEnergyDiv, 1);

                        if ((m_iStep%m_iEnergyStep) != 0)
                        {
                            //accumulate energy and calculate average
                            CmpShader.Dispatch(m_iKernelGetEnergyOut, 1, 1, 1);
                        }
                            
                        ++m_iStep;

                        if (null != m_txStep)
                        {
                            m_txStep.text = m_iStep.ToString();
                        }

                        if (m_iStep != 0 && (m_iStep % m_iEnergyStep) == 0)
                        {
                            CmpShader.Dispatch(m_iKernelDispUsing, m_iSiteNumber[2] / m_iDispDiv, m_iSiteNumber[2] / m_iDispDiv, 1);

                            m_pFloatBuffer = new ComputeBuffer(2, 4);
                            m_pFloatBuffer.SetData(new float[] { 0.0f, 0.0f });
                            CmpShader.SetBuffer(m_iKernelGetEnergyOut, "FloatDataBuffer", m_pFloatBuffer);
                            CmpShader.Dispatch(m_iKernelGetEnergyOut, 1, 1, 1);
                            float[] dataOut = { 0.0f, 0.0f };
                            m_pFloatBuffer.GetData(dataOut);
                            m_lstEnergyStepList.Add(m_iStep);
                            m_lstEnergyList.Add(dataOut[1]);
                            m_lstEnergyList.Add(dataOut[0]);
                            if (null != m_pManager)
                            {
                                m_pManager.LogData(string.Format("step:{0} e:{1}", m_iStep, dataOut[0]));
                            }
                            if (null != m_txEnergy)
                            {
                                m_txEnergy.text = string.Format("Energy: {0}", dataOut[0]);
                            }
                            
                            m_pFloatBuffer.Release();
                            m_pFloatBuffer = null;

                            CmpShader.Dispatch(m_iKernelResetEnergyHistory, 1, 1, 1);
                        }

                        if (m_iStopStep > 1 && 0 == (m_iStep % m_iStopStep))
                        {
                            PauseSimulate();
                            if (null != m_pManager)
                            {
                                m_pManager.OnStopSimulation();
                                string sStp = "\nStep=\n{";
                                string sE = "\nE=\n{";
                                string sEav = "\nEav=\n{";
                                for (int i = 0; i < m_lstEnergyStepList.Count; ++i)
                                {
                                    sStp += m_lstEnergyStepList[i].ToString();
                                    sE += m_lstEnergyList[2 * i].ToString();
                                    sEav += m_lstEnergyList[2 * i + 1].ToString();
                                    if (i != m_lstEnergyStepList.Count - 1)
                                    {
                                        sStp += ",";
                                        sE += ",";
                                        sEav += ",";
                                    }
                                    else
                                    {
                                        sStp += "}\n";
                                        sE += "}\n";
                                        sEav += "}\n";
                                    }
                                }

                                string sFileName = m_pManager.SaveTextResult(string.Format("Fixed Run at:{0}, with group:{1}\n, size:{7}**4, iteration:{2}, betatx:({3},{4}), stop step:{5}, estep:{6}\n" 
                                    , DateTime.Now
                                    , m_sGroupName
                                    , m_iIter
                                    , m_v2Beta.x
                                    , m_v2Beta.y
                                    , m_iStopStep
                                    , m_iEnergyStep
                                    , m_iSiteNumber[1]) 
                                    + sStp + sE + sEav);
                                CManager.ShowMessage("Data save to:" + sFileName);
                                m_pManager.LogData("Stopped. Data save to:" + sFileName);
                            }
                        }
                    }
                    break;
                #endregion

                #region cycle
                case ESimulateType.Cycle:
                    {
                        if (m_iStepNow == m_iSkipStep && 0 == m_iStableTick)
                        {
                            //just enter the cycle
                            //clear energy history and set buffer
                            CmpShader.Dispatch(m_iKernelResetEnergyHistory, 1, 1, 1);
                            m_lstEnergyList.Clear();

                            //only the first time in here, will have m_iStableTick = 0
                            //all others will have m_iStableTick = 1 -> m_iStableStep
                            m_iStableTick = m_iStableStep + 1;
                        }

                        //we are waiting for it to stable
                        if (m_iStableTick > 1)
                        {
                            //if (null != m_pManager)
                            //{
                            //    m_pManager.LogData(string.Format("step:{0} stable", m_iStableStep + 2 - m_iStableTick));
                            //}
                            CmpShader.SetFloat("fRandomSeed", Random.Range(0.0f, 1.0f));
                            CmpShader.SetInt("iLatticeStartX", Random.Range(0, m_iSiteNumber[2] - 1));
                            CmpShader.SetInt("iLatticeStartY", Random.Range(0, m_iSiteNumber[2] - 1));
                            CmpShader.Dispatch(m_iKernelCalcUsing, m_iSiteNumber[2] / m_iCalcDiv, m_iSiteNumber[2] / m_iCalcDiv, 1);
                            CmpShader.Dispatch(m_iKernelEnergyUsing, m_iSiteNumber[2] / m_iEnergyDiv, m_iSiteNumber[2] / m_iEnergyDiv, 1);
                            CmpShader.Dispatch(m_iKernelGetEnergyOut, 1, 1, 1);
                            --m_iStableTick;
                            break;
                        }

                        //now m_iStableTick = 0 or 1
                        //either m_iStepNow < m_iSkipStep
                        //or we finish skip
                        int iStepNow = m_iStepNow >= m_iSkipStep ? (m_iStepNow - m_iSkipStep) : 0;
                        //for example, totalstep = 3490, beta from 0.01 to 3.5
                        //stepnow go from 0,1,...,3490,3491,3492,...,(2*3490)
                        //when stepnow <= 3490, it is 0.01 + (3.49 / 3490) * stepnow
                        //when stepnow > 3490, it is 2*3490 - stepnow.  for example, stepnow = 3491, it is 3490-1, stepnow = 2*3490, it is 0
                        int iMutipNow = (iStepNow > m_iTargetStep) ? (2 * m_iTargetStep - iStepNow) : iStepNow;
                        if (null != m_txStep)
                        {
                            m_txStep.text = iMutipNow.ToString();
                        }
                        float fCurrentBetaX = m_v3BetaX.x + m_v3BetaX.z * iMutipNow;
                        float fCurrentBetaT = m_v3BetaT.x + m_v3BetaT.z * iMutipNow;

                        CmpShader.SetFloat("fBetaX", fCurrentBetaX);
                        CmpShader.SetFloat("fBetaT", fCurrentBetaT);

                        CmpShader.SetFloat("fRandomSeed", Random.Range(0.0f, 1.0f));
                        CmpShader.SetInt("iLatticeStartX", Random.Range(0, m_iSiteNumber[2] - 1));
                        CmpShader.SetInt("iLatticeStartY", Random.Range(0, m_iSiteNumber[2] - 1));
                        CmpShader.Dispatch(m_iKernelCalcUsing, m_iSiteNumber[2] / m_iCalcDiv, m_iSiteNumber[2] / m_iCalcDiv, 1);
                        CmpShader.Dispatch(m_iKernelDispUsing, m_iSiteNumber[2] / m_iDispDiv, m_iSiteNumber[2] / m_iDispDiv, 1);

                        //when first time in here, we already run for stable
                        if (m_iStepNow >= m_iSkipStep && 1 == m_iStableTick)
                        {
                            CmpShader.Dispatch(m_iKernelEnergyUsing, m_iSiteNumber[2] / m_iEnergyDiv, m_iSiteNumber[2] / m_iEnergyDiv, 1);
                            m_pFloatBuffer = new ComputeBuffer(2, 4);
                            m_pFloatBuffer.SetData(new float[] {0.0f, 0.0f});
                            CmpShader.SetBuffer(m_iKernelGetEnergyOut, "FloatDataBuffer", m_pFloatBuffer);
                            CmpShader.Dispatch(m_iKernelGetEnergyOut, 1, 1, 1);
                            float[] dataOut = {0.0f, 0.0f};
                            m_pFloatBuffer.GetData(dataOut);
                            m_pFloatBuffer.Release();
                            m_pFloatBuffer = null;
                            m_lstEnergyList.Add(dataOut[1]);
                            m_lstEnergyList.Add(dataOut[0]);

                            CmpShader.Dispatch(m_iKernelResetEnergyHistory, 1, 1, 1);

                            if (null != m_pManager)
                            {
                                m_pManager.LogData(string.Format("step:{0} e:{1}", m_iStepNow, dataOut[0]));
                            }
                            m_iStableTick = m_iStableStep + 1;
                        }
                        else
                        {
                            if (null != m_pManager)
                            {
                                m_pManager.LogData(string.Format("step:{0} skipping", m_iStepNow));
                            }
                        }

                        ++m_iStepNow;
                        if (CheckStop())
                        {
                            return;
                        }
                    }
                    break;
                    #endregion
            }

        }
    }

    private bool CheckStop()
    {
        if (m_iStepNow > 2 *m_iTargetStep + m_iSkipStep)
        {
            PauseSimulate();
            if (m_lstEnergyList.Count == 4 * m_iTargetStep + 2)
            {
                string sData = string.Format("Cycle Run at:{0}, group:{1}\n betax={2}:{3}:{4}, betat={5}:{6}:{7}\n iteration={8}, skip={9}, stable={10}, cycle={11}\n Increasing:\n"
                    , DateTime.Now
                    , m_sGroupName
                    , m_v3BetaX.x
                    , m_v3BetaX.z
                    , m_v3BetaX.y
                    , m_v3BetaT.x
                    , m_v3BetaT.z
                    , m_v3BetaT.y
                    , m_iIter
                    , m_iSkipStep
                    , m_iStableStep
                    , m_iTargetStep
                    );

                string sBetaX = "BetaX=\n{";
                string sBetaT = "BetaT=\n{";
                string sE = "E=\n{";
                string sEav = "Eva=\n{";
                for (int i = 0; i <= m_iTargetStep; ++i)
                {
                    int imx = (i > m_iTargetStep) ? (2 * m_iTargetStep - i) : i;

                    sBetaX += (m_v3BetaX.x + m_v3BetaX.z * imx).ToString();
                    sBetaT += (m_v3BetaT.x + m_v3BetaT.z * imx).ToString();
                    sE += m_lstEnergyList[2 * i].ToString();
                    sEav += m_lstEnergyList[2 * i + 1].ToString();
                    if (i != m_iTargetStep)
                    {
                        sBetaX += ",";
                        sBetaT += ",";
                        sE += ",";
                        sEav += ",";
                    }
                    else
                    {
                        sBetaX += "}\n";
                        sBetaT += "}\n";
                        sE += "}\n";
                        sEav += "}\n";
                    }
                }

                sData = sData + sBetaX + sBetaT + sE + sEav + "\nDecreasing:\n";
                sBetaX = "BetaX=\n{";
                sBetaT = "BetaT=\n{";
                sE = "E=\n{";
                sEav = "Eva=\n{";
                for (int i = m_iTargetStep; i <= 2*m_iTargetStep; ++i)
                {
                    int imx = (i > m_iTargetStep) ? (2 * m_iTargetStep - i) : i;

                    sBetaX += (m_v3BetaX.x + m_v3BetaX.z * imx).ToString();
                    sBetaT += (m_v3BetaT.x + m_v3BetaT.z * imx).ToString();
                    sE += m_lstEnergyList[2 * i].ToString();
                    sEav += m_lstEnergyList[2 * i + 1].ToString();
                    if (i != 2 * m_iTargetStep)
                    {
                        sBetaX += ",";
                        sBetaT += ",";
                        sE += ",";
                        sEav += ",";
                    }
                    else
                    {
                        sBetaX += "}\n";
                        sBetaT += "}\n";
                        sE += "}\n";
                        sEav += "}\n";
                    }
                }
                sData = sData + sBetaX + sBetaT + sE + sEav;

                if (null != m_pManager)
                {
                    string sFileName = m_pManager.SaveTextResult(sData);
                    CManager.ShowMessage("Data save to:" + sFileName);
                    m_pManager.LogData("Stopped. Data save to:" + sFileName);
                }
            }
            else
            {
                if (null != m_pManager)
                {
                    CManager.ShowMessage("Data Count Not Correct !");
                    m_pManager.LogData("Stopped");
                }
            }

            if (null != m_pManager)
            {
                m_pManager.OnStopSimulation();
            }
            TerminateSimulateCycle();
            return true;
        }
        return false;
    }

    public void OnDestroy()
    {
        m_pWorkingIntBuffer.Release();
        m_pWorkingFloatBuffer.Release();
    }

    //==============================================
    // Display
    //==============================================
    private Text m_txStep = null;
    private InputField m_txEnergy = null;
    private Text m_txStartButton = null;
    private RawImage m_pResImage = null;

    public void InitialDispaly(Text step, InputField energy, Text startButton, RawImage image, CManager cmanager)
    {
        m_txStep = step;
        m_txEnergy = energy;
        m_txStartButton = startButton;
        m_pResImage = image;
        m_pManager = cmanager;
    }

    //==============================================
    // Input
    //==============================================
    public void SetSiteNumber(int[] iSiteNumber)
    {
        m_iSiteNumber = new int[3];
        m_iSiteNumber[0] = iSiteNumber[0];
        m_iSiteNumber[1] = iSiteNumber[1];

        switch (CalcType)
        {
            #region G40964D
            case ECalc.G4096_4D:
                {
                    if (1 == iSiteNumber[0])
                    {
                        m_iKernelCalcUsing = m_iKernelCalculate16;
                        m_iCalcDiv = 4;
                        m_iKernelDispUsing = m_iKernelDisplay16;
                        m_iDispDiv = 4;
                        m_iKernelEnergyUsing = m_iKernelEnergy16;
                        m_iEnergyDiv = 4;
                    }
                    else
                    {
                        m_iKernelCalcUsing = m_iKernelCalculate256;
                        m_iCalcDiv = 16;

                        
                        if (2 == iSiteNumber[0]) //4 sites, 4x4x4x4=16 x 16 sites
                        {
                            m_iKernelDispUsing = m_iKernelDisplay256;
                            m_iDispDiv = 16;

                            m_iKernelEnergyUsing = m_iKernelEnergy256;
                            m_iEnergyDiv = 16;
                        }
                        else
                        {
                            m_iKernelDispUsing = m_iKernelDisplay1024;
                            m_iDispDiv = 32;

                            m_iKernelEnergyUsing = m_iKernelEnergy1024;
                            m_iEnergyDiv = 32;
                        }
                    }

                    CmpShader.SetBuffer(m_iKernelCalcUsing, "WorkingIntDataBuffer", m_pWorkingIntBuffer);
                    CmpShader.SetBuffer(m_iKernelEnergyUsing, "WorkingIntDataBuffer", m_pWorkingIntBuffer);
                    CmpShader.SetBuffer(m_iKernelEnergyUsing, "WorkingFloatDataBuffer", m_pWorkingFloatBuffer);

                    CmpShader.SetInt("iSiteShift", iSiteNumber[0]);
                    CmpShader.SetInt("iPlaqNumber", 12*iSiteNumber[1]*iSiteNumber[1]*iSiteNumber[1]*iSiteNumber[1]);
                    m_iSiteNumber[2] = iSiteNumber[1] * iSiteNumber[1];

                    CmpShader.Dispatch(m_iKernelGenerateConstants, 1, 1, 1);
                    SetWhiteConfigure();
                }
                break;
            #endregion

            #region G40963D
            case ECalc.G4096_3D:
                {
                    //4x4x4
                    if (2 == iSiteNumber[0])
                    {
                        m_iKernelCalcUsing = m_iKernelCalculate16;
                        m_iCalcDiv = 8;
                        m_iKernelDispUsing = m_iKernelDisplay64;
                        m_iDispDiv = 8;
                        m_iKernelEnergyUsing = m_iKernelEnergy16;
                        m_iEnergyDiv = 8;
                    }
                    else
                    {
                        m_iKernelCalcUsing = m_iKernelCalculate256;
                        m_iCalcDiv = 16;
                        m_iKernelEnergyUsing = m_iKernelEnergy256;
                        m_iEnergyDiv = 16;
                        m_iKernelEnergyUsing = m_iKernelDisplay256;
                        m_iEnergyDiv = 16;
                    }

                    CmpShader.SetBuffer(m_iKernelCalcUsing, "WorkingIntDataBuffer", m_pWorkingIntBuffer);
                    CmpShader.SetBuffer(m_iKernelEnergyUsing, "WorkingIntDataBuffer", m_pWorkingIntBuffer);
                    CmpShader.SetBuffer(m_iKernelEnergyUsing, "WorkingFloatDataBuffer", m_pWorkingFloatBuffer);

                    CmpShader.SetInt("iSiteShift", iSiteNumber[0]);
                    CmpShader.SetInt("iPlaqNumber", 6 * iSiteNumber[1] * iSiteNumber[1] * iSiteNumber[1]);
                    m_iSiteNumber[2] = 1 << (iSiteNumber[0] + (iSiteNumber[0] >> 1));

                    CmpShader.Dispatch(m_iKernelGenerateConstants, 1, 1, 1);
                    SetWhiteConfigure();
                }
                break;
                #endregion
        }
    }

    private string m_sGroupName = "";
    public void SetGroupTable(CGroupTable gt)
    {
        if (null == gt)
        {
            return;
        }
        m_sGroupName = gt.m_sFileName;
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

        CmpShader.SetTexture(m_iKernelEnergyUsing, "MT", m_pMT);
        CmpShader.SetTexture(m_iKernelEnergyUsing, "EI", m_pEI);

        CmpShader.SetBuffer(m_iKernelSetGroupData, "IntDataBuffer", m_pIntBuffer);
        CmpShader.SetBuffer(m_iKernelSetGroupData, "FloatDataBuffer", m_pFloatBuffer);
        
        CmpShader.Dispatch(m_iKernelSetGroupData, 4, 4, 1);
        CmpShader.Dispatch(m_iKernelGenerateConstants, 1, 1, 1);

        m_pIntBuffer.Release();
        m_pIntBuffer = null;
        m_pFloatBuffer.Release();
        m_pFloatBuffer = null;

        //Group table reset, need to reset configuration
        SetWhiteConfigure();
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
            case ECalc.G4096_3D:
                {
                    m_pConfiguration = new RenderTexture(m_iSiteNumber[2], m_iSiteNumber[2], 1, RenderTextureFormat.RGInt);
                    m_pConfiguration.enableRandomWrite = true;
                    m_pConfiguration.Create();
                    m_pDisplayConfiguration = new RenderTexture(m_iSiteNumber[2], m_iSiteNumber[2], 1, RenderTextureFormat.ARGB32);
                    m_pDisplayConfiguration.enableRandomWrite = true;
                    m_pDisplayConfiguration.Create();
                    m_pEnergyTable = new RenderTexture(m_iSiteNumber[2], m_iSiteNumber[2], 1, RenderTextureFormat.RGFloat);
                    m_pEnergyTable.enableRandomWrite = true;
                    m_pEnergyTable.Create();

                    CmpShader.SetTexture(m_iKernelInitialWhiteConfiguration, "Configuration", m_pConfiguration);
                    CmpShader.SetTexture(m_iKernelCalcUsing, "Configuration", m_pConfiguration);
                    CmpShader.SetTexture(m_iKernelOutput, "Configuration", m_pConfiguration);

                    CmpShader.SetTexture(m_iKernelDispUsing, "Configuration", m_pConfiguration);
                    CmpShader.SetTexture(m_iKernelDispUsing, "Display", m_pDisplayConfiguration);

                    CmpShader.SetTexture(m_iKernelEnergyUsing, "Configuration", m_pConfiguration);
                    CmpShader.SetTexture(m_iKernelEnergyUsing, "EnergyTable", m_pEnergyTable);
                    CmpShader.SetTexture(m_iKernelGetEnergyOut, "EnergyTable", m_pEnergyTable);

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
    public void ResetStep()
    {
        m_iStep = 0;
        ResetEnergyHistory();
        if (null != m_txStep)
        {
            m_txStep.text = m_iStep.ToString();
        }
    }

    public void ResetStopStep(int iStopStep)
    {
        m_iStopStep = iStopStep;
    }

    public void ResetEnergyHistory()
    {
        CmpShader.Dispatch(m_iKernelResetEnergyHistory, 1, 1, 1);
        if (null != m_txEnergy)
        {
            m_txEnergy.text = "Energy:";
        }
        m_lstEnergyList.Clear();
        m_lstEnergyStepList.Clear();

        m_pFloatBuffer = new ComputeBuffer(2, 4);
        m_pFloatBuffer.SetData(new float[] { 0.0f, 0.0f });
        CmpShader.SetBuffer(m_iKernelGetEnergyOut, "FloatDataBuffer", m_pFloatBuffer);
        CmpShader.Dispatch(m_iKernelGetEnergyOut, 1, 1, 1);
        m_pFloatBuffer.Release();
    }

    private int m_iIter = 1;
    private Vector2 m_v2Beta = Vector2.zero;
    public void StartSimulate(float fBetaT, float fBetaX, int iItera, int iEnergyStep, int iStopStep)
    {
        if (m_bSimulating)
        {
            return;
        }
        m_eSimType = ESimulateType.Fixed;
        m_iEnergyStep = iEnergyStep;
        m_iStopStep = iStopStep;
        m_iIter = iItera;
        m_v2Beta = new Vector2(fBetaT, fBetaX);
        CmpShader.SetInt("iIteration", iItera);
        CmpShader.SetFloat("fBetaX", fBetaX);
        CmpShader.SetFloat("fBetaT", fBetaT);

        m_bSimulating = true;

        if (0 == m_iStep)
        {
            m_pFloatBuffer = new ComputeBuffer(2, 4);
            m_pFloatBuffer.SetData(new float[] { 0.0f, 0.0f });
            CmpShader.SetBuffer(m_iKernelGetEnergyOut, "FloatDataBuffer", m_pFloatBuffer);
            CmpShader.Dispatch(m_iKernelGetEnergyOut, 1, 1, 1);
            m_pFloatBuffer.Release();
        }

        if (null != m_txStartButton)
        {
            m_txStartButton.text = "Pause Simulation";
        }
    }

    private Vector3 m_v3BetaX = Vector3.zero;
    private Vector3 m_v3BetaT = Vector3.zero;
    private int m_iTargetStep = -1;
    private int m_iStepNow = 0;
    private int m_iSkipStep = 0;
    private int m_iStableStep = 1;
    private int m_iStableTick = 0;
    private readonly List<float> m_lstEnergyList = new List<float>();
    private readonly List<int> m_lstEnergyStepList = new List<int>();

    public bool HasCycle()
    {
        return m_iTargetStep > 1 && (m_iStepNow <= 2 * m_iTargetStep + m_iSkipStep);
    }

    public void StartSimulateUsingCycle(Vector2 vBetaX, Vector2 vBetaT, int iTotalStep, int iSkip, int iStable, int iItera)
    {
        if (m_iTargetStep > 1)
        {
            m_bSimulating = true;
            return;
        }

        if (iTotalStep < 2)
        {
            CManager.ShowMessage("Step must be >= 2 !");
            return;
        }
        if (iSkip < 0)
        {
            CManager.ShowMessage("Skip Step must be >= 0 !");
            return;
        }
        if (iStable < 1)
        {
            CManager.ShowMessage("Stable Step must be >= 1 !");
            return;
        }
        m_eSimType = ESimulateType.Cycle;
        m_iStepNow = 0;
        m_iTargetStep = iTotalStep;
        m_iSkipStep = iSkip;
        m_iStableStep = iStable;
        m_iStableTick = 0;
        m_iIter = iItera;
        CmpShader.SetInt("iIteration", iItera);
        m_v3BetaX = new Vector3(vBetaX.x, vBetaX.y, (vBetaX.y - vBetaX.x) / iTotalStep);
        m_v3BetaT = new Vector3(vBetaT.x, vBetaT.y, (vBetaT.y - vBetaT.x) / iTotalStep);
        m_lstEnergyList.Clear();
        CmpShader.Dispatch(m_iKernelResetEnergyHistory, 1, 1, 1);

        m_pFloatBuffer = new ComputeBuffer(2, 4);
        m_pFloatBuffer.SetData(new float[] { 0.0f, 0.0f });
        CmpShader.SetBuffer(m_iKernelGetEnergyOut, "FloatDataBuffer", m_pFloatBuffer);
        CmpShader.Dispatch(m_iKernelGetEnergyOut, 1, 1, 1);
        m_pFloatBuffer.Release();

        m_bSimulating = true;
        if (null != m_txStartButton)
        {
            m_txStartButton.text = "Pause Simulation";
        }
    }

    public void TerminateSimulateCycle()
    {
        m_iTargetStep = -1;
        m_iSkipStep = -1;
        m_bSimulating = false;
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
