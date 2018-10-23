using System.Collections;
using System.Collections.Generic;
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
        
    }

    #endregion

    void Start ()
    {
        //32 bit signed int per channel
        //4 bit per site, up to Z16
        //capable for 16 sites
        //4096 x 4096 x 16 - 128^4 sites
        Debug.Log(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGInt));
        Instance = this;


    }
	
	void Update ()
    {
	    if (!m_bInitialed)
	    {
            G4096_4D.InitialDispaly(SimTxtStep, SimTxtEnergy, SimTxtStartButton, ShowImage);
            OnStopSimulation();
	        m_bInitialed = true;
	    }
	}

    #region Calculator

    public CCalculator G4096_4D;
    private ECalc m_eCalcType = ECalc.G4096_4D;

    private static int[][] m_iSizes =
    {
        new int[]{2, 4, 8, 16, 32, 64 },
        new int[]{1, 2, 3, 4, 5, 6 },

        new int[]{2, 4, 8, 16, 32, 64, 128 },
        new int[]{4, 8, 16, 32, 64, 128, 256 },
        new int[]{4, 8, 16, 32, 64, 128, 256 },
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

    private void OnStopSimulation()
    {
        OnStopSimulationGroupTable();
        OnStopSimConfig();
        OnStopSimSim();
    }

    #region Global



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

        GroupTableTitle.text = "Group Table: " + sPath;
    }

    private void LoadGroupTableLua(string sFileName)
    {
        m_pGroupTable = CUtility.BuildGroupTable(sFileName);
        if (null != m_pGroupTable)
        {
            GroupTableTitle.text = "Group Table: " + sFileName;
            Debug.Log(m_pGroupTable.GetGroupTableDesc());
            G4096_4D.SetGroupTable(m_pGroupTable);
        }
    }

    private void LoadGroupTableGTFile(string sFileName)
    {
        m_pGroupTable = new CGroupTable();
        byte[] data = File.ReadAllBytes(sFileName);
        m_pGroupTable.FillWithByte(sFileName, data);
        Debug.Log(m_pGroupTable.GetGroupTableDesc());
        GroupTableTitle.text = "Group Table: " + sFileName;

        G4096_4D.SetGroupTable(m_pGroupTable);
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
        int iSiteNumber = 0;
        int.TryParse(SiteNumberInput.text, out iSiteNumber);
        for (int i = 0; i < m_iSizes[(int) m_eCalcType].Length; ++i)
        {
            if (iSiteNumber == m_iSizes[2 * (int) m_eCalcType][i])
            {
                G4096_4D.SetSiteNumber(new [] { m_iSizes[2 * (int)m_eCalcType + 1][i] , m_iSizes[2 * (int)m_eCalcType][i], m_iSizes[2 * (int)m_eCalcType][i] * m_iSizes[2 * (int)m_eCalcType][i] });
                ConfigTableTitle.text = "Configuration file: White";
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
        G4096_4D.SetWhiteConfigure();
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
    public Text SimTxtEnergy;
    public Text SimTxtStartButton;
    public Button SimBtStart;
    public InputField SimInputBetaX;
    public InputField SimInputBetaT;
    public InputField SimInputIteration;
    public InputField SimInputEnergyStep;

    public void OnBtStartButton()
    {
        if (G4096_4D.IsSimulating())
        {
            G4096_4D.PauseSimulate();
            OnStopSimulation();
            return;
        }

        int iItera, iEnergyStep;
        float fBetaX, fBetaT;

        int.TryParse(SimInputIteration.text, out iItera);
        int.TryParse(SimInputEnergyStep.text, out iEnergyStep);
        float.TryParse(SimInputBetaX.text, out fBetaX);
        float.TryParse(SimInputBetaT.text, out fBetaT);

        if (iItera < 1)
        {
            ShowMessage("Iteration must be >= 1!");
            return;
        }

        OnStartSimulation();
        G4096_4D.StartSimulate(fBetaT, fBetaX, iItera, iEnergyStep);
    }

    private void OnStartSimSim()
    {
        SimInputBetaX.interactable = false;
        SimInputBetaT.interactable = false;
        SimInputIteration.interactable = false;
        SimInputEnergyStep.interactable = false;
        SimBtStart.interactable = true;
    }

    private void OnStopSimSim()
    {
        SimInputBetaX.interactable = true;
        SimInputBetaT.interactable = true;
        SimInputIteration.interactable = true;
        SimInputEnergyStep.interactable = true;
        SimBtStart.interactable = m_bSiteNumberSet && m_bGroupTableSet;
    }

    #endregion

    #region Show

    public RawImage ShowImage;
    public Material ShowMat;

    #endregion

    #endregion
}
