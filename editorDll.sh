/Applications/Unity2017.2.0p4/Unity.app/Contents/MonoBleedingEdge/bin/mcs \
	-r:/Applications/Unity2017.2.0p4/Unity.app/Contents/Managed/UnityEngine.dll \
	-r:/Applications/Unity2017.2.0p4/Unity.app/Contents/Managed/UnityEditor.dll \
	-r:/Applications/Unity2017.2.0p4/Unity.app/Contents/Mono/lib/mono/2.0/Mono.Cecil.dll \
	-target:library \
	-recurse:'Assets/UUebView/Editor/EditorDll/*.cs' \
	-out:UUebViewEditor.dll \
	-sdk:2
