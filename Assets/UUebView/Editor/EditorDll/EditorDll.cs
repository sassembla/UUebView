
using UnityEditor;
using UnityEngine;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UUebView
{
    public class EditorDll
    {
        [MenuItem("Window/UUebView/Enable TextMesh Pro Plugin")]
        public static void EnableTMPro()
        {
            var path = "Assets/TextMesh Pro/Plugins/Runtime DLL/TextMeshPro-2017.2-1.0.56-Runtime.dll";
            Debug.Log("ここで、tmpro側のilinfoを書き換える。path:" + path);
            var assm = AssemblyDefinition.ReadAssembly(path);
            assm.Name = new AssemblyNameDefinition("TextMeshPro", new System.Version("0.0.0.0"));
            assm.Write(path);
        }
    }
}