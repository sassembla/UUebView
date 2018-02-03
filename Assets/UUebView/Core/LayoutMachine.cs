using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UUebView
{
    /**
        レイアウトを実行するクラス。
    */
    public class LayoutMachine
    {
        private readonly ResourceLoader resLoader;
        private readonly TextGenerator generator;

        public LayoutMachine(ResourceLoader resLoader)
        {
            this.resLoader = resLoader;
            this.generator = new TextGenerator();
        }

        private enum InsertType
        {
            Continue,
            InsertContentToNextLine,
            RetryWithNextLine,
            Crlf,
            HeadInsertedToTheEndOfLine,
            TailInsertedToLine,
            LastLineEndedInTheMiddleOfLine,
            AddContentToContainer,
            LineEndedWithoutNewContainerFirstLine,
            LineEndedWithFilledImage,
        };

        public IEnumerator Layout(TagTree rootTree, Vector2 view, Action<TagTree> layouted)
        {
            var viewCursor = new ViewCursor(0, 0, view.x, view.y);

            var cor = DoLayout(rootTree, viewCursor);

            while (cor.MoveNext())
            {
                if (cor.Current != null)
                {
                    break;
                }
                yield return null;
            }

            var childViewCursor = cor.Current;

            // ビュー高さをセット
            rootTree.viewHeight = childViewCursor.viewHeight;

            layouted(rootTree);
        }

        /**
            コンテンツ単位でのレイアウトの起点。ここからtreeTypeごとのレイアウトを実行する。
         */
        private IEnumerator<ChildPos> DoLayout(TagTree tree, ViewCursor viewCursor, Func<InsertType, TagTree, ViewCursor> insertion = null)
        {
            if (viewCursor.viewWidth < 0)
            {
                throw new Exception("cannot use negative width. viewCursor:" + viewCursor + " tag:" + Debug_GetTagStrAndType(tree));
            }
            // Debug.LogError("tree:" + Debug_GetTagStrAndType(tree) + " viewCursor:" + viewCursor);
            // Debug.LogWarning("まだ実装してない、brとかhrでの改行処理。 実際にはpとかも一緒で、「このコンテンツが終わったら改行する」みたいな属性が必須。区分けとしてはここではないか。＿なんちゃらシリーズと一緒に分けちゃうのもありかな〜");

            IEnumerator<ChildPos> cor = null;
            switch (tree.treeType)
            {
                case TreeType.CustomLayer:
                    {
                        cor = DoLayerLayout(tree, viewCursor, insertion);
                        break;
                    }
                case TreeType.CustomEmptyLayer:
                    {
                        cor = DoEmptyLayerLayout(tree, viewCursor, insertion);
                        break;
                    }
                case TreeType.Container:
                    {
                        cor = DoContainerLayout(tree, viewCursor, insertion);
                        break;
                    }
                case TreeType.Content_Img:
                    {
                        cor = DoImgLayout(tree, viewCursor, insertion);
                        break;
                    }
                case TreeType.Content_Text:
                    {
                        cor = DoTextLayout(tree, viewCursor, insertion);
                        break;
                    }
                case TreeType.Content_CRLF:
                    {
                        cor = DoCRLFLayout(tree, viewCursor, insertion);
                        break;
                    }
                default:
                    {
                        throw new Exception("unexpected tree type:" + tree.treeType);
                    }
            }

            /*
                もしもtreeがhiddenだった場合でも、のちのち表示するために内容のロードは行う。
                コンテンツへのサイズの指定も0で行う。
                ただし、同期的に読む必要はないため、並列でロードする。
             */
            if (tree.hidden)
            {
                var loadThenSetHiddenPosCor = SetHiddenPosCoroutine(tree, cor);
                resLoader.LoadParallel(loadThenSetHiddenPosCor);

                var hiddenCursor = ViewCursor.ZeroSizeCursor(viewCursor);
                // Debug.LogError("hidden tree:" + Debug_GetTagStrAndType(tree) + " viewCursor:" + viewCursor);

                yield return tree.SetPosFromViewCursor(hiddenCursor);
                throw new Exception("never come here.");
            }
            else
            {
                while (cor.MoveNext())
                {
                    if (cor.Current != null)
                    {
                        break;
                    }
                    yield return null;
                }

                // Debug.LogError("done layouted tree:" + Debug_GetTagStrAndType(tree) + " next cursor:" + cor.Current);
                yield return cor.Current;
            }
        }

        /**
            カスタムタグのレイヤーのレイアウトを行う。
            customTagLayer/box/boxContents(layerとか) という構造になっていて、boxはlayer内に必ず規定のポジションでレイアウトされる。
            ここだけ相対的なレイアウトが確実に崩れる。
         */
        private IEnumerator<ChildPos> DoLayerLayout(TagTree layerTree, ViewCursor parentBoxViewCursor, Func<InsertType, TagTree, ViewCursor> insertion = null)
        {
            ViewCursor basePos;

            float rightAnchorWidth = 0;
            float bottomAnchorHeight = 0;

            if (layerTree.keyValueStore.ContainsKey(HTMLAttribute._LAYER_PARENT_TYPE))
            {
                // 親がboxなので、boxのoffsetYとサイズを継承する。offsetXは常に0で来る。

                // layerのコンテナとしての特性として、xには常に0が入る = 左詰めでレイアウトが開始される。
                basePos = new ViewCursor(0, parentBoxViewCursor.offsetY, parentBoxViewCursor.viewWidth, parentBoxViewCursor.viewHeight);
            }
            else
            {
                // 親がboxではないlayerは、layer生成時の高さを使って基礎高さを出す。
                // 内包するboxの位置基準値が生成時のものなので、あらかじめ持っておいた値を使うのが好ましい。
                var boxPos = resLoader.GetUnboxedLayerSize(layerTree.tagValue);

                // 親のサイズから、今回レイヤーコンテンツを詰める箱を作り出す
                var rect = TagTree.GetChildViewRectFromParentRectTrans(parentBoxViewCursor.viewWidth, parentBoxViewCursor.viewHeight, boxPos, out rightAnchorWidth, out bottomAnchorHeight);

                // 幅が、このレイヤーが入る最低サイズ未満なので、改行での挿入を促す。
                if (rect.width <= 0)
                {
                    // 送り出して改行してもらう
                    insertion(InsertType.RetryWithNextLine, null);
                    yield break;
                }


                basePos = new ViewCursor(parentBoxViewCursor.offsetX + rect.position.x, parentBoxViewCursor.offsetY + rect.position.y, rect.width, boxPos.originalHeight);
            }

            // collisionGroup単位での追加高さ、一番下まで伸びてるやつを基準にする。
            float additionalHeight = 0f;

            {
                var boxYPosRecords = new Dictionary<float, float>();
                var collisionGrouId = 0;

                /*
                    レイヤーなので、prefabをロードして、原点位置は0,0、
                        サイズは親サイズ、という形で生成する。
                    
                    ・childlenにboxの中身が含まれている場合(IsContainedThisCustomTag)、childlenの要素を生成する。そうでない要素の場合は生成しない。
                    ・この際のchildのサイズは、layerであれば必ずboxのサイズになる。このへんがキモかな。
                */
                var children = layerTree.GetChildren();

                for (var i = 0; i < children.Count; i++)
                {
                    var boxTree = children[i];

                    // Debug.LogError("box tag:" + resLoader.GetTagFromIndex(boxTree.parsedTag) + " boxTree:" + boxTree.treeType);

                    /*
                        位置情報はkvに入っているが、親のviewの値を使ってレイアウト後の位置に関する数値を出す。
                    */
                    var boxRect = boxTree.keyValueStore[HTMLAttribute._BOX] as BoxPos;
                    float a;
                    var childBoxViewRect = TagTree.GetChildViewRectFromParentRectTrans(basePos.viewWidth, basePos.viewHeight, boxRect, out a, out a);

                    /*
                        collisionGroupによる区切りで、コンテンツ帯ごとの高さを出し、
                        最も下にあるコンテンツの伸び幅を次の縦並びグループの開始オフセット位置追加値としてセットする。
                    */
                    var boxCollisionGroupId = (int)boxTree.keyValueStore[HTMLAttribute._COLLISION];

                    // Debug.LogError("boxCollisionGroupId:" + boxCollisionGroupId);

                    if (collisionGrouId != boxCollisionGroupId)
                    {
                        if (boxYPosRecords.Any())
                        {
                            var tallest = boxYPosRecords.Select(kv => kv.Key).Max();
                            additionalHeight = boxYPosRecords[tallest] + additionalHeight;

                            // update. entried to new collision group.
                            collisionGrouId = boxCollisionGroupId;

                            boxYPosRecords.Clear();
                        }
                    }

                    var childView = new ViewCursor(childBoxViewRect.x, childBoxViewRect.y + additionalHeight, childBoxViewRect.width, childBoxViewRect.height);

                    var cor = LayoutBoxedContents(boxTree, childView);

                    while (cor.MoveNext())
                    {
                        if (cor.Current != null)
                        {
                            break;
                        }
                        yield return null;
                    }

                    // fix position.
                    var childCursor = cor.Current;

                    // add record.
                    var yPos = childCursor.offsetY + childCursor.viewHeight;
                    boxYPosRecords[yPos] = childCursor.viewHeight - childBoxViewRect.height;
                }

                if (boxYPosRecords.Any())
                {
                    // 最終グループの追加値をviewの高さに足す
                    var tallestInGroup = boxYPosRecords.Keys.Max();
                    // Debug.LogError("tallestInGroup:" + tallestInGroup);
                    additionalHeight = boxYPosRecords[tallestInGroup] + additionalHeight;
                }
            }

            // 基礎高さ + 増加分高さ
            var newHeight = basePos.viewHeight + additionalHeight;

            // Debug.LogWarning("after layerTree:" + GetTagStr(layerTree.tagValue) + " layerViewCursor:" + layerViewCursor);
            // treeに位置をセットしてposを返す

            // layerにセットするパラメータは、レイヤー自体のレイアウトに使用する。余白を含めたオフセット、幅が使用される。
            layerTree.SetPos(basePos.offsetX, basePos.offsetY, basePos.viewWidth, newHeight);

            // このコンテンツを表示するのに消費したポジションとして、親のオフセットから開始して、このコンテンツの余白までを含めたサイズを指定する。
            yield return new ChildPos(parentBoxViewCursor.offsetX, parentBoxViewCursor.offsetY, basePos.offsetX + basePos.viewWidth + rightAnchorWidth, basePos.offsetY + newHeight + bottomAnchorHeight);
        }

        private IEnumerator<ChildPos> DoEmptyLayerLayout(TagTree emptyLayerTree, ViewCursor viewCursor, Func<InsertType, TagTree, ViewCursor> insertion = null)
        {
            var baseViewCursorHeight = viewCursor.viewHeight;

            var childView = new ViewCursor(0, viewCursor.offsetY, viewCursor.viewWidth, viewCursor.viewHeight);
            var cor = DoContainerLayout(emptyLayerTree, childView, (type, tree) => { throw new Exception("never called."); });

            while (cor.MoveNext())
            {
                if (cor.Current != null)
                {
                    break;
                }
                yield return null;
            }

            var layoutedChildPos = cor.Current;

            /*
                レイアウト済みの高さがlayer本来の高さより低い場合、レイヤー本来の高さを使用する(隙間ができる)
             */
            if (layoutedChildPos.viewHeight < baseViewCursorHeight)
            {
                layoutedChildPos.viewHeight = baseViewCursorHeight;
            }
            // Debug.LogError("layoutedChildPos:" + layoutedChildPos + " vs baseViewCursorHeight:" + baseViewCursorHeight);

            // treeに位置をセットしてposを返す
            yield return emptyLayerTree.SetPos(layoutedChildPos);
        }

        private IEnumerator<ChildPos> DoImgLayout(TagTree imgTree, ViewCursor viewCursor, Func<InsertType, TagTree, ViewCursor> insertion = null)
        {
            var contentViewCursor = viewCursor;

            var imageWidth = 0f;
            var imageHeight = 0f;

            /*
                デフォルトタグであれば画像サイズは未定(画像依存)なのでDLして判断する必要がある。
                カスタムタグであれば固定サイズで画像が扱えるため、prefabをロードしてサイズを固定して計算する。
             */
            if (resLoader.IsDefaultTag(imgTree.tagValue))
            {
                // default img tag. need to download image for determine size.
                if (!imgTree.keyValueStore.ContainsKey(HTMLAttribute.SRC))
                {
                    throw new Exception("tag:" + resLoader.GetTagFromValue(imgTree.tagValue) + " requires 'src' param.");
                }

                var src = imgTree.keyValueStore[HTMLAttribute.SRC] as string;
                var cor = resLoader.LoadImageAsync(src);

                while (cor.MoveNext())
                {
                    if (cor.Current != null)
                    {
                        break;
                    }
                    yield return null;
                }

                if (cor.Current != null)
                {
                    var sprite = cor.Current;
                    imageWidth = sprite.rect.size.x;
                    imageHeight = sprite.rect.size.y;

                    // 画像幅が画面幅よりも小さい場合、画面幅に合わせて小さくする。
                    if (viewCursor.viewWidth < imageWidth)
                    {
                        imageWidth = viewCursor.viewWidth;

                        // 高さをアスペクト比に対して合わせる。
                        imageHeight = (imageWidth / sprite.rect.size.x) * sprite.rect.size.y;
                        if (insertion != null)
                        {
                            insertion(InsertType.LineEndedWithFilledImage, null);
                        }
                    }
                }
                else
                {
                    // 通信に失敗したので、サイズ0として扱う。
                    imageHeight = 0;
                }
            }
            else
            {
                // customtag, requires prefab.
                var cor = resLoader.LoadPrefab(imgTree.tagValue, TreeType.Content_Img);

                while (cor.MoveNext())
                {
                    if (cor.Current != null)
                    {
                        break;
                    }
                    yield return null;
                }

                var prefab = cor.Current;
                var rect = prefab.GetComponent<RectTransform>();
                imageWidth = rect.sizeDelta.x;
                imageHeight = rect.sizeDelta.y;
            }

            // 画像のアスペクト比に則ったサイズを返す。
            // treeに位置をセットしてposを返す
            yield return imgTree.SetPos(contentViewCursor.offsetX, contentViewCursor.offsetY, imageWidth, imageHeight);
        }

        private IEnumerator<ChildPos> DoContainerLayout(TagTree containerTree, ViewCursor containerViewCursor, Func<InsertType, TagTree, ViewCursor> insertion = null)
        {
            /*
                子供のタグを整列させる処理。
                横に整列、縦に並ぶ、などが実行される。

                親カーソルから子カーソルを生成。高さに関しては適当。
            */

            AlignMode alignMode = AlignMode.def;
            if (containerTree.keyValueStore.ContainsKey(HTMLAttribute.ALIGN))
            {
                var keyStr = containerTree.keyValueStore[HTMLAttribute.ALIGN] as string;

                // no try-catch needed. already checked in html parser.
                alignMode = (AlignMode)Enum.Parse(typeof(AlignMode), keyStr, true);
            }

            var containerChildren = containerTree.GetChildren();
            var childCount = containerChildren.Count;

            if (childCount == 0)
            {
                // treeに位置をセットしてposを返す
                yield return containerTree.SetPosFromViewCursor(containerViewCursor);
                throw new Exception("never come here.");
            }

            var linedElements = new List<TagTree>();
            var mostRightPoint = 0f;
            var mostBottomPoint = 0f;
            {
                var nextChildViewCursor = new ViewCursor(0, 0, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                for (var childIndex = 0; childIndex < childCount; childIndex++)
                {

                    var child = containerChildren[childIndex];
                    if (child.treeType == TreeType.Content_Text)
                    {
                        child.keyValueStore[HTMLAttribute._ONLAYOUT_PRESET_X] = containerViewCursor.offsetX;
                    }

                currentLineRetry:
                    {
                        linedElements.Add(child);

                        // set insertion type.
                        var currentInsertType = InsertType.Continue;
                        // 子供ごとにレイアウトし、結果を受け取る
                        var cor = DoLayout(
                            child,
                            nextChildViewCursor,
                            /*
                                このブロックは<このコンテナ発のinsertion発動地点>か、
                                このコンテナの内部のコンテナから呼ばれる。
                             */
                            (insertType, insertingChild) =>
                            {
                                currentInsertType = insertType;

                                switch (insertType)
                                {
                                    case InsertType.InsertContentToNextLine:
                                        {
                                            /*
                                                現在のコンテンツを分割し、後続の列へと分割後の後部コンテンツを差し込む。
                                                あたまが生成され、後続部分が存在し、それらが改行後のコンテンツとして分割、ここに挿入される。
                                            */
                                            containerChildren.Insert(childIndex + 1, insertingChild);
                                            childCount++;
                                            break;
                                        }
                                    case InsertType.HeadInsertedToTheEndOfLine:
                                        {
                                            // Debug.LogError("received:" + Debug_GetTagStrAndType(containerTree) + " inserting:" + Debug_GetTagStrAndType(insertingChild) + " text:" + insertingChild.keyValueStore[HTMLAttribute._CONTENT]);
                                            if (0 < nextChildViewCursor.offsetX)
                                            {
                                                // 行中開始の子コンテナ内での改行イベントを受け取った

                                                // 子コンテナ自体は除外
                                                linedElements.Remove(child);

                                                // 現在整列してるコンテンツの整列を行う。
                                                linedElements.Add(insertingChild);

                                                // ライン化処理
                                                DoLiningFromFirstContent(containerViewCursor.viewWidth, linedElements);

                                                // 消化
                                                linedElements.Clear();

                                                /*
                                                    子供コンテナの基礎viewを、行頭からのものに更新する。
                                                 */
                                                return new ViewCursor(0, nextChildViewCursor.offsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                                            }
                                            break;
                                        }
                                    case InsertType.AddContentToContainer:
                                        {
                                            Debug.LogError("AddContentToContainerが発生");
                                            linedElements.Add(insertingChild);
                                            break;
                                        }
                                }

                                // 特に何もないのでemptyを返す
                                return ViewCursor.Empty;
                            }
                        );

                        // この位置にコンテンツを置く
                        // Debug.Log("child:" + child.treeType + " nextChildViewCursor:" + nextChildViewCursor);

                        while (cor.MoveNext())
                        {
                            if (cor.Current != null)
                            {
                                break;
                            }
                            yield return null;
                        }

                        // update most right point.
                        if (mostRightPoint < child.offsetX + child.viewWidth)
                        {
                            mostRightPoint = child.offsetX + child.viewWidth;
                        }

                        // update most bottom point.
                        if (mostBottomPoint < child.offsetY + child.viewHeight)
                        {
                            mostBottomPoint = child.offsetY + child.viewHeight;
                        }

                        /*
                            <このコンテナ発のinsertion発動地点>
                            この時点でinsertionは発生済み or No で、発生している場合、そのタイプによって上位へと伝搬するイベントが変わる。
                         */

                        switch (currentInsertType)
                        {
                            case InsertType.Crlf:
                                {
                                    // 現在のコンテナの文字設定のデフォルト行高さを取得、次の行を開始する。
                                    var cor2 = GetDefaultHeightOfContainerText(containerTree);
                                    while (cor2.MoveNext())
                                    {
                                        yield return null;
                                    }

                                    var defaultTextHeight = cor2.Current;

                                    var childOffsetY = nextChildViewCursor.offsetY + defaultTextHeight;

                                    // 処理の開始時にラインにいれていたもの(crlf)を削除
                                    linedElements.Remove(child);

                                    // 含まれているものの整列処理をし、列の高さを受け取る
                                    var newLineOffsetY = DoLining(containerViewCursor.viewWidth, linedElements, alignMode);

                                    // 整列と高さ取得が完了したのでリセット
                                    linedElements.Clear();

                                    // 改行する文字高さ自体がlining後のoffsetYよりも高い場合、そちらを採用する
                                    if (newLineOffsetY < childOffsetY)
                                    {
                                        newLineOffsetY = childOffsetY;
                                    }

                                    nextChildViewCursor = ViewCursor.NextLine(newLineOffsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                                    continue;
                                }
                            case InsertType.RetryWithNextLine:
                                {
                                    // Debug.LogError("テキストコンテンツが指定した行に入らなかった。このコンテンツ自体をもう一度レイアウトする。 childIndex:" + childIndex + " nextChildViewCursor.offsetX:" + nextChildViewCursor.offsetX);

                                    // 処理の開始時にラインにいれていたものを削除
                                    linedElements.Remove(child);


                                    // 最初のコンテンツかつ、このコンテンツが予定されていた行に入らないのが確定した。
                                    if (childIndex == 0 && 0 < (float)child.keyValueStore[HTMLAttribute._ONLAYOUT_PRESET_X] && insertion != null)
                                    {
                                        // 親(このコンテキスト = コンテナ)のさらに親コンテナに、このコンテナの初行が入らず終わったことを伝える。
                                        insertion(InsertType.LineEndedWithoutNewContainerFirstLine, null);
                                        yield break;
                                    }

                                    // 現在までにラインに含まれているものの整列処理をし、最終的な列の高さを受け取る
                                    var newLineOffsetY = DoLining(containerViewCursor.viewWidth, linedElements, alignMode);

                                    // ここまでの列の整列と高さ取得が完了したのでリセット
                                    linedElements.Clear();

                                    // カーソルのセットを行う。
                                    nextChildViewCursor = ViewCursor.NextLine(newLineOffsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);

                                    // もう一度この行を処理する。
                                    goto currentLineRetry;
                                }

                            case InsertType.InsertContentToNextLine:
                                {
                                    /*
                                        子側、InsertContentToNextLineを発行してそれを親まで伝達するか、そのままにするか判定する。
                                     */
                                    if (0 < containerViewCursor.offsetX && insertion != null)
                                    {
                                        /*
                                            親がコンテナで、かつ、現在レイアウト中のこのコンテナで、行の途中から始まっていたコンテンツが幅を使い果たして
                                            自身のコンテナに対して改行(コンテンツの分割と挿入)を行なった。

                                            このコンテナからさらに親のコンテナに対して、折り返しが発生した要素を送りつける。

                                            親コンテナ側でさらにこのコンテナが行途中から開始したコンテナかどうかを判定、
                                            もし行途中から発生したコンテナであれば、その要素の中で送りつけられたtreeの要素をLiningに掛け、
                                            そのy位置を調整する。

                                            このイベント発生後の次の行以降のコンテンツは、そのy位置調整を経て調整される。
                                         */

                                        // Debug.LogError("insertion発生、現在の子コンテナ:" + Debug_GetTagStrAndType(containerTree));
                                        var newView = insertion(InsertType.HeadInsertedToTheEndOfLine, child);

                                        /*
                                            親コンテナからみて条件を満たしていれば、このコンテナに新たなviewが与えられる。
                                            条件は、このコンテナが、親から見て行途中に開始されたコンテナだったかどうか。
                                         */

                                        if (!newView.Equals(ViewCursor.Empty))
                                        {
                                            // Debug.LogError("viewが変更されてるので、コンテナ自体のviewが変更される。で、それに伴ってinsertしたコンテンツのx位置をズラさないといけない。 newView:" + newView);

                                            // 子のコンテンツのxOffsetを、コンテナのoffsetXが0になった際に相対的に移動しない、という前提でズラす。
                                            child.offsetX = containerViewCursor.offsetX;

                                            // update most right point again.
                                            if (mostRightPoint < child.offsetX + child.viewWidth)
                                            {
                                                mostRightPoint = child.offsetX + child.viewWidth;
                                            }

                                            // 改行が予定されているのでライン化を解除
                                            linedElements.Clear();

                                            // ビュー自体を更新
                                            containerViewCursor = newView;

                                            // 次の行のカーソルをセット
                                            nextChildViewCursor = ViewCursor.NextLine(
                                                child.offsetY + child.viewHeight,
                                                containerViewCursor.viewWidth,
                                                containerViewCursor.viewHeight
                                            );
                                            continue;
                                        }
                                    }

                                    /*
                                        これ以降のコンテンツは次行になるため、現在の行についてLining処理を行う。
                                     */
                                    var newLineOffsetY = DoLining(containerViewCursor.viewWidth, linedElements, alignMode);

                                    // 整列と高さ取得が完了したのでリセット
                                    linedElements.Clear();

                                    // ここまでの行の高さがcurrentHeightに出ているので、currentHeightから次の行を開始する。
                                    nextChildViewCursor = ViewCursor.NextLine(newLineOffsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                                    // Debug.LogError("child:" + child.tagValue + " done," + child.ShowContent() + " next childView:" + childView);
                                    continue;
                                }
                            case InsertType.TailInsertedToLine:
                                {
                                    // Debug.Log("TailInsertedToLine containerChildren.Count:" + containerChildren.Count);
                                    // foreach (var linedElement in linedElements)
                                    // {
                                    //     Debug.Log("linedElement:" + linedElement.treeType);
                                    // }

                                    if (1 < containerChildren.Count && childIndex == containerChildren.Count - 1 && insertion != null)
                                    {
                                        // Debug.Log("複数行あるコンテンツの最終行");
                                        // 文章の最終行が終わった。このコンテンツを含むコンテナへと伝達
                                        insertion(InsertType.LastLineEndedInTheMiddleOfLine, child);

                                        // Debug.Log("もしブロック要素なら、ここで改行しちゃえばいい。");
                                        break;
                                    }

                                    // 最終行ではないので何もしない。
                                    break;
                                }
                            case InsertType.LastLineEndedInTheMiddleOfLine:
                                {
                                    // Debug.Log("LastLineEndedInTheMiddleOfLine");
                                    /*
                                       状況として
                                       ・コンテナを跨いだLining
                                       があり得て、ここで処理している要素はとあるコンテナの最終行なので、そうなる素養がある。

                                       ・以降のコンテンツで改行が発生する = newLineへの移行が発生した瞬間に、やっと解放されるようなやつ。

                                       で、newLineが発生するまではいくらでもコンテナを跨いだLining要素を追加する必要がある。
                                    */
                                    var childContainer = child;

                                    /*
                                        現在のコンテナ
                                            子コンテナ
                                                孫テキスト
                                        のような構造になっていて、

                                        孫テキストが TailInsertedToLine 発行 ->
                                        子コンテナがそれを受け取り、孫テキストがコンテナ内での最終コンテンツであることを確認したのでさらに親へと LastLineEndedInTheMiddleOfLine 発行 ->
                                        現在のコンテナがそのイベントを受け取り、子コンテナ、孫コンテナに関しての整列処理を行う。

                                        まず子コンテナ自体の整列はその最終行までが完了したので、子コンテナを整列要素から外す。
                                     */
                                    linedElements.Remove(childContainer);

                                    /*
                                        次に、孫テキストは後続する別コンテナの要素によってLiningが行われるのに巻き込まれる可能性があるため、
                                        孫テキスト単体をこのコンテキストでのLiningに巻き込む。
                                     */
                                    var containersLastChild = childContainer.GetChildren().Last();
                                    linedElements.Add(containersLastChild);

                                    /*
                                        後続のコンテンツがありそれらが行に入りきらず改行が生まれるというタイミングで、
                                        孫テキストを含む行のレイアウトが完成する。
                                        
                                        その際、それ以降の行のオフセットとして、孫テキストを含んでいたコンテナのoffsetYが必要になる。
                                        
                                        現在のコンテナ
                                            孫テキスト( & 子コンテナのoffsetY) + 後続の同じ行内のコンテンツ x (0 ~ N)
                                            改行後にセットされるのが確定したコンテンツ,,

                                        という状態になった際、孫テキストの行の高さ + 子コンテナのoffsetYが、改行後にセットされるコンテンツのoffsetYになる。
                                     */

                                    // Debug.Log("LastLineEndedInTheMiddleOfLine childContainer.offsetY:" + childContainer.offsetY);

                                    containersLastChild.keyValueStore[HTMLAttribute._ONLAYOUT_LAYOUTED_OFFSET_Y] = childContainer.offsetY;

                                    // コンテナ内の最後のコンテンツの右から次のコンテンツが出るように、オフセットをセット。
                                    nextChildViewCursor = new ViewCursor(
                                        containersLastChild.viewWidth,
                                        (childContainer.offsetY + childContainer.viewHeight) - containersLastChild.viewHeight,
                                        containerViewCursor.viewWidth - (containersLastChild.offsetX + containersLastChild.viewWidth),
                                        containerViewCursor.viewHeight
                                    );

                                    // このコンテンツ自体は終了なので、次のコンテンツへと移動。
                                    continue;
                                }
                            case InsertType.LineEndedWithoutNewContainerFirstLine:
                                {
                                    // 直前までのコンテナの要素が入っているので、ここまでの要素で上位コンテナでのliningを行う。

                                    // 現在処理中のコンテンツはまだラインに含まない。先送りにする。
                                    linedElements.Remove(child);

                                    // 先ほどまでのコンテナの要素をLiningにかける。
                                    var newLineOffsetY = DoLining(containerViewCursor.viewWidth, linedElements, alignMode);

                                    // ここで、このliningの起点は直前のコンテナの末尾のコンテンツになっているので、ここで得られた高さにchildコンテンツが所属しているコンテナの起点を足す必要がある。その数値をkvから取り出す。
                                    var alreadyLayoutedFirstLinedContentParentOffsetY = 0f;
                                    if (0 < linedElements.Count && linedElements[0].keyValueStore.ContainsKey(HTMLAttribute._ONLAYOUT_LAYOUTED_OFFSET_Y))
                                    {
                                        alreadyLayoutedFirstLinedContentParentOffsetY = (float)linedElements[0].keyValueStore[HTMLAttribute._ONLAYOUT_LAYOUTED_OFFSET_Y];
                                    }

                                    linedElements.Clear();

                                    // カーソルの指定、オフセット位置はlining起点のコンテナの末尾のコンテンツの高さ + オフセット。
                                    nextChildViewCursor = ViewCursor.NextLine(newLineOffsetY + alreadyLayoutedFirstLinedContentParentOffsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);

                                    // ここで、このコンテナを再度やり直す。
                                    goto currentLineRetry;
                                }
                        }

                        /*
                            コンテンツがwidth内に置けた(少なくとも起点はwidth内にある)
                        */

                        // hiddenコンテンツ以下が来る場合は想定されてないのが惜しいな、なんかないかな、、ないか、、デバッグ用。crlf以外でheightが0になるコンテンツがあれば、それは異常なので蹴る
                        // if (!child.hidden && child.treeType != TreeType.Content_CRLF && cor.Current.viewHeight == 0) {
                        //     throw new Exception("content height is 0. tag:" + GetTagStr(child.tagValue) + " treeType:" + child.treeType);
                        // }

                        // 子供の設置位置を取得
                        var layoutedPos = cor.Current;
                        // Debug.LogError("layoutedPos:" + layoutedPos);

                        // 次のコンテンツの開始位置を取得
                        var nextPos = ChildPos.NextRightCursor(layoutedPos, containerViewCursor.viewWidth);
                        // Debug.LogError("nextPos:" + nextPos);

                        switch (currentInsertType)
                        {
                            case InsertType.LineEndedWithFilledImage:
                                {
                                    // ここでは、最後が画像コンテンツなので、その列の整列後の画像コンテンツのオフセットと高さを使うことで次の行のオフセットが出せる。
                                    DoLiningFromFirstContent(containerViewCursor.viewWidth, linedElements);
                                    var resizedImageContainer = linedElements[linedElements.Count - 1];
                                    var newLineOffsetY = resizedImageContainer.offsetY + resizedImageContainer.viewHeight;

                                    linedElements.Clear();
                                    nextChildViewCursor = ViewCursor.NextLine(newLineOffsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                                    // Debug.Log("LineEndedWithFilledImage nextChildViewCursor:" + nextChildViewCursor);
                                    break;
                                }
                            default:
                                {
                                    // レイアウト直後に次のポイントの開始位置が規定幅を超えている場合、現行の行のライニングを行う。
                                    if (containerViewCursor.viewWidth <= nextPos.offsetX)
                                    {
                                        // 行化
                                        var nextLineOffsetY = DoLining(containerViewCursor.viewWidth, linedElements, alignMode);

                                        // ライン解消
                                        linedElements.Clear();
                                        // Debug.LogError("over. child:" + GetTagStr(child.tagValue) + " vs containerViewCursor.viewWidth:" + containerViewCursor.viewWidth + " vs nextChildViewCursor.offsetX:" + nextChildViewCursor.offsetX);

                                        // 改行処理を加えた次の開始位置
                                        nextChildViewCursor = ViewCursor.NextLine(nextLineOffsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                                    }
                                    else
                                    {
                                        // 次のchildの開始ポイントを現在のchildの右にセット
                                        nextChildViewCursor = new ViewCursor(nextPos);
                                    }
                                    break;
                                }
                        }

                        // Debug.LogError("end, nextChildViewCursor:" + nextChildViewCursor);
                    }

                    // 現在の子供のレイアウトが終わっていて、なおかつライン処理、改行が済んでいる。
                }
            }


            // 最後の列が存在する場合、整列。(最後の要素が改行要因とかだと最後の列が存在しない場合がある)
            if (linedElements.Any())
            {
                DoLining(containerViewCursor.viewWidth, linedElements, alignMode);
            }

            // Debug.LogError("mostBottomPoint:" + mostBottomPoint + " tag:" + Debug_GetTagStrAndType(containerTree));

            // 自分自身のサイズを規定
            yield return containerTree.SetPos(containerViewCursor.offsetX, containerViewCursor.offsetY, mostRightPoint, mostBottomPoint);
        }

        private IEnumerator<float> GetDefaultHeightOfContainerText(TagTree textTree)
        {
            var textComponentCor = GetTextComponent(textTree);

            while (textComponentCor.MoveNext())
            {
                if (textComponentCor.Current != null)
                {
                    break;
                }
                yield return -1;
            }

            object textComponentSrc = textComponentCor.Current;

            // そのうち、列挙された型に対して適応できるように変える。

            if (textComponentSrc is Text)
            {
                var textComponent = (Text)textComponentSrc;
                textComponent.text = "A";

                var defaultHeight = textComponent.preferredHeight;
                textComponent.text = string.Empty;

                yield return defaultHeight;
            }
            else
            {
                if (textComponentSrc is TMPro.TextMeshProUGUI)
                {
                    var textComponent = (TMPro.TextMeshProUGUI)textComponentSrc;
                    textComponent.text = "A";

                    var defaultHeight = textComponent.preferredHeight;
                    textComponent.text = string.Empty;

                    yield return defaultHeight;
                }
            }


        }
        private static TMPro.TextMeshProUGUI tmGoComponent;

        /**
            列挙されたコンポーネント型をプレファブから取得試行してあったものを返す。
         */
        private IEnumerator<Component> GetTextComponent(TagTree textTree, string text = null)
        {
            var cor = resLoader.LoadPrefab(textTree.tagValue, textTree.treeType);

            while (cor.MoveNext())
            {
                if (cor.Current != null)
                {
                    break;
                }
                yield return null;
            }

            var prefab = cor.Current;

            Debug.LogWarning("今後、この部分を外部展開可能にする。コンポーネントを返せればいいので、型の列挙だけできれば良さそう。 prefab:" + prefab);

            var textComponent = prefab.GetComponent<Text>();
            if (textComponent != null)
            {
                if (textComponent.font == null)
                {
                    throw new Exception("font is null. prefab:" + resLoader.UUebTagsName() + "/" + prefab.name);
                }
                yield return textComponent;
            }
            else
            {
                Debug.LogWarning("固定でTMProの内容だけを扱う。まだこのコードは適当。どうやって最適化しようかな、、どんなコンポーネントがあるかが判断できればいいんだよな。");
                Debug.LogWarning("とりあえず動かそう。");
                if (tmGoComponent == null)
                {
                    var tmGo = GameObject.Instantiate(prefab);// 必須 これでTMProGUIを持ったGOが一個出来上がる。
                    tmGo.transform.SetParent(GameObject.Find("Canvas").transform);// 必須
                    tmGoComponent = tmGo.GetComponent<TMPro.TextMeshProUGUI>();
                    tmGoComponent.text = string.Empty;
                }
                else
                {
                    var t = prefab.GetComponent<TMPro.TextMeshProUGUI>();

                    // レイアウトだけだったらこれらのパラメータで足りる、みたいなのを集めて使おう。
                    tmGoComponent.font = t.font;
                    tmGoComponent.fontSize = t.fontSize;

                    tmGoComponent.text = string.Empty;
                }

                if (tmGoComponent.font == null)
                {
                    throw new Exception("font is null. prefab:" + resLoader.UUebTagsName() + "/" + prefab.name);
                }

                yield return tmGoComponent;
            }
        }

        /**
            テキストコンテンツのレイアウトを行う。
            もしテキストが複数行に渡る場合、最終行だけを新規コンテンツとして上位に返す。
         */
        private IEnumerator<ChildPos> DoTextLayout(TagTree textTree, ViewCursor textViewCursor, Func<InsertType, TagTree, ViewCursor> insertion = null)
        {
            var text = textTree.keyValueStore[HTMLAttribute._CONTENT] as string;

            var textComponentCor = GetTextComponent(textTree, text);

            while (textComponentCor.MoveNext())
            {
                if (textComponentCor.Current != null)
                {
                    break;
                }
                yield return null;
            }

            object textComponentSrc = textComponentCor.Current;


            IEnumerator<ChildPos> cor = null;

            // 列挙された型に対して、該当する型のcor<ChildPos>を返す関数を実行し、ienumを回す。

            if (textComponentSrc is Text)
            {
                cor = DoTextComponentLayout(textTree, (Text)textComponentSrc, text, textViewCursor, insertion);
            }
            if (textComponentSrc is TMPro.TextMeshProUGUI)
            {
                cor = DoTextMeshProComponentLayout(textTree, (TMPro.TextMeshProUGUI)textComponentSrc, text, textViewCursor, insertion);
            }

            while (cor.MoveNext())
            {
                if (cor.Current != null)
                {
                    break;
                }
                yield return null;
            }

            yield return cor.Current;
        }

        /**
            Text componentのレイアウトを決定して返す。
            レイアウト、改行などの必要に応じて文字列を分割する。
         */
        private IEnumerator<ChildPos> DoTextComponentLayout(TagTree textTree, Text textComponent, string text, ViewCursor textViewCursor, Func<InsertType, TagTree, ViewCursor> insertion = null)
        {
            // invalidate first.
            generator.Invalidate();

            // set content to prefab.

            textComponent.text = text;
            var setting = textComponent.GetGenerationSettings(new Vector2(textViewCursor.viewWidth, float.PositiveInfinity));
            generator.Populate(text, setting);

            using (new TextComponentUsing(textComponent, generator))
            {
                // この時点で、複数行に分かれるんだけど、最後の行のみ分離する必要がある。
                var lineCount = generator.lineCount;
                // Debug.LogError("lineCount:" + lineCount);
                // Debug.LogError("default preferred width:" + textComponent.preferredWidth);

                // 0行だったら、入らなかったということなので、改行をしてもらってリトライを行う。
                if (lineCount == 0 && !string.IsNullOrEmpty(textComponent.text))
                {
                    Debug.LogError("このケース存在しないかも。");
                    insertion(InsertType.RetryWithNextLine, null);
                    yield break;
                }

                // 1行以上のラインがある。

                /*
                    ここで、このtreeに対するカーソルのoffsetXが0ではない場合、行の中間から行を書き出していることになる。

                    また上記に加え、親コンテナ自体のoffsetXが0ではない場合も、やはり、行の中間から行を書き出していることになる。
                    判定のために、親コンテナからtextTreeへ、親コンテナのoffsetX = 書き始め位置の書き込みをする。

                    行が2行以上ある場合、1行目は右端まで到達しているのが確定する。
                    2行目以降はoffsetX=0の状態で書かれる必要がある。

                    コンテンツを分離し、それを叶える。
                */
                var onLayoutPresetX = (float)textTree.keyValueStore[HTMLAttribute._ONLAYOUT_PRESET_X];
                var isStartAtZeroOffset = onLayoutPresetX == 0 && textViewCursor.offsetX == 0;
                var isMultilined = 1 < lineCount;

                // 複数行存在するんだけど、2行目のスタートが0文字目の場合、1行目に1文字も入っていない。
                if (isMultilined && generator.lines[1].startCharIdx == 0)
                {
                    /*
                        行頭でこれが起きる場合、コンテンツ幅が圧倒的に不足していて、一文字も入らないということが起きている。
                        が、ここで文字を一切消費しないとなると後続の処理でも無限に処理が終わらない可能性があるため、最低でも1文字を消費する。
                    */
                    if (isStartAtZeroOffset)
                    {
                        // 最初の1文字目を強制的にセットする
                        var bodyContent = text.Substring(0, 1);

                        // 1文字目だけをこの文字列の内容としてセットし直す。
                        textTree.keyValueStore[HTMLAttribute._CONTENT] = bodyContent;

                        // 最終行として1文字目以降を取得、
                        var lastLineContent = text.Substring(1);

                        // 最終行を分割して送り出す。追加されたコンテンツを改行後に処理する。
                        var nextLineContent = new InsertedTree(textTree, lastLineContent, textTree.tagValue);
                        insertion(InsertType.InsertContentToNextLine, nextLineContent);


                        var charHeight = GetCharHeight(bodyContent, textComponent);
                        yield return textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textViewCursor.viewWidth, charHeight);
                        yield break;
                    }

                    // 行なかで、1行目のコンテンツがまるきり入らなかった。
                    // よって、改行を行なって次の行からコンテンツを開始する。
                    insertion(InsertType.RetryWithNextLine, null);
                    yield break;
                }

                if (isStartAtZeroOffset)
                {
                    if (isMultilined)
                    {
                        // Debug.LogError("行頭での折り返しのある複数行 text:" + text);

                        // 複数行が頭から出ている状態で、改行を含んでいる。最終行が中途半端なところにあるのが確定しているので、切り離して別コンテンツとして処理する必要がある。
                        var bodyContent = text.Substring(0, generator.lines[generator.lineCount - 1].startCharIdx);

                        // 内容の反映
                        textTree.keyValueStore[HTMLAttribute._CONTENT] = bodyContent;

                        // 最終行
                        var lastLineContent = text.Substring(generator.lines[generator.lineCount - 1].startCharIdx);

                        // 最終行を分割して送り出す。追加されたコンテンツを改行後に処理する。
                        var nextLineContent = new InsertedTree(textTree, lastLineContent, textTree.tagValue);
                        insertion(InsertType.InsertContentToNextLine, nextLineContent);

                        // 最終行以外はハコ型に収まった状態なので、ハコとして出力する。
                        // 最終一つ前までの高さを出して、このコンテンツの高さとして扱う。
                        var totalHeight = 0f + generator.lineCount - 1;// lineの高さだけを足すと、必ずlineCount-1ぶんだけ不足する。この挙動は謎。
                        for (var i = 0; i < generator.lineCount - 1; i++)
                        {
                            var line = generator.lines[i];
                            totalHeight += (line.height * textComponent.lineSpacing);
                        }

                        // このビューのポジションをセット
                        yield return textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textViewCursor.viewWidth, totalHeight);
                    }
                    else
                    {
                        // Debug.LogError("行頭の単一行 text:" + text + " textViewCursor.offsetY:" + textViewCursor.offsetY);
                        var width = textComponent.preferredWidth;
                        var height = (generator.lines[0].height * textComponent.lineSpacing);

                        // 最終行かどうかの判断はここでできないので、単一行の入力が終わったことを親コンテナへと通知する。
                        insertion(InsertType.TailInsertedToLine, textTree);

                        yield return textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, width, height);
                    }
                }
                else
                {
                    if (isMultilined)
                    {
                        // Debug.LogError("行中追加での折り返しのある複数行 text:" + text);
                        var currentLineHeight = (generator.lines[0].height * textComponent.lineSpacing);

                        // 複数行が途中から出ている状態で、まず折り返しているところまでを分離して、後続の文章を新規にstringとしてinsertする。
                        var currentLineContent = text.Substring(0, generator.lines[1].startCharIdx);
                        textTree.keyValueStore[HTMLAttribute._CONTENT] = currentLineContent;

                        // get preferredWidht of text from trimmed line.
                        textComponent.text = currentLineContent;

                        var currentLineWidth = textComponent.preferredWidth;

                        var restContent = text.Substring(generator.lines[1].startCharIdx);
                        var nextLineContent = new InsertedTree(textTree, restContent, textTree.tagValue);

                        // 次のコンテンツを新しい行から開始する。
                        insertion(InsertType.InsertContentToNextLine, nextLineContent);

                        yield return textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, currentLineWidth, currentLineHeight);
                    }
                    else
                    {
                        // Debug.LogError("行中追加の単一行 text:" + text);
                        var width = textComponent.preferredWidth;
                        var height = (generator.lines[0].height * textComponent.lineSpacing);

                        // Debug.LogError("行中の単一行 text:" + text + " textViewCursor:" + textViewCursor);
                        // 最終行かどうかの判断はここでできないので、単一行の入力が終わったことを親コンテナへと通知する。
                        insertion(InsertType.TailInsertedToLine, textTree);

                        // Debug.LogError("newViewCursor:" + newViewCursor);
                        yield return textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, width, height);
                    }
                }
            }
        }

        /**
            TextMesh Proのレイアウトを決定して返す
            レイアウト、改行などの必要に応じて文字列を分割する。
         */
        private IEnumerator<ChildPos> DoTextMeshProComponentLayout(TagTree textTree, TMPro.TextMeshProUGUI textComponent, string text, ViewCursor textViewCursor, Func<InsertType, TagTree, ViewCursor> insertion = null)
        {
            // Debug.LogError("DoTextMeshProComponentLayout text:" + text + " textViewCursor:" + textViewCursor);
            textComponent.text = text;

            // textComponent.に対してwidthをセットする必要がある。
            textComponent.rectTransform.sizeDelta = new Vector2(textViewCursor.viewWidth, float.PositiveInfinity);

            // このメソッドは、コンポーネントがgoにアタッチされてcanvasに乗っている場合のみ動作する。
            var textInfos = textComponent.GetTextInfo(text);

            // 各行の要素とパラメータを取得する。
            var tmGeneratorLines = textInfos.lineInfo;
            var lineSpacing = textComponent.lineSpacing;

            using (new TMProTextComponentUsing(textComponent))
            {
                var tmLineCount = textInfos.lineCount;
                var onLayoutPresetX = (float)textTree.keyValueStore[HTMLAttribute._ONLAYOUT_PRESET_X];
                // Debug.Log("text:" + text + " textViewCursor.viewWidth:" + textViewCursor.viewWidth);

                // 1行以上のラインが画面内にある。

                var isStartAtZeroOffset = onLayoutPresetX == 0 && textViewCursor.offsetX == 0;
                var isMultilined = 1 < tmLineCount;

                // Debug.LogWarning("TMProの場合の動作を丸っと変える必要がある。");

                // このコンテナの1行目を別のコンテナの結果位置 = 行中から書いた結果、この1行の幅が画面幅を超えている場合、全体を次の行に送る。
                // あ、この判定では無理だな、、分割されたコンテナの可能性が出てくる？ 整列を下からではなく上からやる必要がある。
                if (!isStartAtZeroOffset && textViewCursor.viewWidth < tmGeneratorLines[0].length)
                {
                    // 行なかで、1行目のコンテンツがまるきり入らなかった。
                    // よって、改行を行なって次の行からコンテンツを開始する。
                    // textTree.keyValueStore[HTMLAttribute._ONLAYOUT_PRESET_X] = 0.0f;
                    insertion(InsertType.RetryWithNextLine, null);
                    yield break;
                }

                // 複数行存在するんだけど、2行目のスタートが0文字目の場合、1行目に1文字も入っていない。
                if (isMultilined && tmGeneratorLines[1].firstCharacterIndex == 0)
                {
                    // 行頭でこれが起きる場合、コンテンツ幅が圧倒的に不足していて、一文字も入らないということが起きている。
                    // 1文字ずつ切り分けて表示する。
                    if (isStartAtZeroOffset)
                    {
                        // 最初の1文字目を強制的にセットする
                        var bodyContent = text.Substring(0, 1);

                        // 内容の反映
                        textTree.keyValueStore[HTMLAttribute._CONTENT] = bodyContent;

                        // 最終行
                        var lastLineContent = text.Substring(1);

                        // 最終行を分割して送り出す。追加されたコンテンツを改行後に処理する。
                        var nextLineContent = new InsertedTree(textTree, lastLineContent, textTree.tagValue);
                        insertion(InsertType.InsertContentToNextLine, nextLineContent);


                        var charHeight = (tmGeneratorLines[0].lineHeight + lineSpacing);
                        yield return textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textViewCursor.viewWidth, charHeight);
                        yield break;
                    }

                    // 行中からのコンテンツ追加で、複数行があるので、コンテンツ全体を次の行で開始させる。
                    insertion(InsertType.RetryWithNextLine, null);
                    yield break;
                }

                if (isStartAtZeroOffset)
                {
                    if (isMultilined)
                    {
                        // Debug.LogError("行頭での折り返しのある複数行 text:" + text + " textViewCursor.offsetX:" + textViewCursor.offsetX + " tmLineCount:" + tmLineCount);
                        /*
                            TMProのtextInfo上のレイアウト指示と、実際にレイアウトした時に自動的に分割されるワードに差がある。
                            abc が a\nbcになることもあれば、レイアウト時には分割されずabcで入ってしまうこともある。
                            これは予知できないので、textInfoでの分割を正にする方向で対処する。
                            具体的に言うと、文章に人力で\nを入れる。
                         */

                        var bodyContent = string.Empty;
                        var lastLineContent = string.Empty;

                        // TMProの場合、レイアウト時に文字を改行する場所と、実際にコンテンツを放り込んでしまって改行される箇所にズレがある。
                        // よってこの時点で、改行を含んだ文字列へと強制的に変更する。
                        for (var i = 0; i < tmLineCount; i++)
                        {
                            var lineInfo = tmGeneratorLines[i];
                            var lineText = text.Substring(lineInfo.firstCharacterIndex, lineInfo.lastCharacterIndex - lineInfo.firstCharacterIndex + 1);
                            if (i == tmLineCount - 1)
                            {
                                lastLineContent = lineText;
                                continue;
                            }

                            bodyContent += lineText + "\n";
                        }


                        // 内容の反映
                        textTree.keyValueStore[HTMLAttribute._CONTENT] = bodyContent;

                        // 最終行を分割して送り出す。追加されたコンテンツを改行後に処理する。
                        var nextLineContent = new InsertedTree(textTree, lastLineContent, textTree.tagValue);
                        insertion(InsertType.InsertContentToNextLine, nextLineContent);


                        // 最終行以外はハコ型に収まった状態なので、ハコとして出力する。
                        // 最終一つ前までの高さを出して、このコンテンツの高さとして扱う。
                        var totalHeight = 0f;
                        for (var i = 0; i < tmLineCount - 1; i++)
                        {
                            var line = tmGeneratorLines[i];
                            totalHeight += (line.lineHeight + lineSpacing);
                        }

                        // このビューのポジションをセット
                        yield return textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textViewCursor.viewWidth, totalHeight);
                    }
                    else
                    {
                        // Debug.LogError("行頭の単一行 text:" + text);
                        var currentLineWidth = textComponent.preferredWidth;
                        var currentLineHeight = (tmGeneratorLines[0].lineHeight + lineSpacing);

                        // 最終行かどうかの判断はここではできないので、単一行の入力が終わったことを親コンテナへと通知する。
                        insertion(InsertType.TailInsertedToLine, textTree);

                        var childPos = textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, currentLineWidth, currentLineHeight);

                        yield return childPos;
                    }
                }
                else
                {
                    if (isMultilined)
                    {
                        // Debug.LogError("行中追加での折り返しのある複数行 text:" + text);
                        var currentLineHeight = (tmGeneratorLines[0].lineHeight + lineSpacing);

                        // 複数行が途中から出ている状態で、まず折り返しているところまでを分離して、後続の文章を新規にstringとしてinsertする。
                        var currentLineContent = text.Substring(0, tmGeneratorLines[1].firstCharacterIndex);
                        textTree.keyValueStore[HTMLAttribute._CONTENT] = currentLineContent;

                        // get preferredWidht of text from trimmed line.
                        textComponent.text = currentLineContent;

                        var currentLineWidth = textComponent.preferredWidth;

                        var restContent = text.Substring(tmGeneratorLines[1].firstCharacterIndex);
                        var nextLineContent = new InsertedTree(textTree, restContent, textTree.tagValue);

                        // 次のコンテンツを新しい行から開始する。
                        insertion(InsertType.InsertContentToNextLine, nextLineContent);

                        // Debug.LogError("newViewCursor:" + newViewCursor);
                        yield return textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, currentLineWidth, currentLineHeight);
                    }
                    else
                    {
                        // Debug.LogError("行中追加の単一行 text:" + text);
                        var width = textComponent.preferredWidth;
                        var height = (tmGeneratorLines[0].lineHeight + lineSpacing);

                        // Debug.LogError("行中の単一行 text:" + text + " textViewCursor:" + textViewCursor);
                        // 最終行かどうかの判断はここでできないので、単一行の入力が終わったことを親コンテナへと通知する。
                        insertion(InsertType.TailInsertedToLine, textTree);

                        yield return textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, width, height);
                    }
                }
            }
        }


        private IEnumerator<ChildPos> DoCRLFLayout(TagTree crlfTree, ViewCursor viewCursor, Func<InsertType, TagTree, ViewCursor> insertion = null)
        {
            // 親へとcrlfを伝達することで、改行処理を行ってもらう。
            insertion(InsertType.Crlf, crlfTree);

            // コンテンツ自体は0サイズでセット、
            var currentViewCursor = ViewCursor.ZeroSizeCursor(viewCursor);
            yield return crlfTree.SetPosFromViewCursor(currentViewCursor);
        }


        private string Debug_GetTagStrAndType(TagTree tree)
        {
            return resLoader.GetTagFromValue(tree.tagValue) + "_" + tree.treeType;
        }

        private IEnumerator SetHiddenPosCoroutine(TagTree hiddenTree, IEnumerator<ChildPos> cor)
        {
            while (cor.MoveNext())
            {
                if (cor.Current != null)
                {
                    break;
                }
                yield return null;
            }

            hiddenTree.SetHidePos();
        }

        private float GetCharHeight(string headChara, Text textComponent)
        {
            generator.Invalidate();

            // set text for getting preferred height.
            textComponent.text = headChara;

            var setting = textComponent.GetGenerationSettings(new Vector2(textComponent.preferredWidth, float.PositiveInfinity));
            generator.Populate(headChara, setting);

            return generator.lines[0].height * textComponent.lineSpacing;
        }

        /**
            linedChildrenの中で一番高度のあるコンテンツをもとに、他のコンテンツを下揃いに整列させ、次の行の開始Yを返す。
            alignがある場合、Xも変更する。
            整列が終わったら、それぞれのコンテンツのオフセットをいじる。サイズは変化しない。
        */
        private float DoLining(float viewWidth, List<TagTree> linedChildren, AlignMode align)
        {
            var nextOffsetY = 0f;
            var tallestOffsetY = 0f;
            var tallestHeightPoint = 0f;

            for (var i = 0; i < linedChildren.Count; i++)
            {
                var child = linedChildren[i];

                /*
                    下端が一番下にあるコンテンツの値を取り出す
                 */
                if (tallestHeightPoint < child.offsetY + child.viewHeight)
                {
                    tallestOffsetY = child.offsetY;
                    tallestHeightPoint = child.offsetY + child.viewHeight;
                    nextOffsetY = tallestHeightPoint;
                }
            }

            // Debug.LogError("tallestHeightPoint:" + tallestHeightPoint);
            // tallestHeightを最大高さとして、各コンテンツのoffsetYを、この高さのコンテンツに下揃えになるように調整する。
            for (var i = 0; i < linedChildren.Count; i++)
            {
                var child = linedChildren[i];
                var diff = (tallestHeightPoint - tallestOffsetY) - child.viewHeight;

                child.offsetY = child.offsetY + diff;
            }

            switch (align)
            {
                case AlignMode.def:
                    {// or left
                     // do nothing.
                        break;
                    }
                case AlignMode.center:
                    {
                        // このラインの全コンテンツに対して、左端から右端までの長さを取り、offsetXの値を変更する。
                        var left = linedChildren.FirstOrDefault();
                        var right = linedChildren.LastOrDefault();
                        if (left != null && right != null)
                        {
                            // 0 ~ right edge.
                            var contentWidthSpace = viewWidth - (right.offsetX + right.viewWidth);
                            var leftAlignWidth = contentWidthSpace / 2;
                            for (var i = 0; i < linedChildren.Count; i++)
                            {
                                var child = linedChildren[i];
                                child.offsetX += leftAlignWidth;
                            }
                        }
                        break;
                    }
                case AlignMode.right:
                    {
                        // このラインの全コンテンツに対して、左端から右端までの長さを取り、offsetXの値を変更する。
                        var left = linedChildren.FirstOrDefault();
                        var right = linedChildren.LastOrDefault();
                        if (left != null && right != null)
                        {
                            // 0 ~ right edge.
                            var contentWidthSpace = viewWidth - (right.offsetX + right.viewWidth);
                            var leftAlignWidth = contentWidthSpace;
                            for (var i = 0; i < linedChildren.Count; i++)
                            {
                                var child = linedChildren[i];
                                child.offsetX += leftAlignWidth;
                            }
                        }
                        break;
                    }
            }

            // Debug.LogError("lining nextOffsetY:" + nextOffsetY);
            return nextOffsetY;
        }

        /**
            コンテナを跨いだコンテンツのライニング。基本的に下ぞろえ。
         */
        private float DoLiningFromFirstContent(float viewWidth, List<TagTree> linedChildren)
        {
            var tallest = linedChildren[0];
            for (var i = 0; i < linedChildren.Count; i++)
            {
                var child = linedChildren[i];

                /*
                    最大の高さのものを探す。
                 */
                if (tallest.viewHeight < child.viewHeight)
                {
                    tallest = child;
                }
            }

            var tallestHeight = tallest.viewHeight;
            for (var i = 0; i < linedChildren.Count; i++)
            {
                var child = linedChildren[i];
                var diff = tallestHeight - child.viewHeight;

                // Debug.Log("diff:" + diff);
                child.offsetY = child.offsetY + diff;
            }

            /*
                高さが最大のものを選んで、その高さを返す。
             */
            return tallest.viewHeight;
        }

        /**
            ボックス内部のコンテンツのレイアウトを行う
         */
        private IEnumerator<ChildPos> LayoutBoxedContents(TagTree boxTree, ViewCursor boxView)
        {
            // Debug.LogError("boxTree:" + GetTagStr(boxTree.tagValue) + " boxView:" + boxView);

            var containerChildren = boxTree.GetChildren();
            var childCount = containerChildren.Count;

            if (childCount == 0)
            {
                // treeに位置をセットしてposを返す
                yield return boxTree.SetPosFromViewCursor(boxView);
                throw new Exception("never come here.");
            }

            // 内包されたviewCursorを生成する。
            var childView = ViewCursor.ZeroOffsetViewCursor(boxView);

            for (var i = 0; i < childCount; i++)
            {
                var child = containerChildren[i];
                if (child.treeType == TreeType.Content_Text)
                {
                    child.keyValueStore[HTMLAttribute._ONLAYOUT_PRESET_X] = boxTree.offsetX;
                }

                // 子供ごとにレイアウトし、結果を受け取る
                var cor = DoLayout(
                    child,
                    childView,
                    (insertType, newChild) =>
                    {
                        throw new Exception("never come here.");
                    }
                );

                while (cor.MoveNext())
                {
                    if (cor.Current != null)
                    {
                        break;
                    }
                    yield return null;
                }

                /*
                    コンテンツがwidth内に置けた(ギリギリを含む)
                */

                // レイアウトが済んだchildの位置を受け取り、改行
                // Debug.LogError("layoutbox 改行");
                childView = ViewCursor.NextLine(cor.Current.offsetY + cor.Current.viewHeight, boxView.viewWidth, boxView.viewHeight);

                // 現在の子供のレイアウトが終わっていて、なおかつライン処理、改行が済んでいる。
            }

            // 最終コンテンツのoffsetを使ってboxの高さをセット
            // treeに位置をセットしてposを返す
            yield return boxTree.SetPos(boxView.offsetX, boxView.offsetY, boxView.viewWidth, childView.offsetY);
        }


        private class TextComponentUsing : IDisposable
        {
            private Text textComponent;
            private TextGenerator gen;
            public TextComponentUsing(Text textComponent, TextGenerator gen)
            {
                this.textComponent = textComponent;
                this.gen = gen;
            }

            private bool disposedValue = false;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // dispose.
                        textComponent.text = string.Empty;
                        gen.Invalidate();
                    }
                    disposedValue = true;
                }
            }

            void IDisposable.Dispose()
            {
                Dispose(true);
            }
        }

        private class TMProTextComponentUsing : IDisposable
        {
            private TMPro.TextMeshProUGUI textComponent;
            public TMProTextComponentUsing(TMPro.TextMeshProUGUI textComponent)
            {
                this.textComponent = textComponent;
            }

            private bool disposedValue = false;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // dispose.
                        textComponent.text = string.Empty;
                    }
                    disposedValue = true;
                }
            }

            void IDisposable.Dispose()
            {
                Dispose(true);
            }
        }

        /*
            table functions.
         */

        // private class TableLayoutRecord {
        // 	private int rowIndex;
        // 	private List<float> xWidth = new List<float>();

        // 	public void IncrementRow () {
        // 		xWidth.Add(0);
        // 	}

        // 	public void UpdateMaxWidth (float width) {
        // 		if (xWidth[rowIndex] < width) {
        // 			xWidth[rowIndex] = width;
        // 		}
        // 		rowIndex = (rowIndex + 1) % xWidth.Count;
        // 	}
        // 	public float TotalWidth () {
        // 		var ret = 0f;
        // 		foreach (var width in xWidth) {
        // 			ret += width;
        // 		}
        // 		return ret;
        // 	}

        // 	public OffsetAndWidth GetOffsetAndWidth () {
        // 		var currentIndex = rowIndex % xWidth.Count;
        // 		var offset = 0f;
        // 		for (var i = 0; i < currentIndex; i++) {
        // 			offset += xWidth[i];
        // 		}
        // 		var width = xWidth[rowIndex % xWidth.Count];

        // 		rowIndex++;

        // 		return new OffsetAndWidth(offset, width);
        // 	}

        // 	public struct OffsetAndWidth {
        // 		public float offset;
        // 		public float width;
        // 		public OffsetAndWidth (float offset, float width) {
        // 			this.offset = offset;
        // 			this.width = width;
        // 		}
        // 	}
        // }

        // private void CollectTableContentRowCountRecursively (ParsedTree @this, ParsedTree child, TableLayoutRecord tableLayoutRecord) {
        // 	// count up table header count.
        // 	if (child.parsedTag == (int)HtmlTag.th) {
        // 		tableLayoutRecord.IncrementRow();
        // 	}

        // 	foreach (var nestedChild in child.GetChildren()) {
        // 		CollectTableContentRowCountRecursively(child, nestedChild, tableLayoutRecord);
        // 	}
        // }

        // private void CollectTableContentRowMaxWidthsRecursively (ParsedTree @this, ParsedTree child, TableLayoutRecord tableLayoutRecord) {
        // 	var total = 0f;
        // 	foreach (var nestedChild in child.GetChildren()) {
        // 		CollectTableContentRowMaxWidthsRecursively(child, nestedChild, tableLayoutRecord);
        // 		if (child.parsedTag == (int)HtmlTag.th || child.parsedTag == (int)HtmlTag.td) {
        // 			var nestedChildContentWidth = nestedChild.sizeDelta.x;
        // 			total += nestedChildContentWidth;
        // 		}
        // 	}

        // 	if (child.parsedTag == (int)HtmlTag.th || child.parsedTag == (int)HtmlTag.td) {
        // 		tableLayoutRecord.UpdateMaxWidth(total);
        // 	}
        // }

        // private void SetupTableContentPositionRecursively (ParsedTree @this, ParsedTree child, TableLayoutRecord tableLayoutRecord) {
        // 	// overwrite parent content width of TH and TD.
        // 	if (child.parsedTag == (int)HtmlTag.thead || child.parsedTag == (int)HtmlTag.tbody || child.parsedTag == (int)HtmlTag.thead || child.parsedTag == (int)HtmlTag.tr) {
        // 		var width = tableLayoutRecord.TotalWidth();
        // 		child.sizeDelta = new Vector2(width, child.sizeDelta.y);
        // 	}

        // 	/*
        // 		change TH, TD content's x position and width.
        // 		x position -> 0, 1st row's longest content len, 2nd row's longest content len,...
        // 		width -> 1st row's longest content len, 2nd row's longest content len,...
        // 	*/
        // 	if (child.parsedTag == (int)HtmlTag.th || child.parsedTag == (int)HtmlTag.td) {
        // 		var offsetAndWidth = tableLayoutRecord.GetOffsetAndWidth();

        // 		child.anchoredPosition = new Vector2(offsetAndWidth.offset, child.anchoredPosition.y);
        // 		child.sizeDelta = new Vector2(offsetAndWidth.width, child.sizeDelta.y);
        // 	}

        // 	foreach (var nestedChild in child.GetChildren()) {
        // 		SetupTableContentPositionRecursively(child, nestedChild, tableLayoutRecord);	
        // 	}
        // }
    }
}