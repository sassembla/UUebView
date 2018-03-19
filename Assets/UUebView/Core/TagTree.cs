using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace UUebView
{

    public enum TreeType
    {
        NotFound,
        Container,
        Content_Text,
        Content_Img,
        Content_CRLF,
        CustomLayer,
        CustomBox,
        CustomEmptyLayer,
    }

    public enum AlignMode
    {
        def,
        left = def,
        center,
        right
    }

    public class ParsedTree : TagTree
    {
        public List<ParseError> errors = new List<ParseError>();
        public static string ShowErrors(ParsedTree tree)
        {
            var stringBuilder = new StringBuilder();
            foreach (var error in tree.errors)
            {
                stringBuilder.AppendLine("error code:" + error.code + " reason:" + error.reason);
            }
            return stringBuilder.ToString();
        }
    }

    /**
        tree structure.
     */
    public class TagTree
    {
        // tree params.
        public readonly string id;
        private List<TagTree> _children = new List<TagTree>();

        // tag params.
        public readonly int tagValue;
        public readonly AttributeKVs keyValueStore;
        public readonly TreeType treeType;

        public bool hidden
        {
            get; private set;
        }

        private readonly bool hiddenDefault;

        // レイアウト処理
        public float offsetX;
        public float offsetY;
        public float viewWidth;
        public float viewHeight;

        public TagTree()
        {
            this.id = Guid.NewGuid().ToString();
            this.tagValue = (int)HTMLTag._ROOT;
            this.keyValueStore = new AttributeKVs();

            this.hiddenDefault = false;

            this.treeType = TreeType.Container;
        }

        public TagTree(int tagValue)
        {
            this.id = Guid.NewGuid().ToString();
            this.tagValue = tagValue;
            this.keyValueStore = new AttributeKVs();

            this.hiddenDefault = false;

            this.treeType = TreeType.Content_CRLF;
        }

        public TagTree(string textContent, int baseTagValue)
        {// as text_content.
            this.id = Guid.NewGuid().ToString();
            this.tagValue = baseTagValue;

            this.keyValueStore = new AttributeKVs();
            keyValueStore[HTMLAttribute._CONTENT] = textContent;

            this.hiddenDefault = false;

            this.treeType = TreeType.Content_Text;
        }

        public TagTree(string baseId, string textContent, int baseTagValue)
        {// as inserted text_content.
            this.id = baseId + ".";
            this.tagValue = baseTagValue;

            this.keyValueStore = new AttributeKVs();
            keyValueStore[HTMLAttribute._CONTENT] = textContent;

            this.hiddenDefault = false;

            this.treeType = TreeType.Content_Text;
        }

        public TagTree(int parsedTag, AttributeKVs kv, TreeType treeType)
        {
            this.id = Guid.NewGuid().ToString();
            this.tagValue = parsedTag;
            this.keyValueStore = kv;
            this.treeType = treeType;

            if (kv.ContainsKey(HTMLAttribute.HIDDEN) && kv[HTMLAttribute.HIDDEN] as string == "true")
            {
                hidden = true;
                this.hiddenDefault = hidden;
            }
            else
            {
                this.hiddenDefault = false;
            }
        }

        public void ShowOrHide()
        {
            hidden = !hidden;

            if (hidden)
            {
                SetHidePos();
            }
        }
        public void SetHidePos()
        {
            offsetX = 0;
            offsetY = 0;
            viewWidth = 0;
            viewHeight = 0;
        }

        public ChildPos SetPos(float offsetX, float offsetY, float viewWidth, float viewHeight)
        {
            this.offsetX = offsetX;
            this.offsetY = offsetY;
            this.viewWidth = viewWidth;
            this.viewHeight = viewHeight;
            return new ChildPos(this);
        }

        public ChildPos SetPos(ChildPos pos)
        {
            this.offsetX = pos.offsetX;
            this.offsetY = pos.offsetY;
            this.viewWidth = pos.viewWidth;
            this.viewHeight = pos.viewHeight;
            return pos;
        }

        public ChildPos SetPosFromViewCursor(ViewCursor source)
        {
            this.offsetX = source.offsetX;
            this.offsetY = source.offsetY;
            this.viewWidth = source.viewWidth;
            this.viewHeight = source.viewHeight;
            return new ChildPos(this);
        }

        public bool SetParent(TagTree parent)
        {
            // emptylayer cannot have child text content directory.
            if (parent.treeType == TreeType.CustomEmptyLayer && this.treeType == TreeType.Content_Text)
            {
                return false;
            }

            parent._children.Add(this);

            // inherit specific kv to child if child does not have kv.
            if (this.treeType == TreeType.Content_Text)
            {
                var inheritableAttributes = ConstSettings.ShouldInheritAttributes.Intersect(parent.keyValueStore.Keys).ToArray();
                if (inheritableAttributes.Any())
                {
                    foreach (var attr in inheritableAttributes)
                    {
                        this.keyValueStore[attr] = parent.keyValueStore[attr];
                    }
                }
            }
            return true;
        }

        public List<TagTree> GetChildren()
        {
            return _children;
        }


        public void AddChildren(TagTree[] children)
        {
            this._children.AddRange(children);
        }

        public void RemoveChild(TagTree child)
        {
            this._children.Remove(child);
        }

        public void ReplaceChildrenToBox(TagTree[] oldTrees, TagTree newTree)
        {
            foreach (var oldTree in oldTrees)
            {
                this._children.Remove(oldTree);
            }

            this._children.Add(newTree);
        }

        public static string ShowContent(TagTree tree)
        {
            return "val:" + tree.tagValue + " type:" + tree.treeType.ToString();
        }

        public static string ShowWholeContent(TagTree tree)
        {
            var builder = new StringBuilder();
            builder.AppendLine(ShowContent(tree));

            foreach (var child in tree.GetChildren())
            {
                var childInf = "    " + ShowWholeContent(child);
                builder.AppendLine(childInf);
            }
            return builder.ToString();
        }


        /**
            画面幅、高さから、uGUIの計算を行って実際のレイアウト時のパラメータを算出する。
            パラメータとして渡って来るboxPosには、親のサイズに対してのレイアウト値が含まれた状態。
            
         */
        public static Rect GetChildViewRectFromParentRectTrans(float parentWidth, float parentHeight, BoxPos pos, out float rightPadding, out float bottomPadding)
        {
            /*
                出せるものは次の要素。
                ・要素の起点位置(左上オフセット)
                ・要素のサイズ
                ・要素のパディング(右下オフセット)

                基本的に、親のサイズに対して各数値が決まる。

                まず左下のポイントが定まる。これは、anchorMinがどこを指すかに影響される。
                xが0の場合、offsetMinは左からの値になる。
                xが1の場合、offsetMinは右からの値になる。

                アンカー座標系(右上に加算される系)において、

                左下のX位置
                    anchorMinXという値が置けて、
                    anchorMinX = width * anchorMin.x(0で左端、1で右端になる)
                    左下のX位置は、(width * anchorMin.x) + offsetMin.x になる。

                左下のY位置
                    anchorMinYという値が置けて、
                    anchorMinY = height * anchorMin.y(0で下端、1で上端になる)
                    左下のYの位置は、(height * anchorMin.y) + offsetMin.y になる。

                右上のX位置
                    anchorMaxXという値が置けて、
                    anchorMaxX = width * anchorMax.x(0で左端、1で右端になる)
                    右上のX位置は、(width * anchorMax.x) + offsetMax.x になる。

                右上のY位置
                    anchorMaxYという値が置けて、
                    anchorMaxY = height * anchorMax.y(0で下端、1で上端になる)
                    右上のYの位置は、(height * anchorMax.y) + offsetMax.y になる。

                となる。

                これらの値によって、anchorによって決められた描画位置とサイズが得られる。

                y値は調整が必要で、下から上の変な系になっているため、実際のparentHeightから引く、という形になる。
                
                また、heightは、下からの系なので、常にbottomY > topYとなるため、bottomY - topYとなる。

                各anchor値に対して、異常値(親の予算を無視した高い値)を指定できるため、
                実際の親のサイズを使って導出する位置値にリミッターをかける。
             */
            var leftX = (parentWidth * pos.anchorMin.x) + pos.offsetMin.x;
            if (leftX < 0)
            {
                leftX = 0f;
            }

            var rightX = (parentWidth * pos.anchorMax.x) + pos.offsetMax.x;
            if (parentWidth < rightX)
            {
                rightX = parentWidth;
            }

            var bottomY = parentHeight - ((parentHeight * pos.anchorMin.y) + pos.offsetMin.y);
            if (parentHeight < bottomY)
            {
                bottomY = parentHeight;
            }

            var topY = parentHeight - ((parentHeight * pos.anchorMax.y) + pos.offsetMax.y);
            if (topY < 0)
            {
                topY = 0f;
            }

            var viewWidth = rightX - leftX;
            var viewHeight = bottomY - topY;

            /*
                右下の余白
                    この値を実際に採用するかは、boxを並べる側で判断する。
             */
            rightPadding = parentWidth - (leftX + viewWidth);
            if (rightPadding < 0)
            {
                rightPadding = 0f;
            }

            bottomPadding = parentHeight - (topY + viewHeight);
            if (bottomPadding < 0)
            {
                if (bottomPadding.ToString() == "-317.7")
                {
                    Debug.LogWarning("bottomPadding:" + bottomPadding + " negativeに入った、条件は:" + pos + " vs parentHeight:" + parentHeight);
                    /*
                        bottomPadding:-317.7 
                        offsetMin:(-760.0, -407.7) 
                        offsetMax:(760.0, -9.9) 
                        pivot:(0.5, 0.5) 
                        anchorMin:(0.5, 1.0) 
                        anchorMax:(0.5, 1.0) vs 

                        parentHeight:90 なので、上辺からの扱いか。 yのoffsetYすげーな。上からなので、下に407潜ると。
                        この時のtopYとかbottomYどうなってんだろ。そのへんから間違ってる気がする。

                        topY:9.900024 bottomY:407.7

                        parentHeight - ((parentHeight * pos.anchorMin.y) + pos.offsetMin.y)
                        90 - 
                            (parentHeight * pos.anchorMin.y) + pos.offsetMin.y
                            90 * 1 -407 - 90 ふむ、、、
                        
                        bottomYは407になる。
                        これがそもそも間違ってる気がしないでもない。ビューをはるかに超えるoffsetYってなんなの。

                        で、topYは、parentHeight - ((parentHeight * pos.anchorMax.y) + pos.offsetMax.y);
                        90 - 
                            (1 * 90 - 9.9) で、9.9になる。ふむ。

                        viewHeightがいい感じにheightを超えてるんだよね。入れただけで超えるってそうとうなアレだと思うんだけど。

                        viewHeightは397.8になる。親が90なのに。ふむ。

                        -407が入ってるのが最初から異常値みたいな感じか。
                        
                        であれば、ここでやるべきなのは、最初にtopとbottomを出した時に、異常値を丸める事かもしれない。明らかにheightよりも高い範囲に行った場合、それを丸めるみたいな。

                     */
                    Debug.LogWarning("topY:" + topY + " bottomY:" + bottomY + " viewHeight:" + viewHeight);
                }
                bottomPadding = 0f;
            }

            // Debug.Log("leftX:" + leftX + " rightX:" + rightX + " topY:" + topY + " bottomY:" + bottomY + " width:" + viewWidth + " height:" + viewHeight + " rightPadding:" + rightPadding + " bottomPadding:" + bottomPadding);
            // Debug.LogWarning("parentHeight:" + parentHeight + " topY:" + topY + " viewHeight:" + viewHeight + " bottomPadding:" + bottomPadding);

            var resultRect = new Rect(leftX, topY, viewWidth, viewHeight);
            return resultRect;
        }

        public static Vector2 AnchoredPositionOf(TagTree tree)
        {
            return new Vector2(tree.offsetX, -tree.offsetY);
        }

        public static Vector2 SizeDeltaOf(TagTree tree)
        {
            return new Vector2(tree.viewWidth, tree.viewHeight);
        }

        /**
            ・!hiddenなtreeのid列挙
            ・レイアウト変更をする予定なので、InsertedTreeの解消
         */
        public static string[] CorrectTrees(TagTree rootTree)
        {
            // ShowLayoutRecursive(rootTree);
            var usingIds = new List<string>();
            CorrectRecursive(rootTree, usingIds);
            return usingIds.ToArray();
        }

        public static void ShowLayoutRecursive(TagTree tree)
        {
            Debug.Log("tree:" + tree.tagValue + " treeType:" + tree.treeType + " offsetX:" + tree.offsetX + " offsetY:" + tree.offsetY + " width:" + tree.viewWidth + " height:" + tree.viewHeight);
            foreach (var child in tree.GetChildren())
            {
                ShowLayoutRecursive(child);
            }
        }

        private static void CorrectRecursive(TagTree tree, List<string> usingIds)
        {
            var isUsing = !tree.hidden;

            if (isUsing)
            {
                usingIds.Add(tree.id);
            }

            var children = tree.GetChildren();

            /*
                前方に元tree、後方に挿入treeがある場合があるので、
                childrenを逆にした配列を用意して畳み込みを行う。
             */
            var removeTargets = new List<TagTree>();
            foreach (var reverted in children.AsEnumerable().Reverse())
            {
                CorrectRecursive(reverted, usingIds);

                if (reverted is InsertedTree)
                {
                    var insertedTree = reverted as InsertedTree;
                    var baseTree = insertedTree.parentTree;

                    // merge contents to base.
                    baseTree.keyValueStore[HTMLAttribute._CONTENT] = baseTree.keyValueStore[HTMLAttribute._CONTENT] as string + insertedTree.keyValueStore[HTMLAttribute._CONTENT] as string;

                    removeTargets.Add(insertedTree);
                }
            }

            foreach (var removeTarget in removeTargets)
            {
                tree.RemoveChild(removeTarget);
            }
        }


        public static void ResetHideFlags(TagTree layoutedTree)
        {
            ResetRecursive(layoutedTree);
        }
        private static void ResetRecursive(TagTree tree)
        {
            tree.hidden = tree.hiddenDefault;
            foreach (var child in tree.GetChildren())
            {
                ResetRecursive(child);
            }
        }


        public static string[] CollectTreeIds(TagTree layoutedTree)
        {
            var treeIds = new List<string>();
            CollectTreeIdsRecursive(layoutedTree, treeIds);
            return treeIds.ToArray();
        }
        private static void CollectTreeIdsRecursive(TagTree tree, List<string> treeIds)
        {
            if (tree.keyValueStore.ContainsKey(HTMLAttribute.ID))
            {
                treeIds.Add(tree.keyValueStore[HTMLAttribute.ID] as string);
            }

            foreach (var child in tree.GetChildren())
            {
                CollectTreeIdsRecursive(child, treeIds);
            }
        }


        public static TagTree[] GetTreeById(TagTree root, string id)
        {
            var targetTree = new List<TagTree>();
            FindTreeByIdRecursively(root, id, targetTree);
            return targetTree.ToArray();
        }

        private static void FindTreeByIdRecursively(TagTree tree, string id, List<TagTree> collectingTrees)
        {
            if (tree.keyValueStore.ContainsKey(HTMLAttribute.ID))
            {
                var idCandidate = tree.keyValueStore[HTMLAttribute.ID] as string;
                if (idCandidate == id)
                {
                    collectingTrees.Add(tree);
                }
            }

            foreach (var child in tree.GetChildren())
            {
                FindTreeByIdRecursively(child, id, collectingTrees);
            }
        }
    }

    public class InsertedTree : TagTree
    {
        public readonly TagTree parentTree;
        public InsertedTree(TagTree baseTree, string textContent, int baseTag) : base(baseTree.id, textContent, baseTag)
        {
            this.parentTree = baseTree;
        }
    }
}