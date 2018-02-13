/Applications/Unity2017.2.0p4/Unity.app/Contents/MonoBleedingEdge/bin/mcs \
	-r:/Applications/Unity2017.2.0p4/Unity.app/Contents/UnityExtensions/Unity/GUISystem/UnityEngine.UI.dll \
	-r:/Applications/Unity2017.2.0p4/Unity.app/Contents/Managed/UnityEngine.dll \
	-target:library \
	-recurse:'ExtensionSource/*.cs' \
	-out:ExtensionSource/TextMeshPro-2017.2-1.0.56-Runtime.dll \
	-sdk:2

cp ExtensionSource/TextMeshPro-2017.2-1.0.56-Runtime.dll Assets/UUebView/Extension/Delete_This_If_Use_TextMeshPro_Extension.dll