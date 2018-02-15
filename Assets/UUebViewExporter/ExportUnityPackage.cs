using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ExportUnityPackage
{
    [MenuItem("Window/UUebViewEditor/Export")]
    public static void Export()
    {
        File.Copy("UUebView.dll", "Assets/UUebView/UUebView.dll");
        AssetDatabase.Refresh();

        var packageTargetFilePath = new List<string>();
        packageTargetFilePath.Add("Assets/UUebView/UUebView.dll");

        var editorFilePaths = Directory.GetFiles("Assets/UUebView/Editor");
        packageTargetFilePath.AddRange(editorFilePaths);

        packageTargetFilePath.Add("Assets/UUebView/UUebViewComponent.cs");

        AssetDatabase.ExportPackage(packageTargetFilePath.ToArray(), "UUebView.unitypackage", ExportPackageOptions.IncludeDependencies);

        File.Delete("Assets/UUebView/UUebView.dll");
        AssetDatabase.Refresh();
    }
}