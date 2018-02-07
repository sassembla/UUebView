/Applications/Unity2017.2.0p4/Unity.app/Contents/MonoBleedingEdge/bin/mcs \
	-r:/Applications/Unity2017.2.0p4/Unity.app/Contents/Managed/UnityEngine.dll \
	-target:library \
	-recurse:'ExtensionSource/*.cs' \
	-out:ExtensionSource/Delete_This_If_Use_TextMeshPro_Extension.dll \
	-sdk:2

cp ExtensionSource/Delete_This_If_Use_TextMeshPro_Extension.dll Assets/UUebView/Extension/Delete_This_If_Use_TextMeshPro_Extension.dll