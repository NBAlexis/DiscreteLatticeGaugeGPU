using System;
using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.UI;


public class CManager : MonoBehaviour
{
    private static CManager Instance = null;
    private bool m_bInitialed = false;
#if UNITY_EDITOR
    public static readonly string _outfolder = "/";
#else
    public static readonly string _outfolder = "/../";
#endif

    #region Message

    public GameObject MessagePlane;
    public Text MessageTx;

    public static void ShowMessage(string sMessage)
    {
        Debug.Log(sMessage);

        if (null == Instance)
        {
            return;
        }
        Instance.ShowMessageFunc(sMessage);
    }

    private void ShowMessageFunc(string sMessage)
    {
        MessageTx.text = sMessage;
        MessagePlane.SetActive(true);
    }

    public void OnBtMessageBoxOK()
    {
        MessagePlane.SetActive(false);
    }

    #endregion

    // ReSharper disable once UnusedMember.Local
    void Start()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = -1;

        //32 bit signed int per channel
        //4 bit per site, up to Z16
        //capable for 16 sites
        //4096 x 4096 x 16 - 128^4 sites
        Debug.Log(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGInt));
        Instance = this;

        m_pCalcs = new [] { G4096_4D, null, G4096_3D, null };
        //RenderTexture r2test = new RenderTexture(16384, 16384, 1, RenderTextureFormat.RGFloat);
        //r2test.enableRandomWrite = true;
        //r2test.Create();
    }

    // ReSharper disable once UnusedMember.Local
    void Update()
    {
        if (!m_bInitialed)
        {
            m_pCalcs[(int)m_eCalcType].InitialDispaly(SimTxtStep, SimTxtEnergy, SimTxtStartButton, ShowImage, this);
            OnStopSimulation();
            m_bInitialed = true;
        }
    }

    #region Calculator

    public CCalculator G4096_4D;
    public CCalculator G4096_3D;
    private ECalc m_eCalcType = ECalc.G4096_4D;

    private CCalculator[] m_pCalcs = null;

    private static int[][] m_iSizes =
    {
        //G4096_4D
        new []{2, 4, 8, 16, 32, 64 },
        new []{1, 2, 3, 4,  5,  6 },

        new []{4, 16, 64, 256 },
        new []{2,  4,  6,   8 },

        //G4096_3D
        new []{4, 16, 64, 256 },
        new []{2,  4,  6,   8 },

        new []{4, 8, 16, 32, 64, 128, 256 },
    };

    #endregion

    #region UI Message

    private bool m_bSiteNumberSet = false;
    private bool m_bGroupTableSet = false;

    private void OnStartSimulation()
    {
        OnStartSimulationGroupTable();
        OnStartSimConfig();
        OnStartSimSim();
    }

    public void OnStopSimulation()
    {
        OnStopSimulationGroupTable();
        OnStopSimConfig();
        OnStopSimSim();
    }

    #region Global

    // ReSharper disable once UnassignedField.Global
    public Dropdown DropGlobalOption;

    public void OnDropDownGlobalOptionChanged()
    {
        ECalc eNew = (ECalc)DropGlobalOption.value;
        if (eNew == m_eCalcType)
        {
            return;
        }

        if (eNew == ECalc.G4096_4D || eNew == ECalc.G4096_3D)
        {
            m_eCalcType = eNew;
            m_bGroupTableSet = false;
            m_bSiteNumberSet = false;
            m_pCalcs[(int)m_eCalcType].InitialDispaly(SimTxtStep, SimTxtEnergy, SimTxtStartButton, ShowImage, this);
            OnStopSimulation();
        }
        else
        {
            DropGlobalOption.value = (int)m_eCalcType;
            ShowMessage("This type is not supported yet.");
        }
    }

    #endregion

    #region IO

    public string SaveTextResult(string sContent)
    {
        string sFileName = Application.dataPath + _outfolder + "Output/" + DateTime.Now.ToFileTimeUtc().ToString() + ".txt";
        File.WriteAllText(sFileName, sContent);
        return sFileName;
    }

    #endregion

    #region Group Table

    public Text GroupTableTitle;
    public Button GroupTableLoadBt;
    public Button GroupTableSaveBt;

    private CGroupTable m_pGroupTable = null;

    public void OnBtLoadGroupTable()
    {
        string[] sPath = StandaloneFileBrowser.OpenFilePanel("Choose a script or a .gt file",
            Application.dataPath + _outfolder,
            new[] { new ExtensionFilter("Group Table Files", "gt"), new ExtensionFilter("Lua Script", "lua")},
            false);

        if (sPath.Length > 0 && !string.IsNullOrEmpty(sPath[0]))
        {
            if (sPath[0].Contains(".lua"))
            {
                LoadGroupTableLua(sPath[0]);
            }

            if (sPath[0].Contains(".gt"))
            {
                LoadGroupTableGTFile(sPath[0]);
            }
        }

        if (null != m_pGroupTable)
        {
            m_bGroupTableSet = true;
            OnStopSimulation();
        }
    }

    public void OnBtSaveGroupTable()
    {
        if (null == m_pGroupTable)
        {
            ShowMessage("Invalid group table!");
            return;
        }
        string sPath = StandaloneFileBrowser.SaveFilePanel("Choose a .gt file to save",
            Application.dataPath + _outfolder,
            "new group table",
            new[] { new ExtensionFilter("Group Table Files", "gt") });

        File.WriteAllBytes(sPath, m_pGroupTable.GetGroupTableData());

        GroupTableTitle.text = "Group Table:\n" + sPath;
    }

    private void LoadGroupTableLua(string sFileName)
    {
        m_pGroupTable = CUtility.BuildGroupTable(sFileName);
        if (null != m_pGroupTable)
        {
            GroupTableTitle.text = "Group Table:\n" + sFileName;
            Debug.Log(m_pGroupTable.GetGroupTableDesc());
            m_pCalcs[(int)m_eCalcType].SetGroupTable(m_pGroupTable);
        }
    }

    private void LoadGroupTableGTFile(string sFileName)
    {
        m_pGroupTable = new CGroupTable();
        byte[] data = File.ReadAllBytes(sFileName);
        m_pGroupTable.FillWithByte(sFileName, data);
        Debug.Log(m_pGroupTable.GetGroupTableDesc());
        GroupTableTitle.text = "Group Table:\n" + sFileName;

        m_pCalcs[(int)m_eCalcType].SetGroupTable(m_pGroupTable);
    }

    private void OnStartSimulationGroupTable()
    {
        GroupTableLoadBt.interactable = false;
        GroupTableSaveBt.interactable = false;
    }

    private void OnStopSimulationGroupTable()
    {
        GroupTableLoadBt.interactable = m_bSiteNumberSet;
        GroupTableSaveBt.interactable = (null != m_pGroupTable);
    }

    #endregion

    #region Configuration

    public Text ConfigTableTitle;
    public InputField SiteNumberInput;
    public Button SiteBt;
    public Button ConfigInitalBt;
    public Button ConfigLoadBt;
    public Button ConfigSaveBt;

    public void OnBtSiteNumber()
    {
        int iSiteNumber;
        int.TryParse(SiteNumberInput.text, out iSiteNumber);
        for (int i = 0; i < m_iSizes[(int) m_eCalcType].Length; ++i)
        {
            if (iSiteNumber == m_iSizes[2 * (int) m_eCalcType][i])
            {
                m_pCalcs[(int)m_eCalcType].SetSiteNumber(new []
                {
                    m_iSizes[2 * (int)m_eCalcType + 1][i],
                    m_iSizes[2 * (int)m_eCalcType][i]
                });
                ConfigTableTitle.text = "Configuration file:\nWhite";
                m_bSiteNumberSet = true;
                OnStopSimulation();
                return;
            }
        }

        ShowMessage("Site number not supported!");
    }

    public void OnBtConfigLoad()
    {

    }

    public void OnBtConfigWhite()
    {
        m_pCalcs[(int)m_eCalcType].SetWhiteConfigure();
        ConfigTableTitle.text = "Configuration file:\nWhite";
    }

    public void OnBtConfigSave()
    {

    }

    private void OnStartSimConfig()
    {
        SiteBt.interactable = false;
        ConfigInitalBt.interactable = false;
        ConfigLoadBt.interactable = false;
        ConfigSaveBt.interactable = false;
        SiteNumberInput.interactable = false;
    }

    private void OnStopSimConfig()
    {
        SiteBt.interactable = true;
        SiteNumberInput.interactable = true;
        ConfigInitalBt.interactable = m_bSiteNumberSet;
        ConfigLoadBt.interactable = m_bSiteNumberSet;
        ConfigSaveBt.interactable = m_bSiteNumberSet;
    }

    #endregion

    #region Simulation

    public Text SimTxtStep;
    public InputField SimTxtEnergy;
    public Text SimTxtStartButton;
    public Button SimBtStart;
    public Button SimBtResetStopStep;
    public InputField SimInputBetaX;
    public InputField SimInputBetaT;
    public InputField SimInputIteration;
    public InputField SimInputEnergyStep;
    public InputField SimInputStopStep;

    public void OnBtStartButton()
    {
        if (m_pCalcs[(int)m_eCalcType].IsSimulating())
        {
            m_pCalcs[(int)m_eCalcType].PauseSimulate();
            OnStopSimulation();
            return;
        }

        if (CycleUseCycle.isOn)
        {
            int iItera, iTargetStep, iSkipStep, iStableStep;
            float fBetaFrom, fBetaTo;

            int.TryParse(SimInputIteration.text, out iItera);
            int.TryParse(CycleInputTotalSteps.text, out iTargetStep);
            int.TryParse(CycleInputSkipSteps.text, out iSkipStep);
            int.TryParse(CycleInputStableSteps.text, out iStableStep);
            float.TryParse(CycleInputBetaXFrom.text, out fBetaFrom);
            float.TryParse(CycleInputBetaXFromTo.text, out fBetaTo);

            if (iItera < 1)
            {
                ShowMessage("Iteration must be >= 1!");
                return;
            }

            OnStartSimulation();
            m_pCalcs[(int)m_eCalcType].StartSimulateUsingCycle(new Vector2(fBetaFrom, fBetaTo), iTargetStep, iSkipStep, iStableStep, iItera);
        }
        else
        {
            int iItera, iEnergyStep, iStopStep;
            float fBeta;

            int.TryParse(SimInputIteration.text, out iItera);
            int.TryParse(SimInputEnergyStep.text, out iEnergyStep);
            int.TryParse(SimInputStopStep.text, out iStopStep);
            //float.TryParse(SimInputBetaX.text, out fBetaX);
            float.TryParse(SimInputBetaT.text, out fBeta);

            if (iItera < 1)
            {
                ShowMessage("Iteration must be >= 1!");
                return;
            }

            OnStartSimulation();
            m_pCalcs[(int)m_eCalcType].StartSimulate(fBeta, iItera, iEnergyStep, iStopStep);
        }
    }

    public void OnBtResetEnergyHistory()
    {
        m_pCalcs[(int)m_eCalcType].ResetEnergyHistory();
    }

    public void OnBtResetStep()
    {
        m_pCalcs[(int)m_eCalcType].ResetStep();
    }

    public void OnBtResetStopStep()
    {
        int iStopStep;
        int.TryParse(SimInputStopStep.text, out iStopStep);
        m_pCalcs[(int)m_eCalcType].ResetStopStep(iStopStep);
    }

    private void OnStartSimSim()
    {
        if (CycleUseFixedBeta.isOn)
        {
            SimInputBetaX.interactable = false;
            SimInputBetaT.interactable = false;
            SimInputIteration.interactable = false;
            SimInputEnergyStep.interactable = false;
            SimBtStart.interactable = true;
            SimBtResetStopStep.interactable = true;
        }

        if (CycleUseCycle.isOn)
        {
            SimInputIteration.interactable = false;
            CycleInputBetaXFrom.interactable = false;
            CycleInputBetaXFromTo.interactable = false;
            CycleInputBetaYFrom.interactable = false;
            CycleInputBetaYFromTo.interactable = false;
            CycleInputTotalSteps.interactable = false;
            CycleInputSkipSteps.interactable = false;
            CycleInputStableSteps.interactable = false;
        }

        CycleUseFixedBeta.interactable = false;
        CycleUseCycle.interactable = false;
    }

    private void OnStopSimSim()
    {
        if (CycleUseFixedBeta.isOn)
        {
            SimInputBetaX.interactable = true;
            SimInputBetaT.interactable = true;
            SimInputIteration.interactable = true;
            SimInputEnergyStep.interactable = true;
            SimInputStopStep.interactable = true;
            SimBtResetStopStep.interactable = false;

            CycleInputBetaXFrom.interactable = false;
            CycleInputBetaXFromTo.interactable = false;
            CycleInputBetaYFrom.interactable = false;
            CycleInputBetaYFromTo.interactable = false;
            CycleInputTotalSteps.interactable = false;
            CycleInputSkipSteps.interactable = false;
            CycleInputStableSteps.interactable = false;

            CycleBtTerminate.interactable = false;
        }

        if (CycleUseCycle.isOn)
        {
            SimInputBetaX.interactable = false;
            SimInputBetaT.interactable = false;
            SimInputIteration.interactable = !m_pCalcs[(int)m_eCalcType].HasCycle();
            SimInputEnergyStep.interactable = false;
            SimInputStopStep.interactable = false;
            SimBtResetStopStep.interactable = false;

            CycleInputBetaXFrom.interactable = !m_pCalcs[(int)m_eCalcType].HasCycle();
            CycleInputBetaXFromTo.interactable = !m_pCalcs[(int)m_eCalcType].HasCycle();
            CycleInputBetaYFrom.interactable = !m_pCalcs[(int)m_eCalcType].HasCycle();
            CycleInputBetaYFromTo.interactable = !m_pCalcs[(int)m_eCalcType].HasCycle();
            CycleInputTotalSteps.interactable = !m_pCalcs[(int)m_eCalcType].HasCycle();
            CycleInputSkipSteps.interactable = !m_pCalcs[(int)m_eCalcType].HasCycle();
            CycleInputStableSteps.interactable = !m_pCalcs[(int)m_eCalcType].HasCycle();

            CycleUseFixedBeta.interactable = !m_pCalcs[(int)m_eCalcType].HasCycle();
            CycleUseCycle.interactable = !m_pCalcs[(int)m_eCalcType].HasCycle();

            CycleBtTerminate.interactable = m_pCalcs[(int)m_eCalcType].HasCycle();
        }

        SimBtStart.interactable = m_bSiteNumberSet && m_bGroupTableSet;
    }

    #endregion

    #region Show

    public RawImage ShowImage;
    public Material ShowMat;

    #endregion

    #region Log

    // ReSharper disable once UnassignedField.Global
    public Text LogTxArea;
    private const int LogLength = 8192;

    public void LogData(string sData)
    {
        Debug.Log(string.Format("{0}: {1}", Time.time, sData));
        sData = sData + "\n" + LogTxArea.text;
        sData = sData.Replace("\n\n", "\n");
        if (sData.Length > LogLength)
        {
            sData = sData.Substring(0, LogLength);
        }
        LogTxArea.text = sData;
    }

    #endregion

    #region Therm Cycle

    public Toggle CycleUseFixedBeta;
    public Toggle CycleUseCycle;
    public InputField CycleInputBetaXFrom;
    public InputField CycleInputBetaXFromTo;
    public InputField CycleInputBetaYFrom;
    public InputField CycleInputBetaYFromTo;
    public InputField CycleInputTotalSteps;
    public InputField CycleInputSkipSteps;
    public InputField CycleInputStableSteps;
    public Button CycleBtTerminate;

    public void OnToggleUseFixedBeta()
    {
        if (CycleUseFixedBeta.isOn)
        {
            CycleUseCycle.isOn = false;
            OnStopSimulation();
        }
    }

    public void OnToggleUseCycle()
    {
        if (CycleUseCycle.isOn)
        {
            CycleUseFixedBeta.isOn = false;
            OnStopSimulation();
        }
    }

    public void OnBtTerminateCycle()
    {
        if (m_pCalcs[(int)m_eCalcType].HasCycle())
        {
            m_pCalcs[(int)m_eCalcType].TerminateSimulateCycle();
            OnStopSimulation();
        }
    }

    #endregion

    #endregion
}
