#region Assembly TextMeshPro-2017.2-1.0.56-Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// TextMeshPro-2017.2-1.0.56-Runtime.dll
#endregion

using System;
using UnityEngine;
using UnityEngine.UI;

namespace TMPro
{
    public class TextMeshProUGUI : Component
    {
        public string text;
        public float preferredHeight;

        public object fontSize;
        public object fontStyle;
        public RectTransform rectTransform;
        public int lineSpacing;
        public float preferredWidth;

        public Font font { get { throw new Exception("this feature requires TextMesh Pro. please get it from AssetStore then delete UUebView/Extension/Delete_This_If_Use_TextMeshPro_Extension.dll."); } set { } }

        public TextInfo GetTextInfo(string text)
        {
            throw new NotImplementedException();
        }

        public class TextInfo
        {
            public readonly LineInfo[] lineInfo;
            public int lineCount;

            public class LineInfo
            {
                public int lineHeight;
                public int firstCharacterIndex;
                public int lastCharacterIndex;
                public float length;
            }
        }
    }
}