using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UUebView
{
    public class DefaultBehaviour : IPluggable
    {

        private readonly TextGenerator generator;
        public DefaultBehaviour()
        {
            this.generator = new TextGenerator();
        }

        float IPluggable.GetDefaultHeightOfContainerText(Component textComponentSrc)
        {
            return GetDefaultHeightOfContainerText(textComponentSrc);
        }

        public float GetDefaultHeightOfContainerText(Component textComponentSrc)
        {
            var textComponent = (Text)textComponentSrc;
            textComponent.text = "A";

            var defaultHeight = textComponent.preferredHeight;
            textComponent.text = string.Empty;

            return defaultHeight;
        }

        void IPluggable.SetText(GameObject targetGameObject, string text)
        {
            var textComponent = targetGameObject.GetComponent<Text>();
            if (textComponent != null)
            {
                textComponent.text = text;
                return;
            }

            throw new Exception("failed to set text to component:" + targetGameObject);
        }

        Component IPluggable.TextComponent(GameObject prefab, string uuebTagsName)
        {
            var textComponent = prefab.GetComponent<Text>();
            if (textComponent != null)
            {
                if (textComponent.font == null)
                {
                    throw new Exception("font is null. prefab:" + uuebTagsName + "/" + prefab.name);
                }
                return textComponent;
            }

            throw new Exception("failed to set text to component:" + prefab + " target component type is not Text component. not Text component is unsupported by default. please search plugin for component.");
        }

        IEnumerator<ChildPos> IPluggable.TextLayoutCoroutine(Component sourceComponent, TagTree textTree, string text, ViewCursor textViewCursor, Func<InsertType, TagTree, ViewCursor> insertion)
        {
            return DoTextComponentLayout(textTree, (Text)sourceComponent, text, textViewCursor, insertion);
        }

        /**
            uGUIのText componentのレイアウトを決定して返す。
            レイアウト、改行などの必要に応じて文字列を分割する。
         */
        public IEnumerator<ChildPos> DoTextComponentLayout(TagTree textTree, Text textComponent, string text, ViewCursor textViewCursor, Func<InsertType, TagTree, ViewCursor> insertion = null)
        {
            // Debug.Log("DoTextComponentLayout text:" + text.Length + " textViewCursor:" + textViewCursor);

            // invalidate first.
            generator.Invalidate();

            // set content to prefab.

            textComponent.text = text;
            var setting = textComponent.GetGenerationSettings(new Vector2(textViewCursor.viewWidth, float.PositiveInfinity));
            generator.Populate(text, setting);

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
        private float GetCharHeight(string headChara, Text textComponent)
        {
            generator.Invalidate();

            // set text for getting preferred height.
            textComponent.text = headChara;

            var setting = textComponent.GetGenerationSettings(new Vector2(textComponent.preferredWidth, float.PositiveInfinity));
            generator.Populate(headChara, setting);

            return generator.lines[0].height * textComponent.lineSpacing;
        }

    }
}