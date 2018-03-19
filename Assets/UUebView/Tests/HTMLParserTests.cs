// using Miyamasu;
// using UnityEngine;
// using System.Collections.Generic;
// using System;
// using System.Linq;
// using UnityEngine.UI;
// using UnityEngine.Events;
// using UUebView;
// using System.Collections;

// /**
//     test for html parser.
//  */
// public class HTMLParserTests : MiyamasuTestRunner
// {
//     private HTMLParser parser;

//     private ResourceLoader loader;

//     private UUebView.UUebViewComponent executor;

//     [MSetup]
//     public void Setup()
//     {

//         executor = new GameObject("htmlParserTest").AddComponent<UUebViewComponent>();
//         var core = new UUebView.UUebViewCore(executor);
//         executor.SetCore(core);
//         loader = new ResourceLoader(executor.Core.CoroutineExecutor);

//         parser = new HTMLParser(loader);
//     }

//     [MTeardown]
//     public void Teardown()
//     {
//         GameObject.DestroyImmediate(executor);
//         GameObject.DestroyImmediate(loader.cacheBox);
//     }

//     public static void ShowRecursive(TagTree tree, ResourceLoader loader)
//     {
//         // Debug.Log("parsedTag:" + loader.GetTagFromValue(tree.tagValue) + " type:" + tree.treeType);
//         foreach (var child in tree.GetChildren())
//         {
//             ShowRecursive(child, loader);
//         }
//     }

//     private int CountContentsRecursive(TagTree tree)
//     {
//         // Debug.Log("tag:" + loader.GetTagFromValue(tree.tagValue));
//         var children = tree.GetChildren();
//         var count = 0;
//         foreach (var child in children)
//         {
//             count += CountContentsRecursive(child);
//         }
//         return count + 1;// add this content count.
//     }

//     private IEnumerator GetParsedRoot(string sampleHtml, Action<ParsedTree> ret)
//     {
//         ParsedTree parsedRoot = null;
//         var cor = parser.ParseRoot(
//             sampleHtml,
//             parsed =>
//             {
//                 parsedRoot = parsed;
//             }
//         );
//         executor.Core.CoroutineExecutor(cor);

//         yield return WaitUntil(
//             () => parsedRoot != null, () => { throw new TimeoutException("too late."); }, 1
//         );

//         ret(parsedRoot);
//     }

//     [MTest]
//     public IEnumerator LoadSimpleHTML()
//     {
//         var sampleHtml = @"
// <body>something</body>
//         ";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         var children = parsedRoot.GetChildren();

//         True(children.Count == 1, "not match. children.Count:" + children.Count);
//     }

//     [MTest]
//     public IEnumerator LoadDepthAssetListIsDone()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/ParserTest/UUebTags'>
// <body>something</body>
//         ";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         True(!loader.IsLoadingUUebTags, "still loading.");
//     }

//     [MTest]
//     public IEnumerator LoadDepthAssetListWithCustomTag()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/ParserTest/UUebTags'>
// <customtag><customtagtext>something</customtagtext></customtag>
//         ";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         True(!loader.IsLoadingUUebTags, "still loading.");
//     }


//     // 解析した階層が想定通りかどうか

//     [MTest]
//     public IEnumerator ParseSimpleHTML()
//     {
//         var sampleHtml = @"
// <body>something</body>
//         ";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });
//         var children = parsedRoot.GetChildren();

//         True(parsedRoot.GetChildren().Count == 1, "not match.");
//         True(parsedRoot.GetChildren()[0].tagValue == (int)HTMLTag.body, "not match.");

//     }

//     [MTest]
//     public IEnumerator ParseCustomTag()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/ParserTest/UUebTags'>
// <customtag><customtagpos><customtagtext>something</customtagtext></customtagpos></customtag>
//         ";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         // loader contains 4 additional custom tags.
//         var count = loader.GetAdditionalTagCount();
//         True(count == 4, "not match. count:" + count);
//     }

//     [MTest]
//     public IEnumerator ParseCustomTagMoreDeep()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/ParserTest/UUebTags'>
// <customtag><customtagpos><customtagtext>
//     <customtag2><customtagtext2><customtagtext>something</customtagtext></customtagtext2></customtag2>
// </customtagtext></customtagpos></customtag>
//         ";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         // loader contains 7 additional custom tags.
//         var count = loader.GetAdditionalTagCount();
//         True(count == 7, "not match. count:" + count);
//     }


//     [MTest]
//     public IEnumerator ParseCustomTagRecursive()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/ParserTest/UUebTags'>
// <customtag><customtagpos><customtagtext>
//     something<customtag><customtagpos><customtagtext>else</customtagtext></customtagpos></customtag>
// </customtagtext></customtagpos></customtag>
//         ";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         // loader contains 4 additional custom tags.
//         var count = loader.GetAdditionalTagCount();
//         True(count == 4, "not match. count:" + count);
//     }


//     [MTest]
//     public IEnumerator ParseImageAsImgContent()
//     {
//         var sampleHtml = @"
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' />";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         True(parsedRoot.GetChildren().Count == 1, "not match.");
//         True(parsedRoot.GetChildren()[0].tagValue == (int)HTMLTag.img, "not match 1. actual:" + parsedRoot.GetChildren()[0].tagValue);
//         True(parsedRoot.GetChildren()[0].treeType == TreeType.Content_Img, "not match.");
//     }

//     [MTest]
//     public IEnumerator ParseCustomImgAsImgContent()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/ParserTestImgView/UUebTags'>
// <myimg src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' />";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         True(parsedRoot.GetChildren().Count == 1, "not match.");
//         True(parsedRoot.GetChildren()[0].treeType == TreeType.Content_Img, "not match. expected:" + TreeType.Content_Img + " actual:" + parsedRoot.GetChildren()[0].treeType);
//     }

//     [MTest]
//     public IEnumerator ParseCustomTextAsTextContent()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/ParserTestTextView/UUebTags'>
// <mytext>text</mytext>";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         True(parsedRoot.GetChildren().Count == 1, "not match.");
//         True(parsedRoot.GetChildren()[0].treeType == TreeType.Container, "not match. expected:" + TreeType.Container + " actual:" + parsedRoot.GetChildren()[0].treeType);
//     }

//     [MTest]
//     public IEnumerator ParserTestCustomLayerAndCustomContentCombination()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/ParserTestCombination/UUebTags'>
// <customtag><customtagtext><customtext>text</customtext></customtagtext></customtag>
// <customtext>text</customtext>";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         True(parsedRoot.GetChildren().Count == 2, "not match.");
//         True(parsedRoot.GetChildren()[0].treeType == TreeType.CustomLayer, "not match. expected:" + TreeType.CustomLayer + " actual:" + parsedRoot.GetChildren()[0].treeType);
//         True(parsedRoot.GetChildren()[1].treeType == TreeType.Container, "not match. expected:" + TreeType.Container + " actual:" + parsedRoot.GetChildren()[0].treeType);
//     }

//     [MTest]
//     public IEnumerator Revert()
//     {
//         var sampleHtml = @"
// <body>something</body>
//         ";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         {
//             var bodyContainer = parsedRoot.GetChildren()[0];

//             var textChildren = bodyContainer.GetChildren();

//             True(textChildren.Count == 1, "not match a. actual:" + textChildren.Count);

//             var textChildrenTree = textChildren[0];
//             var textPart = textChildrenTree.keyValueStore[HTMLAttribute._CONTENT] as string;
//             var frontHalf = textPart.Substring(0, 4);
//             var backHalf = textPart.Substring(4);

//             textChildrenTree.keyValueStore[HTMLAttribute._CONTENT] = frontHalf;

//             var insertionTree = new InsertedTree(textChildrenTree, backHalf, textChildrenTree.tagValue);
//             insertionTree.SetParent(bodyContainer);

//             // 増えてるはず
//             True(bodyContainer.GetChildren().Count == 2, "not match b. actual:" + bodyContainer.GetChildren().Count);
//         }

//         TagTree.CorrectTrees(parsedRoot);

//         {
//             var bodyContainer = parsedRoot.GetChildren()[0];

//             var textChildren = bodyContainer.GetChildren();
//             var textChildrenTree = textChildren[0];

//             True(textChildren.Count == 1, "not match c. actual:" + textChildren.Count);
//             True(textChildrenTree.keyValueStore[HTMLAttribute._CONTENT] as string == "something", "actual:" + textChildrenTree.keyValueStore[HTMLAttribute._CONTENT] as string);
//         }
//     }

//     [MTest]
//     public IEnumerator ParseDefaultTag()
//     {
//         var sampleHtml = @"
// <body>something</body>
//         ";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         // parsedRootを与えて、custimizedRootを返してくる
//         // treeの内容が変わらないはず
//         var contentsCount = CountContentsRecursive(parsedRoot);
//         True(contentsCount == 3/*root + body + content*/, "not match. contentsCount:" + contentsCount);

//         var newContentsCount = CountContentsRecursive(parsedRoot);
//         True(newContentsCount == 3, "not match. newContentsCount:" + newContentsCount);
//     }

//     [MTest]
//     public IEnumerator WithCustomTag()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/WithCustomTag/UUebTags'>
// <customtag><custompos><customtext>something</customtext></custompos></customtag>
// <p>else</p>
//         ";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         var contentsCount = CountContentsRecursive(parsedRoot);
//         True(contentsCount == 8, "not match. contentsCount:" + contentsCount);
//     }

//     [MTest]
//     public IEnumerator WithWrongCustomTag()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/WithCustomTag/UUebTags'>
// <customtag><typotagpos><customtext>something</customtext></typotagpos></customtag>
// <p>else</p>
//         ";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         True(parsedRoot.errors.Any(), "no error.");
//     }

//     [MTest]
//     public IEnumerator WithDeepCustomTag()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/WithDeepCustomTag/UUebTags'>
// <customtag><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' /><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' /></customtag>
// <p>else</p>
//         ";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         var contentsCount = CountContentsRecursive(parsedRoot);

//         // 増えてる階層に関してのチェックを行う。1種のcustomTagがあるので1つ増える。
//         True(contentsCount == 7, "not match. contentsCount:" + contentsCount);

//         // ShowRecursive(customizedTree, loader);
//     }

//     [MTest]
//     public IEnumerator WithDeepCustomTagBoxHasBoxAttr()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/WithDeepCustomTag/UUebTags'>
// <customtag><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' /><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' /></customtag>
// <p>else</p>
//         ";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         foreach (var s in parsedRoot.GetChildren()[0].GetChildren())
//         {
//             True(s.treeType == TreeType.CustomBox, "not match, s.treeType:" + s.treeType);
//             True(s.keyValueStore.ContainsKey(HTMLAttribute._BOX), "box does not have pos kv.");
//         }

//         // ShowRecursive(customizedTree, loader);
//     }

//     [MTest]
//     public IEnumerator Order()
//     {
//         var sampleHtml = @"
// <body>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></body>";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         True(
//             parsedRoot/*root*/.GetChildren()[0]/*body*/.GetChildren()[0]/*text of body*/.treeType == TreeType.Content_Text, "not match, type:" + parsedRoot/*root*/.GetChildren()[0]/*body*/.GetChildren()[0]/*text of body*/.treeType
//         );

//         True(
//             parsedRoot/*root*/.GetChildren()[0]/*body*/.GetChildren()[1]/*img*/.treeType == TreeType.Content_Img, "not match, type:" + parsedRoot/*root*/.GetChildren()[0]/*body*/.GetChildren()[1]/*img*/.treeType
//         );

//     }

//     [MTest]
//     public IEnumerator ParseErrorAtDirectContentUnderLayer()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/MultipleBoxConstraints/UUebTags'>
// <itemlayout>
// <topleft>
//     <img src='https://dummyimage.com/100.png/09f/fff'/>
// </topleft>
// <topright>
//     <img src='https://dummyimage.com/100.png/08f/fff'/>
// </topright>
// <content>something! should not be direct.</content>
// <bottom>
//     <img src='https://dummyimage.com/100.png/07f/fff'/>
// </bottom>
// </itemlayout>";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });

//         // parse failed by ErrorAtDirectContentUnderLayer. returns empty tree.
//         True(parsedRoot.errors[0].code == (int)ParseErrors.CANNOT_CONTAIN_TEXT_IN_BOX_DIRECTLY, "not match.");
//     }

//     [MTest]
//     public IEnumerator BrSupport()
//     {
//         var sampleHtml = @"
// <p>
//     something<br>
//     else
// </p>";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });
//         var p = parsedRoot.GetChildren()[0].GetChildren();
//         // foreach (var pp in p) {
//         //     Debug.LogError("pp:" + pp.tagValue);
//         // }
//         True(p.Count == 3, "not match, count:" + p.Count);
//     }

//     [MTest]
//     public IEnumerator PSupport()
//     {
//         var sampleHtml = @"
// <p>
//     p1<a href=''>a</a>p2
// </p>";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });
//         var pChildren = parsedRoot.GetChildren()[0].GetChildren();
//         // foreach (var pp in pChildren) {
//         //     Debug.LogError("pp:" + pp.tagValue);
//         // }
//         True(pChildren.Count == 3, "not match, count:" + pChildren.Count);
//     }

//     [MTest]
//     public IEnumerator CoronWrappedContentSupport()
//     {
//         var sampleHtml = @"
// <p>
//     a'<a href=''>aqua color string</a>'b
// </p>";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });
//         var pChildren = parsedRoot.GetChildren()[0].GetChildren();
//         foreach (var pp in pChildren)
//         {
//             True(pp.keyValueStore[HTMLAttribute._CONTENT] as string == "a'<a href=''>aqua color string</a>'b", "not match, " + pp.keyValueStore[HTMLAttribute._CONTENT]);
//         }
//         True(pChildren.Count == 1, "not match, pChildren count:" + pChildren.Count);
//     }

//     [MTest]
//     public IEnumerator UnityRichTextColorSupport()
//     {
//         var sampleHtml = @"
// <p>
//     a<color=#00ffffff>aqua color string</color>b
// </p>";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });
//         var pChildren = parsedRoot.GetChildren()[0].GetChildren();
//         foreach (var pp in pChildren)
//         {
//             // Debug.LogError("pp:" + pp.tagValue + " text:" + pp.keyValueStore[HTMLAttribute._CONTENT]);
//             True(pp.keyValueStore[HTMLAttribute._CONTENT] as string == "a<color=#00ffffff>aqua color string</color>b", "not match, " + pp.keyValueStore[HTMLAttribute._CONTENT]);
//         }
//         True(pChildren.Count == 1, "not match, pChildren count:" + pChildren.Count);
//     }

//     [MTest]
//     public IEnumerator UnityRichTextSizeSupport()
//     {
//         var sampleHtml = @"
// <p>
//     a<size=50>large string</size>b
// </p>";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });
//         var pChildren = parsedRoot.GetChildren()[0].GetChildren();
//         foreach (var pp in pChildren)
//         {
//             // Debug.LogError("pp:" + pp.tagValue + " text:" + pp.keyValueStore[HTMLAttribute._CONTENT]);
//             True(pp.keyValueStore[HTMLAttribute._CONTENT] as string == "a<size=50>large string</size>b", "not match, " + pp.keyValueStore[HTMLAttribute._CONTENT]);
//         }
//         True(pChildren.Count == 1, "not match, pChildren count:" + pChildren.Count);
//     }

//     [MTest]
//     public IEnumerator CustomEmptyLayerCanSingleCloseTag()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/MyInfoView/UUebTags'>
// <newbadge/>";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });
//         True(parsedRoot.errors.Count == 0, "not match. error:" + ParsedTree.ShowErrors(parsedRoot));
//         foreach (var child in parsedRoot.GetChildren())
//         {
//             if (child.tagValue == 28)
//             {
//                 Debug.LogError("child text:" + child.keyValueStore[HTMLAttribute._CONTENT]);
//             }
//         }
//         True(parsedRoot.GetChildren().Count == 1, "count:" + parsedRoot.GetChildren().Count);
//     }

//     [MTest]
//     public IEnumerator CustomEmptyLayerCanSingleCloseTag2()
//     {
//         var sampleHtml = @"
// <!DOCTYPE uuebview href='resources://Views/MyInfoView/UUebTags'>
// <body><newbadge/>aaa</body>";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });
//         True(parsedRoot.errors.Count == 0, "not match.");
//         True(parsedRoot.GetChildren().Count == 1, "count:" + parsedRoot.GetChildren().Count);
//         True(parsedRoot.GetChildren()[0].GetChildren().Count == 2, "count:" + parsedRoot.GetChildren()[0].GetChildren().Count);
//     }


//     [MTest]
//     public IEnumerator AlignSupport()
//     {
//         var sampleHtml = @"
// <body><p align='center'>aaa</p></body>";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });
//     }

//     [MTest]
//     public IEnumerator SinglePSupport()
//     {
//         var sampleHtml = @"
// <body>bbb<p>aaa</body>";
//         ParsedTree parsedRoot = null;
//         yield return GetParsedRoot(sampleHtml, parsedRootSource => { parsedRoot = parsedRootSource; });
//     }

// }