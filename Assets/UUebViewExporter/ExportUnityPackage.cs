using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;


public class ExportUnityPackage
{
    [MenuItem("Window/UUebViewEditor/Export")]
    public static void Export()
    {
        File.Copy("UUebView.dll", "Assets/UUebView/UUebView.dll");

        var packageTargetFilePath = new List<string>();

        var editorFilePaths = Directory.GetFiles("Assets/UUebView/Editor");
        packageTargetFilePath.AddRange(editorFilePaths);

        // GeneratedResourcesのDefaultを足す。
        var defaultGeneratedResourcesPaths = Directory.GetFiles("Assets/UUebView/GeneratedResources/Resources/Views/Default/");
        packageTargetFilePath.AddRange(defaultGeneratedResourcesPaths);

        // Editorに入っているオリジナルprefabも足す。
        packageTargetFilePath.Add("Assets/UUebView/GeneratedResources/Resources/Views/Default/Editor/Default.prefab");

        // コンポーネントコードを足す。
        packageTargetFilePath.Add("Assets/UUebView/UUebViewComponent.cs");

        // TMProのpluginコードをzipにして足す。
        var fileToCompress = new FileInfo("Assets/UUebView/UUebViewPlugins/TMProPlugin.cs");
        using (FileStream originalFileStream = fileToCompress.OpenRead())
        {
            if ((File.GetAttributes(fileToCompress.FullName) &
               FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ".gz")
            {
                using (FileStream compressedFileStream = File.Create(fileToCompress.FullName + ".gz"))
                {
                    using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                    {
                        byte[] buffer = new byte[10240];
                        int read;
                        while ((read = originalFileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            compressionStream.Write(buffer, 0, read);
                        }
                    }
                }
            }
        }

        // リフレッシュを行って生成系のファイルをunitypackageに足せるようにする。
        AssetDatabase.Refresh();

        packageTargetFilePath.Add("Assets/UUebView/UUebView.dll");
        packageTargetFilePath.Add("Assets/UUebView/UUebViewPlugins/TMProPlugin.cs.gz");

        // export unitypackage.
        AssetDatabase.ExportPackage(packageTargetFilePath.ToArray(), "UUebView.unitypackage", ExportPackageOptions.IncludeDependencies);


        // 不要なdllを消す
        File.Delete("Assets/UUebView/UUebView.dll");
        AssetDatabase.Refresh();
    }
}