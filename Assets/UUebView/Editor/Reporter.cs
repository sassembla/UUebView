using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UUebView
{
    public class Reporter
    {
        [MenuItem("Window/UUebView/Report Problem With Selected UUebTags")]
        public static void Report()
        {
            // unitypackageを作り出す。
            // defaultと、htmlと、あるならば選択されているUUebTag。
            // htmlを書くと、テキストファイルが作られて、レポート時にファイルとして入る、と言う風にするか。
            // レポートが終わったら消す。
            var tempPath = "Assets/UUebView/Report/Resources/UUebViewReport/report.txt";

            if (!Directory.Exists("Assets/UUebView/Report"))
            {
                Directory.CreateDirectory("Assets/UUebView/Report");
            }

            if (!Directory.Exists("Assets/UUebView/Report/Resources"))
            {
                Directory.CreateDirectory("Assets/UUebView/Report/Resources");
            }

            if (!Directory.Exists("Assets/UUebView/Report/Resources/UUebViewReport"))
            {
                Directory.CreateDirectory("Assets/UUebView/Report/Resources/UUebViewReport");
            }

            var htmlStr = "something";
            File.WriteAllText(tempPath, htmlStr);

            var reportTargetAssetPaths = new List<string>();

            using (new ShouldDeleteFileAtPathContstraint(tempPath))
            {
                // で、テキストを依存に巻き込む。
                reportTargetAssetPaths.Add(tempPath);

                var uuebTagsCandidate = Selection.activeGameObject;
                if (uuebTagsCandidate != null && uuebTagsCandidate.transform.parent.GetComponent<Canvas>() != null)
                {
                    // 選択されている名称のuuebTagsが存在しているはずなので、云々。初めからUUebTagsだけを扱えばいいか。

                }

                // デフォルトもあるはずなので、そちらも。なかったらいらない。
                if (false)
                {

                }
            }
        }

        private class ShouldDeleteFileAtPathContstraint : IDisposable
        {
            private string targetFilePath;
            public ShouldDeleteFileAtPathContstraint(string targetFilePath)
            {
                AssetDatabase.Refresh();
                this.targetFilePath = targetFilePath;
            }

            private bool disposedValue = false;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // dispose.
                        File.Delete(targetFilePath);
                        AssetDatabase.Refresh();
                    }
                    disposedValue = true;
                }
            }

            void IDisposable.Dispose()
            {
                Dispose(true);
            }
        }
    }
}