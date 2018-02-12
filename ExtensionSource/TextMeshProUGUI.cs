#region Assembly TextMeshPro-Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// TextMeshPro-Runtime.dll
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
        public Font font;
        public object fontSize;
        public object fontStyle;
        public RectTransform rectTransform;
        public int lineSpacing;
        public float preferredWidth;

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