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
	test for TMPro extension materialize.
 */
public class TMProExtensionMaterializeMachineTests : MiyamasuTestRunner
{
    private HTMLParser parser;

    private GameObject canvas;


    GameObject rootObj;
    UUebView.UUebViewComponent view;

    UUebView.UUebViewCore core;

    private void ShowLayoutRecursive(TagTree tree)
    {
        Debug.Log("tree:" + core.resLoader.GetTagFromValue(tree.tagValue) + " offsetX:" + tree.offsetX + " offsetY:" + tree.offsetY + " width:" + tree.viewWidth + " height:" + tree.viewHeight);
        foreach (var child in tree.GetChildren())
        {
            ShowLayoutRecursive(child);
        }
    }


    [MSetup]
    public void Setup()
    {
        rootObj = new GameObject();
        var rectTrans = rootObj.AddComponent<RectTransform>();
        rectTrans.anchorMin = new Vector2(0, 1);
        rectTrans.anchorMax = new Vector2(0, 1);
        rectTrans.pivot = new Vector2(0, 1);

        view = rootObj.AddComponent<UUebViewComponent>();
        core = new UUebView.UUebViewCore(view, new TMProPlugin());

        view.SetCore(core);

        var canvas = GameObject.Find("Canvas/TMProExtensionMaterializeMachineTestPlace");
        if (canvas == null)
        {
            var prefab = Resources.Load<GameObject>("TestPrefabs/Canvas");
            var canvasBase = GameObject.Instantiate(prefab);
            canvasBase.name = "Canvas";
            canvas = GameObject.Find("Canvas/MaterializeTestPlace");
        }


        rootObj.transform.SetParent(canvas.transform, false);

        rectTrans.anchoredPosition = new Vector2(100 * index, 0);
        index++;

        parser = new HTMLParser(core.resLoader, core.defaultImageDownloader);
    }

    private IEnumerator CreateLayoutedTree(string sampleHtml, Action<TagTree> onLayouted, float width = 100)
    {
        ParsedTree parsedRoot = null;
        var cor = parser.ParseRoot(
            sampleHtml,
            parsed =>
            {
                parsedRoot = parsed;
            }
        );

        view.Core.Internal_CoroutineExecutor(cor);

        yield return WaitUntil(
            () => parsedRoot != null, () => { throw new TimeoutException("too late."); }, 1
        );

        if (parsedRoot.errors.Any())
        {
            foreach (var error in parsedRoot.errors)
            {
                Debug.LogError("error:" + error.code + " reason:" + error.reason);
            }
            throw new Exception("failed to create layouted tree.");
        }

        TagTree layouted = null;

        var layoutMachine = new LayoutMachine(core.resLoader, new TMProPlugin());

        var cor2 = layoutMachine.Layout(
            parsedRoot,
            new Vector2(width, 100),
            layoutedTree =>
            {
                layouted = layoutedTree;
            }
        );
        view.Core.Internal_CoroutineExecutor(cor2);

        yield return WaitUntil(
            () => layouted != null, () => { throw new TimeoutException("too late."); }, 5
        );

        onLayouted(layouted);
    }

    private static int index;
    private IEnumerator Show(TagTree tree)
    {
        var materializeMachine = new MaterializeMachine(core.resLoader, new TMProPlugin());

        var materializeDone = false;

        var cor = materializeMachine.Materialize(rootObj, core, tree, new Vector2(tree.viewWidth, tree.viewHeight), 0, () =>
         {
             materializeDone = true;
         });
        view.Core.Internal_CoroutineExecutor(cor);

        yield return WaitUntil(
            () => materializeDone && !view.Core.IsLoading(),
            () => { throw new TimeoutException("slow materialize. materializeDone:" + materializeDone + " view.IsLoading():" + view.Core.IsLoading()); }
        );
    }

    [MTest]
    public IEnumerator MaterializeHTML()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>something</tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLHasValidView()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>something</tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithSmallTextHasValidView()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>over 100px string should be multi lined text with good separation. need some length.</tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithLink()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody><tma href='https://dummyimage.com/100.png/09f/fff'>link!</tma></tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithLinkWithId()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody><tma href='https://dummyimage.com/100.png/09f/fff' id='linkId'>link!</tma></tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithImage()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody><img src='https://dummyimage.com/100.png/09f/fff'/></tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithImageAsButton()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody><img src='https://dummyimage.com/100.png/09f/fff' button='true''/></tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithImageAsButtonWithId()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody><img src='https://dummyimage.com/100.png/09f/fff' button='true' id='imageId'/></tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithImageAsButtonWithIdMakeChanges()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>
    <tmp listen='imageId' hidden='true'>something</tmp>
    <img src='https://dummyimage.com/100.png/09f/fff' button='true' id='imageId'/>
    </tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithDoubleBoxedLayer()
    {
        var sample = @"
    <!DOCTYPE uuebview href='resources://Views/TMMyInfoView/UUebTags'>
    <textbox>
        <tmp>fmmm???</tmp>
        <updatetext>something.</updatetext>
        <updatetext>omake!</updatetext>
    </textbox>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithSmallImage()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody><img src='https://dummyimage.com/10.png/09f/fff'/></tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithSmallImageAndText()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody><img src='https://dummyimage.com/10.png/09f/fff'/>text</tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithSmallImageAndSmallText()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody><img src='https://dummyimage.com/10.png/09f/fff'/>over 100px string should be multi lined text with good separation. need some length.</tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithWideImageAndText()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody><img src='https://dummyimage.com/97x10/000/fff'/>something</tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithTextAndWideImage()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>something<img src='https://dummyimage.com/100x10/000/fff'/></tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }


    [MTest]
    public IEnumerator MaterializeHTMLWithTextAndWideImageAndText()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>something<img src='https://dummyimage.com/100x10/000/fff'/>else</tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithTextAndWideImageAndTextAndWideImageAndText()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>something<img src='https://dummyimage.com/100x10/000/fff'/>else<img src='https://dummyimage.com/100x20/000/fff'/>other</tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithWideImageAndTextAndWideImageAndText()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody><img src='https://dummyimage.com/100x10/000/fff'/>else<img src='https://dummyimage.com/100x20/000/fff'/>other</tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }


    [MTest]
    public IEnumerator MaterializeHTMLWithTextAndSmallImage()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>something<img src='https://dummyimage.com/10x10/000/fff'/></tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }


    [MTest]
    public IEnumerator MaterializeHTMLWithTextAndSmallImageAndText()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>something<img src='https://dummyimage.com/10x10/000/fff'/>b!</tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithTextAndSmallImageAndTextAndWideImageAndText()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>something<img src='https://dummyimage.com/10x10/000/fff'/>else<img src='https://dummyimage.com/100x10/000/fff'/>other</tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithSmallImageAndTextAndSmallImageAndText()
    {
        var sample = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody><img src='https://dummyimage.com/10x10/000/fff'/>else<img src='https://dummyimage.com/10x20/000/fff'/>other</tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }


    [MTest]
    public IEnumerator LoadHTMLWithCustomTagLink()
    {
        var sample = @"
    <!DOCTYPE uuebview href='resources://Views/TMLayoutHTMLWithCustomTag/UUebTags'>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithCustomTag()
    {
        var sample = @"
    <!DOCTYPE uuebview href='resources://Views/TMLayoutHTMLWithCustomTag/UUebTags'>
    <tmbody>
    <customtag><custombg><textbg><customtext>something</customtext></textbg></custombg></customtag>
    else

    </tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithCustomTagSmallText()
    {
        var sample = @"
    <!DOCTYPE uuebview href='resources://Views/TMLayoutHTMLWithCustomTag/UUebTags'>
    <tmbody>
    <customtag><custombg><textbg><customtext>
    something you need is not time, money, but do things fast.</customtext></textbg></custombg></customtag>
    else
    </tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithCustomTagLargeText()
    {
        var sample = @"
    <!DOCTYPE uuebview href='resources://Views/TMLayoutHTMLWithCustomTag/UUebTags'>
    <tmbody>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    else
    <customimg src='https://dummyimage.com/10x20/000/fff'/>
    </tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MultipleBoxConstraints()
    {

        var sample = @"
    <!DOCTYPE uuebview href='resources://Views/TMMultipleBoxConstraints/UUebTags'>
    <itemlayout>
    <topleft>
        <img src='https://dummyimage.com/100.png/09f/fff'/>
    </topleft>
    <topright>
        <img src='https://dummyimage.com/100.png/08f/fff'/>
    </topright>
    <content><tmp>something! need more lines for test. get wild and tough is really good song. really really good song. forever. long lives get wild and tough!</tmp></content>
    <bottom>
        <img src='https://dummyimage.com/100.png/07f/fff'/>
    </bottom>
    </itemlayout>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithCustomTagMultiple()
    {
        var sample = @"
    <!DOCTYPE uuebview href='resources://Views/TMLayoutHTMLWithCustomTag/UUebTags'>
    <tmbody>
    <customtag><custombg><textbg><customtext>something1</customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>something2</customtext></textbg></custombg></customtag>
    else
    </tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithCustomTagMultipleByInnerContent()
    {
        var sample = @"
    <!DOCTYPE uuebview href='resources://Views/TMLayoutHTMLWithCustomTag/UUebTags'>
    <tmbody>
    <customtag>
        <custombg><textbg><customtext>something1</customtext></textbg></custombg>
        <custombg><textbg><customtext>something2</customtext></textbg></custombg>
    </customtag>
    else
    </tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator LayoutHTMLWithCustomTagMultipleByInnerContentWithParentLayer()
    {
        var sample = @"
    <!DOCTYPE uuebview href='resources://Views/TMLayoutHTMLWithCustomTag/UUebTags'>
    <customtag>
        <custombg><textbg><customtext>something1</customtext></textbg></custombg>
        <custombg><textbg><customtext>something2</customtext></textbg></custombg>
    </customtag>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sample, treeSource => { tree = treeSource; });

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeHTMLWithDoubleBoxedLayerNeverOverLayout()
    {
        var sampleHtml = @"
    <!DOCTYPE uuebview href='resources://Views/TMMyInfoView/UUebTags'>
    <tmbody>
        <bg>
        	<textbg>
        		<textbox>
    	    		<tmp>koko ni nihongo ga iikanji ni hairu. <br> 2line content! 2line content! 2line content!2 line content! a good thing.<tma href='somewhere'>link</tma>a long text will make large window. something like this.</tmp>
    	    		<updatetext>omake! abc d</updatetext>
                    <tmp>ef ghijklm</tmp>
                    <updatetext>aaaaaaaaaaaaannnnnnnnnnnnnnn</updatetext>
    	    	</textbox>
    	    </textbg>
        </bg>
    </tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sampleHtml, treeSource => { tree = treeSource; }, 300);

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeSampleView2_HiddenBreakView()
    {
        var sampleHtml = @"
    <!DOCTYPE uuebview href='resources://Views/TMMyInfoView/UUebTags'>
    <tmbody>
        <bg>
        	<titlebox>
        		<titletext>レモン一個ぶんのビタミンC</titletext>
        	</titlebox>
        	<newbadge></newbadge>
        	<textbg>
        		<textbox>
    	    		<updatetext>koko ni nihongo ga iikanji ni hairu. good thing. long text will make large window. like this.</updatetext>
    	    		<updatetext hidden='true' listen='readmore'>omake!<img src='https://dummyimage.com/100.png/07f/fff'/></updatetext>
                    <img src='https://dummyimage.com/100.png/09f/fff' button='true' id='readmore'/>
    	    	</textbox>
    	    </textbg>
        </bg>
    </tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sampleHtml, treeSource => { tree = treeSource; }, 300);

        yield return Show(tree);
    }

    [MTest]
    public IEnumerator LayoutSampleView2_HiddenBreakView()
    {
        var sampleHtml = @"
    <!DOCTYPE uuebview href='resources://Views/TMMyInfoView/UUebTags'>
    <tmbody>
        <bg>
        	<textbg>
        		<textbox>
    	    		<updatetext>koko ni nihongo ga iikanji ni hairu. good thing. long text will make large window. like this.</updatetext>
    	    		<updatetext hidden='true' listen='readmore'>omake!</updatetext>
    	    	</textbox>
    	    </textbg>
        </bg>
    </tmbody>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sampleHtml, treeSource => { tree = treeSource; }, 300);
        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeGroupHeightChanged()
    {
        var sampleHtml = @"
    <!DOCTYPE uuebview href='resources://Views/TMMyInfoView/UUebTags'>
    <tmbody>
    	<bg>
    		<titlebox>
    			<titletext>Sample</titletext>
    		</titlebox>
    		<newbadge/>
    		<newbadge/>
            <textbg>
            </textbg>
        </bg>
    </tmbody>
    ";
        TagTree tree = null;
        yield return CreateLayoutedTree(sampleHtml, treeSource => { tree = treeSource; }, 300);
        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeAfterLayer()
    {
        var sampleHtml = @"
    <!DOCTYPE uuebview href='resources://Views/TMMyInfoView/UUebTags'>
    <tmbody>
    	<bg>
        </bg>
        <tmp>hey!</tmp>
    </tmbody>
    ";
        TagTree tree = null;
        yield return CreateLayoutedTree(sampleHtml, treeSource => { tree = treeSource; }, 300);
        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeBrBrSupport()
    {
        var sampleHtml = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmp>
        something<br><br>
        else
    </tmp>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sampleHtml, treeSource => { tree = treeSource; });
        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeCenterAlignSupport()
    {
        var sampleHtml = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmp align='center'>aaa</tmp>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sampleHtml, treeSource => { tree = treeSource; });
        yield return Show(tree);
    }

    [MTest]
    public IEnumerator MaterializeRightAlignSupport()
    {
        var sampleHtml = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmp align='right'>aaa</tmp>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sampleHtml, treeSource => { tree = treeSource; });
        yield return Show(tree);
    }



    [MTest]
    public IEnumerator PSupport()
    {
        var sampleHtml = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmp>
        p1<tma href=''>a1</tma>p2
    </tmp>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sampleHtml, treeSource => { tree = treeSource; });
        yield return Show(tree);
    }

    [MTest]
    public IEnumerator PSupport2()
    {
        Debug.LogWarning("保留");
        yield break;

        var sampleHtml = @"
        <!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmp>
        p1<tma href=''>a1</tma>p2
    </tmp><tmp>
        p3
    </tmp>";
        TagTree tree = null;
        yield return CreateLayoutedTree(sampleHtml, treeSource => { tree = treeSource; });
        yield return Show(tree);
    }
}