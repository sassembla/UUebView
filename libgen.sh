/Applications/Unity2017.2.0p4/Unity.app/Contents/MonoBleedingEdge/bin/mcs \
	-r:/Applications/Unity2017.2.0p4/Unity.app/Contents/UnityExtensions/Unity/GUISystem/UnityEngine.UI.dll \
	-r:/Applications/Unity2017.2.0p4/Unity.app/Contents/Managed/UnityEngine.dll \
	-target:library \
	-recurse:'Assets/UUebView/Core/*.cs' \
	-out:./UUebView.dll \
	-sdk:2

# TMPro本来のdllを使う必要はない。
# -r:"Assets/TextMesh Pro/Plugins/Runtime DLL/TextMeshPro-2017.2-1.0.56-Runtime.dll" \