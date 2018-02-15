using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace UUebView
{

    public struct ParseError
    {
        public readonly int code;
        public readonly string reason;

        public ParseError(int code, string reason)
        {
            this.code = code;
            this.reason = reason;
        }
    }

    public interface IUUebView
    {
        void AddChild(Transform t);
        void UpdateParentSizeIfExist(Vector2 size);
        GameObject GetGameObject();
        void StartCoroutine(IEnumerator iEnum);
    }

    /**
        動作のコア、UUebViewのインスタンスコントロールを行う。
        固有のresourceLoader、layoutMachine、materializeMachineを持つ。
     */
    public class UUebViewCore
    {
        private Dictionary<string, List<TagTree>> listenerDict = new Dictionary<string, List<TagTree>>();
        public readonly IUUebView view;
        public readonly ResourceLoader resLoader;
        private LayoutMachine layoutMachine;
        private MaterializeMachine materializeMachine;
        private readonly Action<List<ParseError>> onParseFailed;

        private static IPluggable pluggable;

        public UUebViewCore(IUUebView uuebView, ResourceLoader.MyHttpRequestHeaderDelegate requestHeader = null, ResourceLoader.MyHttpResponseHandlingDelegate httpResponseHandlingDelegate = null, Action<List<ParseError>> onParseFailed = null)
        {
            if (pluggable == null)
            {
                Debug.Log("とりあえず適当に初期化。実際には外側から渡す。");
                pluggable = new Handler();
            }

            this.view = uuebView;

            resLoader = new ResourceLoader(this.LoadParallel, requestHeader, httpResponseHandlingDelegate);
            this.view.AddChild(resLoader.cacheBox.transform);

            layoutMachine = new LayoutMachine(resLoader, pluggable);
            materializeMachine = new MaterializeMachine(resLoader, pluggable);

            if (onParseFailed != null)
            {
                this.onParseFailed = onParseFailed;
            }
            else
            {
                this.onParseFailed = errors =>
                {
                    Debug.LogError("parse errors:" + errors.Count);
                    foreach (var error in errors)
                    {
                        Debug.LogError("code:" + error.code + " reason:" + error.reason);
                    }
                };
            }
        }

        private void StartCalculateProgress(string[] treeIds)
        {
            // ローディングするものが一切ない場合、ここで完了。
            if (!IsLoading())
            {
                viewState = ViewState.Ready;

                if (eventReceiverGameObj != null)
                {
                    ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data) => handler.OnProgress(1));
                    ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data) => handler.OnLoaded(treeIds));
                }
                return;
            }


            var progressCor = CreateProgressCoroutine(treeIds);
            Internal_CoroutineExecutor(progressCor);
        }

        private void UpdateParentViewSizeIfExist()
        {
            view.UpdateParentSizeIfExist(new Vector2(layoutedTree.viewWidth, layoutedTree.viewHeight));
        }

        private LoadingCoroutineObj[] LoadingActs()
        {
            return loadingCoroutines.Where(r => !r.isDone).ToArray();
        }

        private IEnumerator CreateProgressCoroutine(string[] treeIds)
        {
            while (IsWaitStartLoading())
            {
                yield return null;
            }

            var loadingActions = LoadingActs();
            var loadingCount = loadingActions.Length;
            var perProgressUnit = 1.0 / loadingCount;
            var perProgress = perProgressUnit;


            while (IsLoading())
            {
                var currentLoadingCount = LoadingActs().Length;
                if (currentLoadingCount != loadingCount)
                {
                    var diff = loadingCount - currentLoadingCount;
                    perProgress = perProgress + (perProgressUnit * diff);

                    // notify.
                    if (eventReceiverGameObj != null)
                    {
                        ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data) => handler.OnProgress(perProgress));
                    }

                    // update count.
                    loadingCount = currentLoadingCount;
                }

                yield return null;
            }

            // loaded.
            viewState = ViewState.Ready;

            if (eventReceiverGameObj != null)
            {
                ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data) => handler.OnLoaded(treeIds));
            }
        }


        public TagTree layoutedTree
        {
            private set; get;
        }

        private Vector2 viewRect;
        private GameObject eventReceiverGameObj;

        public void LoadHtml(string source, Vector2 viewRect, float offsetY, GameObject eventReceiverGameObj = null)
        {
            viewState = ViewState.Loading;

            if (this.viewRect != viewRect)
            {
                if (eventReceiverGameObj != null)
                {
                    ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data) => handler.OnLoadStarted());
                }

                this.viewRect = viewRect;
                this.eventReceiverGameObj = eventReceiverGameObj;
            }

            var cor = Parse(source, offsetY);
            CoroutineExecutor(cor);
        }

        public void DownloadHtml(string url, Vector2 viewRect, GameObject eventReceiverGameObj = null)
        {
            viewState = ViewState.Loading;

            if (eventReceiverGameObj != null)
            {
                ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data) => handler.OnLoadStarted());
            }

            this.viewRect = viewRect;
            this.eventReceiverGameObj = eventReceiverGameObj;

            var cor = DownloadHTML(url);
            CoroutineExecutor(cor);
        }

        private IEnumerator DownloadHTML(string url)
        {
            viewState = ViewState.Loading;

            var uri = new Uri(url);
            var scheme = uri.Scheme;

            var html = string.Empty;
            switch (scheme)
            {
                case "http":
                case "https":
                    {
                        var downloadFailed = false;
                        Action<ContentType, int, string> failed = (contentType, code, reason) =>
                        {
                            downloadFailed = true;

                            if (eventReceiverGameObj != null)
                            {
                                ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data) => handler.OnLoadFailed(contentType, code, reason));
                            }
                        };

                        var cor = resLoader.DownloadHTMLFromWeb(url, failed);

                        while (cor.MoveNext())
                        {
                            if (cor.Current != null)
                            {
                                break;
                            }
                            yield return null;
                        }

                        if (downloadFailed)
                        {
                            yield break;
                        }

                        html = cor.Current;
                        break;
                    }
                case "resources":
                    {
                        var resourcePathWithExtension = url.Substring("resources://".Length);
                        var resourcePath = Path.ChangeExtension(resourcePathWithExtension, null);
                        var cor = Resources.LoadAsync(resourcePath);
                        while (!cor.isDone)
                        {
                            yield return null;
                        }
                        var res = cor.asset as TextAsset;
                        if (res == null)
                        {
                            if (eventReceiverGameObj != null)
                            {
                                ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data) => handler.OnLoadFailed(ContentType.HTML, 0, "could not found html:" + url));
                            }
                            yield break;
                        }

                        html = res.text;
                        break;
                    }
            }

            var parse = Parse(html, 0f);

            while (parse.MoveNext())
            {
                yield return null;
            }
        }

        private IEnumerator Parse(string source, float offsetY)
        {
            IEnumerator reload = null;

            var parser = new HTMLParser(resLoader);
            var parse = parser.ParseRoot(
                source,
                parsedTagTree =>
                {
                    if (parsedTagTree.errors.Any())
                    {
                        if (onParseFailed != null)
                        {
                            onParseFailed(parsedTagTree.errors);
                        }
                        return;
                    }
                    reload = Load(parsedTagTree, viewRect, offsetY, eventReceiverGameObj);
                }
            );

            while (parse.MoveNext())
            {
                yield return null;
            }

            if (reload == null)
            {
                yield break;
            }

            while (reload.MoveNext())
            {
                yield return null;
            }
        }

        /**
            layout -> materialize.
            if parsedTagTree was changed, materialize dirty flagged content only.
         */
        private IEnumerator Load(TagTree tree, Vector2 viewRect, float offsetY, GameObject eventReceiverGameObj = null)
        {
            var usingIds = TagTree.CorrectTrees(tree);

            IEnumerator materialize = null;
            var layout = layoutMachine.Layout(
                tree,
                viewRect,
                layoutedTree =>
                {
                    // update layouted tree.
                    this.layoutedTree = layoutedTree;

                    var newIds = TagTree.CollectTreeIds(this.layoutedTree);

                    UpdateParentViewSizeIfExist();

                    resLoader.BackGameObjects(usingIds);
                    materialize = materializeMachine.Materialize(
                        view.GetGameObject(),
                        this,
                        this.layoutedTree,
                        viewRect,
                        offsetY,
                        () =>
                        {
                            StartCalculateProgress(newIds);
                        }
                    );

                }
            );

            {
                while (layout.MoveNext())
                {
                    yield return null;
                }
            }

            {
                while (materialize.MoveNext())
                {
                    yield return null;
                }
            }
        }

        /**
            update contents.
         */
        private IEnumerator Update(TagTree tree, Vector2 viewRect, string[] appendedTreeIds, GameObject eventReceiverGameObj = null, Action onAfterUpdate = null)
        {
            var usingIds = TagTree.CorrectTrees(tree);

            IEnumerator materialize = null;
            var layout = layoutMachine.Layout(
                tree,
                viewRect,
                newLayoutedTree =>
                {
                    // update layouted tree.
                    this.layoutedTree = newLayoutedTree;

                    UpdateParentViewSizeIfExist();

                    resLoader.BackGameObjects(usingIds);
                    materialize = materializeMachine.Materialize(
                        view.GetGameObject(),
                        this,
                        this.layoutedTree,
                        viewRect,
                        0f,
                        () =>
                        {
                            // done updating.
                            viewState = ViewState.Ready;

                            if (onAfterUpdate != null)
                            {
                                onAfterUpdate();
                            }

                            if (eventReceiverGameObj != null)
                            {
                                ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data) => handler.OnUpdated(appendedTreeIds));
                            }
                        }
                    );
                }
            );

            while (layout.MoveNext())
            {
                yield return null;
            }

            Debug.Assert(materialize != null, "materialize is null.");

            while (materialize.MoveNext())
            {
                yield return null;
            }
        }

        /**
            すべてのGameObjectを消して、コンテンツをリロードする
         */
        public void Reload()
        {
            resLoader.Reset();
            CoroutineExecutor(Load(layoutedTree, viewRect, 0f, eventReceiverGameObj));
        }

        /**
            コンテンツのパラメータを初期値に戻す
         */
        public void Reset()
        {
            TagTree.ResetHideFlags(layoutedTree);
        }

        public void Update(string[] appendedTreeIds, Action onAfterUpdate)
        {
            viewState = ViewState.Updating;
            CoroutineExecutor(Update(layoutedTree, viewRect, appendedTreeIds, eventReceiverGameObj, onAfterUpdate));
        }

        public void OnImageTapped(GameObject element, string src, string buttonId = "")
        {
            // Debug.LogError("image. element:" + element + " key:" + key + " buttonId:" + buttonId);

            if (!string.IsNullOrEmpty(buttonId) && listenerDict.ContainsKey(buttonId))
            {
                listenerDict[buttonId].ForEach(t => t.ShowOrHide());
                Update(
                    new string[0],

                    () =>
                    {
                        if (eventReceiverGameObj != null)
                        {
                            ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data) => handler.OnElementTapped(ContentType.IMAGE, element, src, buttonId));
                        }
                    }
                );
            }
            else
            {
                if (eventReceiverGameObj != null)
                {
                    ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data) => handler.OnElementTapped(ContentType.IMAGE, element, src, buttonId));
                }
            }
        }


        public void OnLinkTapped(GameObject element, string href, string linkId = "")
        {
            // Debug.LogError("link. element:" + element + " key:" + key + " linkId:" + linkId);

            if (!string.IsNullOrEmpty(linkId) && listenerDict.ContainsKey(linkId))
            {
                listenerDict[linkId].ForEach(t => t.ShowOrHide());
                Update(
                    new string[0],
                    () =>
                    {
                        if (eventReceiverGameObj != null)
                        {
                            ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data) => handler.OnElementTapped(ContentType.LINK, element, href, linkId));
                        }
                    }
                );
            }
            else
            {
                if (eventReceiverGameObj != null)
                {
                    ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data) => handler.OnElementTapped(ContentType.LINK, element, href, linkId));
                }
            }


        }

        // public void OnScrollRangeChanged(float index)
        // {
        //     // スクロール値が範囲内だけれど、イベント中なので無視する。
        //     if (viewState != ViewState.Ready)
        //     {
        //         // ignore.
        //         Debug.Log("イベント中にスクロールが来ても無視する(記録しておく必要はある気がする");
        //         return;
        //     }

        //     var offsetY = (int)index;
        //     var cor = materializeMachine.OnScroll(view.GetGameObject(), this.layoutedTree, offsetY, viewRect.y);
        //     CoroutineExecutor(cor);
        // }

        private enum ViewState
        {
            Loading,
            Updating,
            Ready
        }

        private ViewState viewState;

        public void AddListener(TagTree tree, string listenTargetId)
        {
            if (!listenerDict.ContainsKey(listenTargetId))
            {
                listenerDict[listenTargetId] = new List<TagTree>();
            }

            if (!listenerDict[listenTargetId].Contains(tree))
            {
                listenerDict[listenTargetId].Add(tree);
            }
        }

        public void LoadParallel(IEnumerator cor)
        {
            CoroutineExecutor(cor);
        }

        private object lockObj = new object();
        private Queue<IEnumerator> queuedCoroutines = new Queue<IEnumerator>();
        private Queue<IEnumerator> unmanagedCoroutines = new Queue<IEnumerator>();
        private List<LoadingCoroutineObj> loadingCoroutines = new List<LoadingCoroutineObj>();


        public void Dequeue(IUUebView view)
        {
            lock (lockObj)
            {
                while (0 < queuedCoroutines.Count)
                {
                    var cor = queuedCoroutines.Dequeue();
                    var loadCorObj = new LoadingCoroutineObj();
                    var loadingCor = CreateLoadingCoroutine(cor, loadCorObj);
                    view.StartCoroutine(loadingCor);

                    // collect loading coroutines.
                    AddLoading(loadCorObj);
                }

                while (0 < unmanagedCoroutines.Count)
                {
                    var cor = unmanagedCoroutines.Dequeue();
                    view.StartCoroutine(cor);
                }
            }
        }

        private IEnumerator CreateLoadingCoroutine(IEnumerator cor, LoadingCoroutineObj loadCor)
        {
            while (cor.MoveNext())
            {
                yield return null;
            }
            loadCor.isDone = true;
        }

        private void AddLoading(LoadingCoroutineObj runObj)
        {
            loadingCoroutines.Add(runObj);
        }

        public void Internal_CoroutineExecutor(IEnumerator iEnum)
        {
            lock (lockObj)
            {
                unmanagedCoroutines.Enqueue(iEnum);
            }
        }

        public void CoroutineExecutor(IEnumerator iEnum)
        {
            lock (lockObj)
            {
                queuedCoroutines.Enqueue(iEnum);
            }
        }

        public bool IsWaitStartLoading()
        {
            lock (lockObj)
            {
                if (queuedCoroutines.Any())
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsLoading()
        {
            lock (lockObj)
            {
                if (queuedCoroutines.Any())
                {
                    return true;
                }

                if (loadingCoroutines.Where(cor => !cor.isDone).Any())
                {
                    // Debug.LogError("loading:" + loadingCoroutines.Count);
                    return true;
                }
            }

            return false;
        }

        public class LoadingCoroutineObj
        {
            public bool isDone = false;
        }


        /*
            TreeQL関連、実際に操作できるタイミングは非アップデート中とかそんな感じなので、
            stateに関する制約が必要。
         */

        public void AppendContentToLast(string htmlContent, string query = "")
        {
            if (viewState != ViewState.Ready)
            {
                return;
            }

            TagTree.CorrectTrees(layoutedTree);

            var parser = new HTMLParser(resLoader);
            var parse = parser.ParseRoot(
                htmlContent,
                parsedTagTree =>
                {
                    if (parsedTagTree.errors.Any())
                    {
                        Debug.LogError("parse errors:" + parsedTagTree.errors.Count);

                        foreach (var error in parsedTagTree.errors)
                        {
                            Debug.LogError("code:" + error.code + " reason:" + error.reason);
                        }
                        return;
                    }

                    // parse succeeded.
                    var newTreeIds = TagTree.CollectTreeIds(parsedTagTree);
                    var currentTreeIds = TagTree.CollectTreeIds(layoutedTree);

                    var appendedTreeIds = newTreeIds.Except(currentTreeIds).ToArray();

                    var children = parsedTagTree.GetChildren();

                    // add to content.
                    if (string.IsNullOrEmpty(query))
                    {
                        layoutedTree.AddChildren(children.ToArray());
                    }
                    else
                    {
                        var targetTreeFamily = GetTreeFamily(new List<TagTree> { layoutedTree }, GetQuery(query));
                        targetTreeFamily.Last().AddChildren(children.ToArray());
                    }

                    // relayout.
                    Update(appendedTreeIds, () => { });
                }
            );

            CoroutineExecutor(parse);
        }

        public void DeleteByPoint(string query)
        {
            if (viewState != ViewState.Ready)
            {
                return;
            }

            TagTree.CorrectTrees(layoutedTree);

            var queryArray = GetQuery(query);

            var targetTreeFamily = GetTreeFamily(new List<TagTree> { layoutedTree }, queryArray);
            if (targetTreeFamily.Any())
            {
                var deleteTargetTree = targetTreeFamily.Last();
                targetTreeFamily[targetTreeFamily.Length - 2].GetChildren().Remove(deleteTargetTree);
            }

            Update(new string[0], () => { });
        }

        private string[] GetQuery(string queryStr)
        {
            if (queryStr.StartsWith("/"))
            {
                queryStr = queryStr.TrimStart('/');
            }

            if (queryStr.EndsWith("/"))
            {
                queryStr = queryStr.TrimEnd('/');
            }

            return queryStr.Split('/');
        }

        private TagTree[] GetTreeFamily(List<TagTree> family, string[] query)
        {
            var tagValueAndIndex = query[0].Split(':');
            var headStr = tagValueAndIndex[0];
            var head = resLoader.FindTag(headStr);

            var count = 0;
            if (1 < tagValueAndIndex.Length)
            {
                count = Convert.ToInt32(tagValueAndIndex[1]);
            }

            var currentChildren = family.Last().GetChildren();

            var candidates = currentChildren.Where(t => t.tagValue == head &&
                (t.treeType == TreeType.Container || t.treeType == TreeType.Content_Img)
            ).ToArray();

            if (count < candidates.Length)
            {
                var target = candidates[count];
                family.Add(target);

                if (query.Length == 1)
                {
                    return family.ToArray();
                }

                var newQuery = query.ToList();
                newQuery.RemoveAt(0);

                return GetTreeFamily(family, newQuery.ToArray());
            }

            // not found. return empty tag tree.
            throw new ArgumentException("requested tag not found. tag:" + headStr);
        }

        public TagTree[] GetTreeById(string id)
        {
            return TagTree.GetTreeById(layoutedTree, id);
        }
    }

    public enum ContentType
    {
        HTML,
        IMAGE,
        LINK,
        CUSTOMTAGLIST
    }

    public interface IUUebViewEventHandler : IEventSystemHandler
    {
        void OnLoadStarted();
        void OnProgress(double progress);
        void OnLoaded(string[] treeIds);
        void OnUpdated(string[] newTreeIds);
        void OnLoadFailed(ContentType type, int code, string reason);
        void OnElementTapped(ContentType type, GameObject element, string param, string id);
        void OnElementLongTapped(ContentType type, string param, string id);
    }
}