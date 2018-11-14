/Applications/Unity2017.4.14f1/Unity.app/Contents/MonoBleedingEdge/bin/mcs \
	-r:/Applications/Unity2017.4.14f1/Unity.app/Contents/UnityExtensions/Unity/GUISystem/UnityEngine.UI.dll \
	-r:/Applications/Unity2017.4.14f1/Unity.app/Contents/Managed/UnityEngine.dll \
	-target:library \
	-recurse:'Assets/UUebView/Core/*.cs' \
	-out:./UUebView.dll \
	-sdk:2
