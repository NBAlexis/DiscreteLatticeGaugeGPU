using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class PostBuildFileCopy
{
    private static readonly string[] expellFileNameList = {};

    private static bool Expelled(string sFileName)
    {
        for (int i = 0; i < expellFileNameList.Length; ++i)
        {
            if (sFileName.ToLower().Contains(expellFileNameList[i]))
            {
                return true;
            }
        }
        return false;
    }

    private static void CreateIfNeed(string sFullPath)
    {
        string[] allparts = sFullPath.Split('/');
        for (int i = 1; i < allparts.Length - 1; ++i)
        {
            string sFolderName = allparts[0];
            for (int j = 0; j < i; ++j)
            {
                sFolderName = sFolderName + "/" + allparts[j + 1];
            }
            DirectoryInfo targetDir1 = new DirectoryInfo(sFolderName);
            if (!targetDir1.Exists)
            {
                targetDir1.Create();
            }
        }
    }

    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        pathToBuiltProject = pathToBuiltProject.Replace("\\", "/");
        string sFolder = pathToBuiltProject.Substring(0, pathToBuiltProject.LastIndexOf("/") + 1);
        DirectoryInfo targetDir = new DirectoryInfo(sFolder);

        DirectoryInfo targetDir1 = new DirectoryInfo(sFolder + "/LuaScript");
        DirectoryInfo targetDir2 = new DirectoryInfo(sFolder + "/WorkingData");
        DirectoryInfo targetDir3 = new DirectoryInfo(sFolder + "/Doc");

        if (targetDir1.Exists)
        {
            targetDir1.Delete(true);
        }
        if (targetDir2.Exists)
        {
            targetDir2.Delete(true);
        }
        if (targetDir3.Exists)
        {
            targetDir3.Delete(true);
        }

        targetDir.CreateSubdirectory("LuaScript");
        foreach (FileInfo f in new DirectoryInfo(Application.dataPath + "/LuaScript").GetFiles("*.lua", SearchOption.AllDirectories))
        {
            if (!Expelled(f.FullName))
            {
                string sFullName = f.FullName;
                string sToBeReplace = Application.dataPath + "/LuaScript/";
                sToBeReplace = sToBeReplace.Replace("\\", "/");
                sFullName = sFullName.Replace("\\", "/");

                sFullName = sFullName.Replace(sToBeReplace, sFolder + "/LuaScript/");
                CreateIfNeed(sFullName);
                File.Copy(f.FullName, sFullName);
            }
        }

        targetDir.CreateSubdirectory("WorkingData");
        foreach (FileInfo f in new DirectoryInfo(Application.dataPath + "/WorkingData").GetFiles("*.gt", SearchOption.AllDirectories))
        {
            if (!Expelled(f.FullName))
            {
                string sFullName = f.FullName;
                string sToBeReplace = Application.dataPath + "/WorkingData/";
                sToBeReplace = sToBeReplace.Replace("\\", "/");
                sFullName = sFullName.Replace("\\", "/");

                sFullName = sFullName.Replace(sToBeReplace, sFolder + "/WorkingData/");
                CreateIfNeed(sFullName);
                File.Copy(f.FullName, sFullName);
            }
        }

        targetDir.CreateSubdirectory("Doc");
        foreach (FileInfo f in new DirectoryInfo(Application.dataPath + "/Doc").GetFiles("*.pdf", SearchOption.AllDirectories))
        {
            if (!Expelled(f.FullName))
            {
                string sFullName = f.FullName;
                string sToBeReplace = Application.dataPath + "/Doc/";
                sToBeReplace = sToBeReplace.Replace("\\", "/");
                sFullName = sFullName.Replace("\\", "/");

                sFullName = sFullName.Replace(sToBeReplace, sFolder + "/Doc/");
                CreateIfNeed(sFullName);
                File.Copy(f.FullName, sFullName);
            }
        }
    }
}
