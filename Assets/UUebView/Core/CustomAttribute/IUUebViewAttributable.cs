using System;
using System.Collections.Generic;
using UnityEngine;

namespace UUebView
{
    public interface IUUebViewAttributable
    {
        // 現在のページを指定のHTMLでリロードする
        Action<string> HTMLReloadAct { get; set; }

        Action<string, Action<object>, Action> ResourceDownloadAct { get; set; }

        void OnInitialize(string key, object value);

        void OnLayouted();
    }
}