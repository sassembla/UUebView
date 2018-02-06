using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace UUebView
{
    public class MaterializeMachine
    {
        private readonly ResourceLoader resLoader;
        private UUebViewCore core;
        public MaterializeMachine(ResourceLoader resLoader)
        {
            this.resLoader = resLoader;
        }

        public readonly Dictionary<string, KeyValuePair<GameObject, string>> eventObjectCache = new Dictionary<string, KeyValuePair<GameObject, string>>();

        // スクロールイベントから生成を行う ↓
        // オブジェクトプールの再考(画面外に行ったオブジェクトのプール復帰、新しい要素のプールからの取得)

        // Layer系のオブジェクト、高さが0のツリー、hiddenを無視する(この辺は上記のチョイス時に削れると良さそう。)
        public IEnumerator Materialize(GameObject root, UUebViewCore core, TagTree tree, Vector2 viewRect, float yOffset, Action onLoaded)
        {
            var viewHeight = viewRect.y;

            {
                var rootRectTrans = root.GetComponent<RectTransform>();
                this.core = core;

                // set anchor to left top.
                rootRectTrans.anchorMin = Vector2.up;
                rootRectTrans.anchorMax = Vector2.up;
                rootRectTrans.pivot = Vector2.up;

                rootRectTrans.sizeDelta = new Vector2(tree.viewWidth, tree.viewHeight);
            }

            // 描画範囲にあるツリーのidを集める。ここから一瞬でも外れたらskip。
            var drawTreeIds = TraverseTree(tree, yOffset, viewHeight);

            // materialize root's children in parallel.
            var children = tree.GetChildren();

            var cors = new List<IEnumerator>();
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                var cor = MaterializeRecursive(child, root, drawTreeIds);
                cors.Add(cor);
            }

            // firstviewのmaterializeまでを並列で実行する
            while (true)
            {
                for (var i = 0; i < cors.Count; i++)
                {
                    var cor = cors[i];
                    if (cor == null)
                    {
                        continue;
                    }

                    var cont = cor.MoveNext();

                    if (!cont)
                    {
                        cors[i] = null;
                    }
                }

                var running = cors.Where(c => c != null).Any();

                // wait all coroutine's end.
                if (!running)
                {
                    break;
                }

                yield return null;
            }

            onLoaded();
        }

        /**
            ツリーの中で画面内の範囲のもののidを集める。
         */
        private List<string> TraverseTree(TagTree sourceTree, float offsetY, float viewHeight)
        {
            var drawTargetTreeIds = new List<string>();

            CollectTreeIdRecursive(sourceTree, 0, offsetY, viewHeight, drawTargetTreeIds);

            return drawTargetTreeIds;
        }

        private void CollectTreeIdRecursive(TagTree sourceTree, float treeOffsetY, float offsetY, float viewHeight, List<string> drawTargetTreeIds)
        {
            var currentTreeOffsetY = treeOffsetY + sourceTree.offsetY;
            // Debug.Log("currentTreeOffsetY:" + currentTreeOffsetY + " offsetY:" + offsetY + " viewHeight:" + viewHeight);

            // top is in range or not.
            if (offsetY + viewHeight < currentTreeOffsetY)
            {
                // do nothing.
                return;
            }

            // bottom is in range or not.
            if (currentTreeOffsetY + sourceTree.viewHeight < offsetY)
            {
                // do nothing.
                return;
            }

            // this container is in range.
            drawTargetTreeIds.Add(sourceTree.id);

            var childlen = sourceTree.GetChildren();
            for (var i = 0; i < childlen.Count; i++)
            {
                var child = childlen[i];
                CollectTreeIdRecursive(child, currentTreeOffsetY, offsetY, viewHeight, drawTargetTreeIds);
            }
        }

        /**
            現状は全ての子について1f内で各1度は実行する、という処理になっている。
            n-m-o
             \p-q
               \r

            みたいな数のツリーがある場合、
            nのツリーを処理する段階で、毎フレームm,qが1度ずつ展開される。
            mの展開時にはoが毎フレーム1度ずつ展開される。
            pの展開時には、qとrが毎フレーム1度ずつ展開される。

            なので、全てのツリーが1fに1度は初期化されるようになる。
         */
        private IEnumerator MaterializeRecursive(TagTree tree, GameObject parent, List<string> drawTargetTreeIds)
        {
            // Debug.LogError("materialize:" + tree.treeType + " tag:" + resLoader.GetTagFromValue(tree.tagValue));
            if (tree.keyValueStore.ContainsKey(HTMLAttribute.LISTEN) && tree.keyValueStore.ContainsKey(HTMLAttribute.HIDDEN))
            {
                core.AddListener(tree, tree.keyValueStore[HTMLAttribute.LISTEN] as string);
            }

            if (tree.hidden || tree.treeType == TreeType.Content_CRLF)
            {
                // cancel materialize of this tree.
                yield break;
            }

            // if (!drawTargetTreeIds.Contains(tree.id))
            // {
            //     yield break;
            // }

            var objCor = resLoader.LoadGameObjectFromPrefab(tree.id, tree.tagValue, tree.treeType);

            while (objCor.MoveNext())
            {
                if (objCor.Current != null)
                {
                    break;
                }
                yield return null;
            }

            // set pos and size.
            var newGameObject = objCor.Current;

            var cached = false;
            if (newGameObject.transform.parent != null)
            {
                cached = true;
            }

            newGameObject.transform.SetParent(parent.transform, false);
            var rectTrans = newGameObject.GetComponent<RectTransform>();
            rectTrans.anchoredPosition = TagTree.AnchoredPositionOf(tree);
            rectTrans.sizeDelta = TagTree.SizeDeltaOf(tree);

            // set parameters and events by container type. button, link.
            var src = string.Empty;

            if (tree.keyValueStore.ContainsKey(HTMLAttribute.SRC))
            {
                src = tree.keyValueStore[HTMLAttribute.SRC] as string;
            }

            switch (tree.treeType)
            {
                case TreeType.Content_Img:
                    {
                        if (tree.viewHeight == 0)
                        {
                            break;
                        }

                        // 画像コンテンツはキャッシュ済みの場合再度画像取得を行わない。
                        if (!cached)
                        {
                            // 画像指定がある場合のみ読み込む
                            if (!string.IsNullOrEmpty(src))
                            {
                                var imageLoadCor = resLoader.LoadImageAsync(src);

                                // combine coroutine.
                                var setImageCor = SetImageCor(newGameObject, imageLoadCor);
                                resLoader.LoadParallel(setImageCor);
                            }
                        }
                        break;
                    }

                case TreeType.Content_Text:
                    {
                        // テキストコンテンツは毎回内容が変わる可能性があるため、キャッシュに関わらず更新する。
                        if (tree.keyValueStore.ContainsKey(HTMLAttribute._CONTENT))
                        {
                            var text = tree.keyValueStore[HTMLAttribute._CONTENT] as string;
                            if (!string.IsNullOrEmpty(text))
                            {
                                var textComponent = newGameObject.GetComponent<Text>();
                                if (textComponent != null)
                                {
                                    textComponent.text = text;
                                }
                                else
                                {
                                    var textComponentPro = newGameObject.GetComponent<TMPro.TextMeshProUGUI>();
                                    textComponentPro.text = text;
                                }
                            }
                        }

                        // 文字コンテンツのリンク化(hrefがついてるとリンクになる。実態はボタン。)
                        if (tree.keyValueStore.ContainsKey(HTMLAttribute.HREF))
                        {
                            var href = tree.keyValueStore[HTMLAttribute.HREF] as string;

                            var linkId = string.Empty;
                            if (tree.keyValueStore.ContainsKey(HTMLAttribute.ID))
                            {
                                linkId = tree.keyValueStore[HTMLAttribute.ID] as string;
                            }

                            eventObjectCache[linkId] = new KeyValuePair<GameObject, string>(newGameObject, href);

                            // add button component.
                            AddButton(newGameObject, () => core.OnLinkTapped(newGameObject, href, linkId));
                        }
                        break;
                    }

                default:
                    {
                        // do nothing.
                        break;
                    }

            }

            // button attrに応じたボタン化
            if (tree.keyValueStore.ContainsKey(HTMLAttribute.BUTTON))
            {
                var isButton = tree.keyValueStore[HTMLAttribute.BUTTON] as string == "true";
                if (isButton)
                {
                    var buttonId = string.Empty;
                    if (tree.keyValueStore.ContainsKey(HTMLAttribute.ID))
                    {
                        buttonId = tree.keyValueStore[HTMLAttribute.ID] as string;
                    }

                    eventObjectCache[buttonId] = new KeyValuePair<GameObject, string>(newGameObject, src);

                    // add button component.
                    AddButton(newGameObject, () => core.OnImageTapped(newGameObject, src, buttonId));
                }
            }

            var children = tree.GetChildren();

            var enums = new List<IEnumerator>();
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                var cor = MaterializeRecursive(child, newGameObject, drawTargetTreeIds);
                enums.Add(cor);
            }

            while (0 < enums.Count)
            {
                for (var i = 0; i < enums.Count; i++)
                {
                    var continuation = enums[i].MoveNext();
                    if (!continuation || enums[i].Current != null)
                    {
                        // 終わったので除外する
                        enums.RemoveAt(i);
                    }
                }
                yield return null;
            }
        }

        private IEnumerator SetImageCor(GameObject target, IEnumerator<Sprite> imageLoadCor)
        {
            while (imageLoadCor.MoveNext())
            {
                if (imageLoadCor.Current != null)
                {
                    break;
                }
                yield return null;
            }

            if (imageLoadCor.Current != null)
            {
                var sprite = imageLoadCor.Current;
                target.GetComponent<Image>().sprite = sprite;
            }
        }

        private void AddButton(GameObject obj, UnityAction param)
        {
            var button = obj.GetComponent<Button>();
            if (button == null)
            {
                button = obj.AddComponent<Button>();
            }

            if (Application.isPlaying)
            {
                button.onClick.RemoveAllListeners();

                /*
                    this code can set action to button. but it does not appear in editor inspector.
                */
                button.onClick.AddListener(param);
            }
            else
            {
                try
                {
                    button.onClick.AddListener(// 現状、エディタでは、Actionをセットする方法がわからん。関数単位で何かを用意すればいけそう = ButtonをPrefabにするとかしとけば行けそう。
                        param
                    );
                    // UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                    // 	button.onClick,
                    // 	() => rootMBInstance.OnImageTapped(tagPoint.tag, src)
                    // );

                    // // 次の書き方で、固定の値をセットすることはできる。エディタにも値が入ってしまう。
                    // インスタンスというか、Prefabを作りまくればいいのか。このパーツのインスタンスを用意して、そこに値オブジェクトを入れて、それが着火する、みたいな。
                    // UnityEngine.Events.UnityAction<String> callback = new UnityEngine.Events.UnityAction<String>(rootMBInstance.OnImageTapped);
                    // UnityEditor.Events.UnityEventTools.AddStringPersistentListener(
                    // 	button.onClick, 
                    // 	callback,
                    // 	src
                    // );
                }
                catch (Exception e)
                {
                    Debug.LogError("e:" + e);
                }
            }
        }

        /*
            scrollに合わせてMaterializeを行うプラン、とりあえず現状は不要っぽい。
         */
        // private List<string> drawTreeIds = new List<string>();
        // private bool running = false;
        // public IEnumerator OnScroll(GameObject root, TagTree layoutedTree, float yOffset, float viewHeight)
        // {
        //     // ここは、現在runしているツリーに関して記録して、それ以外が発生したらそれも追加で走らせる、とかのほうがいいんだろうか。
        //     // それともこの形状でも問題ないのかな。範囲の問題な気はする。
        //     if (running)
        //     {
        //         // 現在通過しているポイントまでの間のオブジェクトもちゃんと含む必要がある。ので、running中でも何らかの対処は必要。
        //         yield break;
        //     }

        //     var newDrawTreeIds = TraverseTree(layoutedTree, yOffset, viewHeight);

        //     // idに関してはあるものがそのまま出ているので文句なし。
        //     // で、まず重なるところをなんとかするか。
        //     // 単純で、すでに存在するidは外す。
        //     var newIds = newDrawTreeIds.Except(drawTreeIds).ToList();
        //     Debug.Log("走ってる newIds:" + newIds.Count);

        //     if (0 < newIds.Count())
        //     {
        //         running = true;
        //     }
        //     else
        //     {
        //         yield break;
        //     }

        //     /*
        //         重複するところ、キャッシュをローテーションするところを作る。
        //      */
        //     var children = layoutedTree.GetChildren();
        //     for (var i = 0; i < children.Count; i++)
        //     {
        //         var child = children[i];
        //         var cor = MaterializeRecursive(child, root, newIds);

        //         while (cor.MoveNext())
        //         {
        //             yield return null;
        //         }
        //     }

        //     drawTreeIds = newDrawTreeIds;
        //     running = false;
        // }
    }
}