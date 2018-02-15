using System;
using System.Collections.Generic;
using UnityEngine;

namespace UUebView
{
    public interface IPluggable
    {
        IEnumerator<ChildPos> TextLayoutCoroutine(Component sourceComponent, TagTree textTree, string text, ViewCursor textViewCursor, Func<InsertType, TagTree, ViewCursor> insertion = null);
        float GetDefaultHeightOfContainerText(Component textComponent);
        Component TextComponent(GameObject prefab, string tagsName);
        void SetText(GameObject targetGameObject, string text);
    }
}