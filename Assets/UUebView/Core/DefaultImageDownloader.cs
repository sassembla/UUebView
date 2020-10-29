using System;
using System.Collections;
using System.Collections.Generic;

namespace UUebView
{
    public class DefaultImageDownloader
    {
        private readonly ResourceLoader resLoader;
        private readonly List<string> downloadingUris = new List<string>();
        public readonly Action<IEnumerator> LoadParallel;

        public DefaultImageDownloader(Action<IEnumerator> loadParallel, ResourceLoader resLoader)
        {
            this.LoadParallel = loadParallel;
            this.resLoader = resLoader;
        }

        // 画像読み込みリクエストを行う
        public void RequestLoadImage(string uri)
        {
            // 同一のuriで既にリクエストが行われている場合は何もしない
            if (downloadingUris.Contains(uri))
            {
                return;
            }

            // 読込中uriとしてlistに詰める
            downloadingUris.Add(uri);

            LoadParallel(LoadImageAsync(uri));
        }

        private IEnumerator LoadImageAsync(string uri)
        {
            var cor = resLoader.LoadImageAsync(uri);
            while (cor.MoveNext())
            {
                yield return null;
            }

            // ResourceLoader.LoadImageAsyncが完了した段階で読み込み完了
            // もう一度同一uriのリクエストが行われた場合、ResourceLoader側が画像をキャッシュしているので
            // IEnumeratorは即座に完了する
            downloadingUris.Remove(uri);
        }

        // downloadingUrisが1以上ならDefaultImageDownloaderは画像読み込み処理を行っている
        public bool IsRunning()
        {
            return downloadingUris.Count > 0;
        }

        public void Reset()
        {
            downloadingUris.Clear();
        }
    };
}