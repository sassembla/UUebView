using Miyamasu;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using UUebView;

/**
	test for layoutMachine.
 */
public class LayoutMachineTests : MiyamasuTestRunner
{
    private HTMLParser parser;

    private ResourceLoader loader;

    private UUebView.UUebViewComponent executor;

    private void ShowLayoutRecursive(TagTree tree)
    {
        Debug.Log("tree:" + loader.GetTagFromValue(tree.tagValue) + " offsetX:" + tree.offsetX + " offsetY:" + tree.offsetY + " width:" + tree.viewWidth + " height:" + tree.viewHeight);
        foreach (var child in tree.GetChildren())
        {
            ShowLayoutRecursive(child);
        }
    }

    [MSetup]
    public void Setup()
    {

        executor = new GameObject("layoutMachineTest").AddComponent<UUebViewComponent>();
        var core = new UUebView.UUebViewCore(executor);
        executor.SetCore(core);

        loader = new ResourceLoader(executor.Core.CoroutineExecutor);

        parser = new HTMLParser(loader);
    }

    [MTeardown]
    public void Teardown()
    {
        GameObject.DestroyImmediate(executor);
        GameObject.DestroyImmediate(loader.cacheBox);
    }

    private IEnumerator CreateTagTree(string sampleHtml, Action<TagTree> onParsed, float width = 100)
    {
        ParsedTree parsedRoot = null;
        TagTree layoutedRoot = null;

        var cor = parser.ParseRoot(
            sampleHtml,
            parsed =>
            {
                parsedRoot = parsed;
            }
        );

        executor.Core.CoroutineExecutor(cor);

        yield return WaitUntil(
            () => parsedRoot != null, () => { throw new TimeoutException("too late."); }, 1
        );

        if (parsedRoot.errors.Any())
        {
            throw new Exception("failed to parse. error:" + parsedRoot.errors[0].reason);
        }


        var layoutMachine = new LayoutMachine(
            loader
        );

        var loaderCor = layoutMachine.Layout(
            parsedRoot,
            new Vector2(width, 100),
            layoutedTree =>
            {
                layoutedRoot = layoutedTree;
            }
        );

        executor.Core.CoroutineExecutor(loaderCor);

        yield return WaitUntil(
            () => layoutedRoot != null, () => { throw new TimeoutException("too late."); }, 5
        );

        onParsed(layoutedRoot);
    }

    [MTest]
    public IEnumerator LayoutHTML()
    {
        var sample = @"
<body>something</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
    }

    [MTest]
    public IEnumerator LayoutHTMLHasValidView()
    {
        var sample = @"
<body>something</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 16, "not match.");
    }

    [MTest]
    public IEnumerator LayoutHTMLWithSmallTextHasValidView()
    {
        var sample = @"
<body>over 100px string should be multi lined text with good separation. need some length.</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 118, "not match. tree.viewHeight:" + tree.viewHeight);
    }

    [MTest]
    public IEnumerator LayoutHTMLWithImage()
    {
        var sample = @"
<body><img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 100, "not match.");
    }

    [MTest]
    public IEnumerator LayoutHTMLWithSmallImage()
    {
        var sample = @"
<body><img src='https://dummyimage.com/10.png/09f/fff'/></body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 10, "not match.");
    }

    [MTest]
    public IEnumerator LayoutHTMLWithSmallImageAndText()
    {
        var sample = @"
<body><img src='https://dummyimage.com/10.png/09f/fff'/>text</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 16, "not match.");
    }

    [MTest]
    public IEnumerator LayoutHTMLWithSmallImageAndSmallText()
    {
        var sample = @"
<body><img src='https://dummyimage.com/10.png/09f/fff'/>over 100px string should be multi lined text with good separation. need some length.</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 117, "not match. tree.viewHeight:" + tree.viewHeight);
    }


    [MTest]
    public IEnumerator LayoutHTMLWithWideImageAndText()
    {
        var sample = @"
<body><img src='https://dummyimage.com/97x10/000/fff'/>something</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 26, "not match. tree.viewHeight:" + tree.viewHeight);
    }

    [MTest]
    public IEnumerator LayoutHTMLWithTextAndWideImage()
    {
        var sample = @"
<body>something<img src='https://dummyimage.com/100x10/000/fff'/></body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 16, "not match.");
    }


    [MTest]
    public IEnumerator LayoutHTMLWithTextAndWideImageAndText()
    {
        var sample = @"
<body>something<img src='https://dummyimage.com/100x10/000/fff'/>else</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 16 + 16, "not match.");
    }

    [MTest]
    public IEnumerator LayoutHTMLWithTextAndWideImageAndTextAndWideImageAndText()
    {
        var sample = @"
<body>
something
<img src='https://dummyimage.com/100x10/000/fff'/>
else
<img src='https://dummyimage.com/100x20/000/fff'/>
other
</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 16 + 16 + 16, "not match. tree.viewHeight:" + tree.viewHeight);
    }

    [MTest]
    public IEnumerator LayoutHTMLWithWideImageAndTextAndWideImageAndText()
    {
        var sample = @"
<body><img src='https://dummyimage.com/100x10/000/fff'/>else<img src='https://dummyimage.com/100x20/000/fff'/>other</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 10 + 16 + 16, "not match.");
    }


    [MTest]
    public IEnumerator LayoutHTMLWithTextAndSmallImage()
    {
        var sample = @"
<body>something<img src='https://dummyimage.com/10x10/000/fff'/></body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 16, "not match.");
    }


    [MTest]
    public IEnumerator LayoutHTMLWithTextAndSmallImageAndText()
    {
        var sample = @"
<body>something<img src='https://dummyimage.com/10x10/000/fff'/>b!</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 16, "not match.");
    }

    [MTest]
    public IEnumerator LayoutHTMLWithTextAndSmallImageAndTextAndWideImageAndText()
    {
        var sample = @"
<body>something<img src='https://dummyimage.com/10x10/000/fff'/>else<img src='https://dummyimage.com/100x10/000/fff'/>other</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 16 + 16 + 16, "not match.");
    }

    [MTest]
    public IEnumerator LayoutHTMLWithSmallImageAndTextAndSmallImageAndText()
    {
        var sample = @"
<body><img src='https://dummyimage.com/10x10/000/fff'/>else<img src='https://dummyimage.com/10x20/000/fff'/>other</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(tree.viewHeight == 20, "not match.");
    }


    [MTest]
    public IEnumerator LoadHTMLWithCustomTagLink()
    {
        var sample = @"
<!DOCTYPE uuebview href='resources://Views/LayoutHTMLWithCustomTag/UUebTags'>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
    }

    [MTest]
    public IEnumerator LayoutHTMLWithCustomTag()
    {
        var sample = @"
<!DOCTYPE uuebview href='resources://Views/LayoutHTMLWithCustomTag/UUebTags'>
<body>
<customtag><custombg><textbg><customtext>something</customtext></textbg></custombg></customtag>
else
<customimg src='https://dummyimage.com/10x20/000/fff'/>
</body>
        ";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
    }

    [MTest]
    public IEnumerator LayoutHTMLWithCustomTagSmallText()
    {
        var sample = @"
<!DOCTYPE uuebview href='resources://Views/LayoutHTMLWithCustomTag/UUebTags'>
<body>
<customtag><custombg><textbg><customtext>
something you need is not time, money, but do things fast.
</customtext></textbg></custombg></customtag>
else
</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
    }

    [MTest]
    public IEnumerator LayoutHTMLWithCustomTagLargeText()
    {
        var sample = @"
<!DOCTYPE uuebview href='resources://Views/LayoutHTMLWithCustomTag/UUebTags'>
<body>
<customtag><custombg><textbg><customtext>
Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
</customtext></textbg></custombg></customtag>
else
</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        while (true)
        {
            if (0 < tree.GetChildren().Count)
            {
                tree = tree.GetChildren()[tree.GetChildren().Count - 1];
                if (tree.offsetY != 0)
                {
                    True(tree.offsetY.ToString() == "799.95", "not match, offsetY:" + tree.offsetY);
                }
            }
            else
            {
                break;
            }
        }
    }

    [MTest]
    public IEnumerator RevertLayoutHTMLWithSmallImageAndSmallText()
    {
        var sample = @"
<body><img src='https://dummyimage.com/10.png/09f/fff'/>over 100px string should be multi lined text with good separation. need some length.</body>";

        ParsedTree parsedRoot = null;
        {
            var cor = parser.ParseRoot(
                sample,
                parsed =>
                {
                    parsedRoot = parsed;
                }
            );

            executor.Core.CoroutineExecutor(cor);

            yield return WaitUntil(
                () => parsedRoot != null, () => { throw new TimeoutException("too late."); }, 1
            );
        }

        {
            var done = false;

            LayoutMachine layoutMachine = null;

            layoutMachine = new LayoutMachine(
                loader
            );

            var cor = layoutMachine.Layout(
                parsedRoot,
                new Vector2(100, 100),
                layoutedTree =>
                {
                    done = true;
                    True(layoutedTree.viewHeight == 117, "not match. layoutedTree.viewHeight:" + layoutedTree.viewHeight);
                }
            );
            executor.Core.CoroutineExecutor(cor);


            yield return WaitUntil(
                () => done, () => { throw new TimeoutException("too late."); }, 5.0001
            );

            TagTree.CorrectTrees(parsedRoot);

            /*
                revert-layout.
            */
            var done2 = false;

            var cor2 = layoutMachine.Layout(
                parsedRoot,
                new Vector2(100, 100),
                layoutedTree =>
                {
                    done2 = true;
                    True(layoutedTree.viewHeight == 117, "not match. actual:" + layoutedTree.viewHeight);
                }
            );

            executor.Core.CoroutineExecutor(cor2);


            yield return WaitUntil(
                () => done2, () => { throw new TimeoutException("too late."); }, 5
            );
        }
    }

    [MTest]
    public IEnumerator RevertLayoutHTMLWithSmallImageAndSmallTextAndBr()
    {
        var sample = @"
<body><img src='https://dummyimage.com/10.png/09f/fff'/>over 100px string should be multi lined text with good separation.
<br>need some length.</body>";

        ParsedTree parsedRoot = null;
        {
            var cor = parser.ParseRoot(
                sample,
                parsed =>
                {
                    parsedRoot = parsed;
                }
            );

            executor.Core.CoroutineExecutor(cor);

            yield return WaitUntil(
                () => parsedRoot != null, () => { throw new TimeoutException("too late."); }, 1
            );
        }

        {
            var done = false;

            LayoutMachine layoutMachine = null;

            layoutMachine = new LayoutMachine(
                loader
            );

            var cor = layoutMachine.Layout(
                parsedRoot,
                new Vector2(100, 100),
                layoutedTree =>
                {
                    done = true;
                    True(layoutedTree.viewHeight == 116, "not match. layoutedTree.viewHeight:" + layoutedTree.viewHeight);
                }
            );
            executor.Core.CoroutineExecutor(cor);


            yield return WaitUntil(
                () => done, () => { throw new TimeoutException("too late."); }, 5
            );

            TagTree.CorrectTrees(parsedRoot);

            /*
                revert-layout.
            */
            var done2 = false;

            var cor2 = layoutMachine.Layout(
                parsedRoot,
                new Vector2(100, 100),
                layoutedTree =>
                {
                    done2 = true;
                    True(layoutedTree.viewHeight == 116, "not match. actual:" + layoutedTree.viewHeight);
                }
            );

            executor.Core.CoroutineExecutor(cor2);


            yield return WaitUntil(
                () => done2, () => { throw new TimeoutException("too late."); }, 5
            );
        }
    }

    [MTest]
    public IEnumerator Order()
    {
        var sample = @"
<body>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(
            tree/*root*/.GetChildren()[0]/*body*/.GetChildren()[0]/*text of body*/.treeType == TreeType.Content_Text, "not match, type:" + tree/*root*/.GetChildren()[0]/*body*/.GetChildren()[0]/*text of body*/.treeType
        );

        True(
            tree/*root*/.GetChildren()[0]/*body*/.GetChildren()[1]/*img*/.treeType == TreeType.Content_Img, "not match, type:" + tree/*root*/.GetChildren()[0]/*body*/.GetChildren()[1]/*img*/.treeType
        );
    }

    [MTest]
    public IEnumerator Position()
    {
        var sample = @"
<body>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(
            tree/*root*/.GetChildren()[0]/*body*/.GetChildren()[0]/*text of body*/.offsetY == 6f, "not match, offsetY:" + tree/*root*/.GetChildren()[0]/*body*/.GetChildren()[0]/*text of body*/.offsetY
        );

        True(
            tree/*root*/.GetChildren()[0]/*body*/.GetChildren()[1]/*img*/.offsetY == 0, "not match, offsetY:" + tree/*root*/.GetChildren()[0]/*body*/.GetChildren()[1]/*img*/.offsetY
        );
    }

    [MTest]
    public IEnumerator MultipleBoxConstraints()
    {
        var sample = @"
<!DOCTYPE uuebview href='resources://Views/MultipleBoxConstraints/UUebTags'>
<itemlayout>
<topleft>
    <img src='https://dummyimage.com/100.png/09f/fff'/>
</topleft>
<topright>
    <img src='https://dummyimage.com/100.png/08f/fff'/>
</topright>
<content><p>something!</p></content>
<bottom>
    <img src='https://dummyimage.com/100.png/07f/fff'/>
</bottom>
</itemlayout>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        var itemLayout = tree.GetChildren()[0];
        var topleft = itemLayout.GetChildren()[0];
        var topright = itemLayout.GetChildren()[1];
        True(topleft.offsetY == 0, "not match, topleft.offsetY:" + topleft.offsetY);
        True(topright.offsetY == 0, "not match, topright.offsetY:" + topright.offsetY);
    }


    [MTest]
    public IEnumerator LayoutHTMLWithCustomTagMultiple()
    {
        var sample = @"
<!DOCTYPE uuebview href='resources://Views/LayoutHTMLWithCustomTag/UUebTags'>
<customtag><custombg><textbg><customtext>something1</customtext></textbg></custombg></customtag>
<customtag><custombg><textbg><customtext>something2</customtext></textbg></custombg></customtag>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(0 < tree.GetChildren().Count, "not match, actual:" + tree.GetChildren().Count);
        True(tree.GetChildren()[0].offsetY.ToString() == "0.04999924", "not match of 1. actual:" + tree.GetChildren()[0].offsetY);
        True(tree.GetChildren()[1].offsetY == 60.8f, "not match of 2. actual:" + tree.GetChildren()[1].offsetY);
    }

    [MTest]
    public IEnumerator LayoutHTMLWithCustomTagMultipleInBody()
    {
        var sample = @"
<!DOCTYPE uuebview href='resources://Views/LayoutHTMLWithCustomTag/UUebTags'>
<body>
<customtag><custombg><textbg><customtext>something1</customtext></textbg></custombg></customtag>
<customtag><custombg><textbg><customtext>something2</customtext></textbg></custombg></customtag>
</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        True(0 < tree.GetChildren().Count, "not match, actual:" + tree.GetChildren().Count);
        True(tree.GetChildren()[0].GetChildren()[0].offsetY.ToString() == "0.04999924", "not match of 1. actual:" + tree.GetChildren()[0].GetChildren()[0].offsetY);
        True(tree.GetChildren()[0].GetChildren()[1].offsetY == 60.8f, "not match of 2. actual:" + tree.GetChildren()[0].GetChildren()[1].offsetY);
    }

    [MTest]
    public IEnumerator LayoutSampleView2_HiddenBreakView()
    {
        var sample = @"
<!DOCTYPE uuebview href='resources://Views/MyInfoView/UUebTags'>
<body>
    <bg>
    	<textbg>
    		<textbox>
	    		<updatetext>koko ni nihongo ga iikanji ni hairu. good thing. long text will make large window. like this.</updatetext>
	    		<updatetext hidden='true' listen='readmore'>omake!</updatetext>
	    	</textbox>
	    </textbg>
    </bg>
</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        var textBox = tree.GetChildren()[0].GetChildren()[0].GetChildren()[0].GetChildren()[0].GetChildren()[0].GetChildren()[0];
        var updatetextBox = textBox.GetChildren()[0];
        // Debug.LogError("updatetextBox:" + updatetextBox.viewHeight);
        True(textBox.viewHeight == 2023, "not match, textBox.viewHeight:" + textBox.viewHeight);
    }

    [MTest]
    public IEnumerator SampleView2()
    {
        var sample = @"
<!DOCTYPE uuebview href='resources://Views/MyInfoView/UUebTags'>
<body>
    <bg>
    	<titlebox>
    		<titletext>レモン一個ぶんのビタミンC</titletext>
    	</titlebox>
    	<newbadge></newbadge>
    	<textbg>
    		<textbox>
	    		<updatetext>1st line.</updatetext>
	    	</textbox>
            <textbox>
	    		<updatetext>2nd line.</updatetext>
	    	</textbox>
	    </textbg>
    </bg>
</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
    }

    [MTest]
    public IEnumerator LayoutHTMLWithCustomTagMultipleByInnerContentWithParentLayer()
    {
        var sample = @"
<!DOCTYPE uuebview href='resources://Views/LayoutHTMLWithCustomTag/UUebTags'>
<customtag>
    <custombg><textbg><customtext>something1</customtext></textbg></custombg>
    <custombg><textbg><customtext>something2</customtext></textbg></custombg>
</customtag>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        var custombgs = tree.GetChildren()[0]/*customtag*/.GetChildren()[0]/*box*/.GetChildren();
        True(custombgs[0].offsetY == 0, "not match. custombgs[0].offsetY:" + custombgs[0].offsetY);
        True(custombgs[1].offsetY == 60.7f, "not match. custombgs[1].offsetY:" + custombgs[1].offsetY);
    }

    [MTest]
    public IEnumerator LayoutHTMLWithDoubleBoxedLayerNeverOverLayout()
    {
        var sample = @"
<!DOCTYPE uuebview href='resources://Views/MyInfoView/UUebTags'>
<body>
    <bg>
    	<textbg>
    		<textbox>
	    		<p>koko ni nihongo ga iikanji ni hairu. <br> 2line content! 2line content! 2line content!2 line content! a good thing.<a href='somewhere'>link</a>a long text will make large window. something like this.</p>
	    		<updatetext>omake! abc d</updatetext>
                <p>ef ghijklm</p>
                <updatetext>aaaaaaaaaaaaabcdefghijk</updatetext>
	    	</textbox>
	    </textbg>
    </bg>
</body>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; }, 300);

        var pAndUpdateText = tree.GetChildren()[0]/*body*/.GetChildren()[0]/*bg*/.GetChildren()[0]/*textbg*/.GetChildren()[0]/*textbox*/.GetChildren()[0]/*textbox_box*/.GetChildren()[0].GetChildren();
        // foreach (var s in pAndUpdateText) {
        //     Debug.LogError("s:" + loader.GetTagFromValue(s.tagValue) + " treeType:" + s.treeType);
        // }

        var pContainer = pAndUpdateText[0];
        True(pContainer.viewWidth.ToString() == "208.9", "not match. pContainer.viewWidth:" + pContainer.viewWidth);

        var lastPContents = pContainer.GetChildren().Last();
        True(lastPContents.offsetY.ToString() == "109", "not match. lastPContents.offsetY:" + lastPContents.offsetY);

        var updateTextContainer = pAndUpdateText[1];
        True(updateTextContainer.offsetX == 51f, "not match. updateTextContainer.offsetX:" + updateTextContainer.offsetX);
        True(updateTextContainer.offsetY == 100f, "not match. updateTextContainer.offsetY:" + updateTextContainer.offsetY);

        var updateTextContainer2 = pAndUpdateText[2];
        True(updateTextContainer2.offsetY == 100f, "not match. updateTextContainer2.offsetY:" + updateTextContainer2.offsetY);

        var pContainer2 = pAndUpdateText[3];
        True(pContainer2.offsetY == 125, "not match. pContainer2.offsetY:" + pContainer2.offsetY);

        var pContainer2FirstLine = pContainer2.GetChildren()[0];
        True(pContainer2FirstLine.offsetX == 44f, "not match. pContainer2FirstLine.offsetX:" + pContainer2FirstLine.offsetX);
        True(pContainer2FirstLine.offsetY == 0f, "not match. pContainer2FirstLine.offsetY:" + pContainer2FirstLine.offsetY);

        var pContainer2SecondLine = pContainer2.GetChildren()[1];
        True(pContainer2SecondLine.offsetX == 0f, "not match. pContainer2SecondLine.offsetX:" + pContainer2SecondLine.offsetX);
        True(pContainer2SecondLine.offsetY.ToString() == "25", "not match. pContainer2SecondLine.offsetY:" + pContainer2SecondLine.offsetY);
    }

    [MTest]
    public IEnumerator LayoutGroupHeightChanged()
    {
        var sample = @"
<!DOCTYPE uuebview href='resources://Views/MyInfoView/UUebTags'>
<body>
	<bg>
		<titlebox>
			<titletext>Sample</titletext>
		</titlebox>
		<newbadge/>
		<newbadge/>
        <textbg>
        </textbg>
    </bg>
</body>
";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; }, 300);
        var textBg = tree.GetChildren()[0].GetChildren()[0].GetChildren()[2];
        True(textBg.offsetY.ToString() == "56.97501", "not match, textBg.offsetY:" + textBg.offsetY);
    }

    [MTest]
    public IEnumerator LayoutAfterLayer()
    {
        var sample = @"
<!DOCTYPE uuebview href='resources://Views/MyInfoView/UUebTags'>
<body>
	<bg>
    </bg>
    <p>hey!</p>
</body>
";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; }, 300);
        var p = tree.GetChildren()[0].GetChildren()[1];

        True(p.offsetX == 0 && p.offsetY == 100, "not match. p.offsetY:" + p.offsetY);
    }

    [MTest]
    public IEnumerator BrSupport()
    {
        var sample = @"
<p>
    something<br>
    else
</p>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        var p = tree.GetChildren()[0]/*p*/.GetChildren();

        True(p[0].offsetY == 0, "not match. custombgs[0].offsetY:" + p[0].offsetY);
        True(p[2].offsetY == 16f, "not match. custombgs[2].offsetY:" + p[2].offsetY);
    }

    [MTest]
    public IEnumerator BrBrSupport()
    {
        var sample = @"
<p>
    something<br><br>
    else
</p>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        var p = tree.GetChildren()[0]/*p*/.GetChildren();

        True(p[0].offsetY == 0, "not match. custombgs[0].offsetY:" + p[0].offsetY);
        True(p[3].offsetY == 32f, "not match. custombgs[3].offsetY:" + p[3].offsetY);
    }

    [MTest]
    public IEnumerator LayoutCenterAlignSupport()
    {
        var sample = @"
<p align='center'>aaa</p>";

        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        var p = tree.GetChildren()[0]/*p*/.GetChildren();

        True(p[0].offsetX == 38, "not match. custombgs[0].offsetX:" + p[0].offsetX);
        True(p[0].offsetY == 0, "not match. custombgs[0].offsetY:" + p[0].offsetY);
    }

    [MTest]
    public IEnumerator LayoutRightAlignSupport()
    {
        var sample = @"
<p align='right'>aaa</p>";

        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        var p = tree.GetChildren()[0]/*p*/.GetChildren();

        True(p[0].offsetX == 76, "not match. custombgs[0].offsetX:" + p[0].offsetX);
        True(p[0].offsetY == 0, "not match. custombgs[0].offsetY:" + p[0].offsetY);
    }

    [MTest]
    public IEnumerator PSupport()
    {
        var sample = @"
<p>
    p1<a href=''>a1</a>p2
</p>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        var p1 = tree.GetChildren()[0];

        True(p1.GetChildren().Count == 3, "not match, p1.GetChildren().Count:" + p1.GetChildren().Count);
    }

    [MTest]
    public IEnumerator PSupport2()
    {
        Debug.LogWarning("保留。");
        yield break;
        var sample = @"
<p>
    p1<a href=''>a1</a>p2
</p><p>
    p3
</p>";
        TagTree tree = null; yield return CreateTagTree(sample, tagTreeSource => { tree = tagTreeSource; });
        var p1 = tree.GetChildren()[0];
        var p2 = tree.GetChildren()[1];

        True(p1.GetChildren().Count == 3, "not match, p1.GetChildren().Count:" + p1.GetChildren().Count);

        True(p1.offsetY == 0, "not match. p1.offsetY:" + p1.offsetY);
        True(p2.offsetY == 16f, "not match. p2.offsetY:" + p2.offsetY);
    }
}