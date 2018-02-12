
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
            {
                var path = "Assets/TextMesh Pro/Plugins/Runtime DLL/TextMeshPro-2017.2-1.0.56-Runtime.dll";
                Debug.Log("ここで、tmpro側のilinfoを書き換える。path:" + path);
                var assm = AssemblyDefinition.ReadAssembly(path);
                var metadata = assm.MetadataToken;
                Debug.Log("assm:" + assm.Name + " metadata:" + metadata);
                assm.Name = new AssemblyNameDefinition("TextMeshPro", new System.Version("0.0.0.0"));
                assm.Write(path);
            }

            {
                var path = "Assets/UUebView/Extension/Delete_This_If_Use_TextMeshPro_Extension.dll";
                Debug.Log("read. path:" + path);
                var assm = AssemblyDefinition.ReadAssembly(path);
                var metadata = assm.MetadataToken;
                Debug.Log("assm:" + assm.Name + " metadata:" + metadata);
                assm.Name = new AssemblyNameDefinition("TextMeshPro", new System.Version("0.0.0.0"));
                assm.Write(path);
            }


        }
    }
}