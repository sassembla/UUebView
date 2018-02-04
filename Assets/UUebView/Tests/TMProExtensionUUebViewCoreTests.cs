
using System;
using System.Collections;
using Miyamasu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UUebView;

/**
    test for UUebView generator.
*/
public class TMProExtensionUUebViewCoreTests : MiyamasuTestRunner
{
    GameObject eventReceiverGameObj;
    GameObject view;

    private static int index;
    private void Show(GameObject view, Action loaded = null)
    {
        var canvas = GameObject.Find("Canvas/TMProExtensionUUebViewCoreTestPlace");
        if (canvas == null)
        {
            var prefab = Resources.Load<GameObject>("TestPrefabs/Canvas");
            var canvasBase = GameObject.Instantiate(prefab);
            canvasBase.name = "Canvas";
            canvas = GameObject.Find("Canvas/MaterializeTestPlace");
        }
        var baseObj = new GameObject("base");


        // ベースオブジェクトを見やすい位置に移動
        var baseObjRect = baseObj.AddComponent<RectTransform>();
        baseObjRect.anchoredPosition = new Vector2(100 * index, 0);
        baseObjRect.anchorMin = new Vector2(0, 1);
        baseObjRect.anchorMax = new Vector2(0, 1);
        baseObjRect.pivot = new Vector2(0, 1);


        baseObj.transform.SetParent(
            canvas.transform, false);

        view.transform.SetParent(baseObj.transform, false);

        index++;
        if (loaded != null)
        {
            loaded();
        }
    }

    [MSetup]
    public void Setup()
    {
        eventReceiverGameObj = new GameObject("controller");
        eventReceiverGameObj.AddComponent<TestReceiver>();
    }

    private void ShowLayoutRecursive(TagTree tree, ResourceLoader loader)
    {
        Debug.Log("tree:" + loader.GetTagFromValue(tree.tagValue) + " offsetX:" + tree.offsetX + " offsetY:" + tree.offsetY + " width:" + tree.viewWidth + " height:" + tree.viewHeight);
        foreach (var child in tree.GetChildren())
        {
            ShowLayoutRecursive(child, loader);
        }
    }

    [MTest]
    public IEnumerator GenerateSingleViewFromSource()
    {
        var source = @"<!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></tmbody>";

        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator GenerateSingleViewFromUrl()
    {
        var url = "resources://UUebViewTest/TMUUebViewTest.html";

        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromUrl(eventReceiverGameObj, url, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator LoadThenReload()
    {
        var source = @"<!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>
        reload sample.
        <img src='https://dummyimage.com/100.png/09f/fff'/>
    </tmbody>";

        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );

        var done2 = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done2 = true;
        };
        var core = view.GetComponent<UUebViewComponent>().Core;
        core.Reload();

        yield return WaitUntil(
            () => done2, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator ShowAndHide()
    {
        var source = @"<!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>
        something3.
        <img src='https://dummyimage.com/100.png/09f/fff' id='button' button='true'/>
        <tmp hidden='false' listen='button'>else other long text.<tma href='somewhere'> and link.</tma></tmp>
    </tmbody>";

        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator HideAndShow()
    {
        var source = @"<!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>
        something3.
        <img src='https://dummyimage.com/100.png/09f/fff' id='button' button='true'/>
        <tmp hidden='true' listen='button'>else</tmp>
    </tmbody>";

        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator CascadeButton()
    {
        var source = @"<!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>
        <img src='https://dummyimage.com/100.png/09f/fff' id='button' button='true'/>
        <img hidden='true' src='https://dummyimage.com/100.png/08f/fff' id='button2' button='true' listen='button'/>
        <img hidden='true' src='https://dummyimage.com/100.png/07f/fff' id='button3' button='true' listen='button2'/>
        <img hidden='true' src='https://dummyimage.com/100.png/06f/fff' id='button4' button='true' listen='button3'/>
    </tmbody>";

        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator CachedContent()
    {
        var source = @"<!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>
        <img src='https://dummyimage.com/100.png/09f/fff' id='button' button='true'/>
        <img hidden='true' src='https://dummyimage.com/100.png/08f/fff' id='button2' button='true' listen='button'/>
        <img hidden='true' src='https://dummyimage.com/100.png/07f/fff' id='button3' button='true' listen='button2'/>
        <img hidden='true' src='https://dummyimage.com/100.png/06f/fff' id='button4' button='true' listen='button3'/>
    </tmbody>";

        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator ShowLinkByButton()
    {
        var source = @"<!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>
        something3.
        <img src='https://dummyimage.com/100.png/09f/fff' id='button1' button='true'/>
        <tma href='href test' hidden='true' listen='button1'>link</tma>
    </tmbody>";

        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator ManyImages()
    {
        var source = @"
    <!DOCTYPE uuebview href='resources://Views/TMLayoutHTMLWithCustomTag/UUebTags'>
    <tmbody>
        something4.
        <customimg src='https://dummyimage.com/100.png/09f/fff' id='button1' button='true'/>
        <customimg src='https://dummyimage.com/101.png/09f/fff' id='button1' button='true'/>
        <customimg src='https://dummyimage.com/102.png/09f/fff' id='button1' button='true'/>
        <customimg src='https://dummyimage.com/103.png/09f/fff' id='button1' button='true'/>
        <customimg src='https://dummyimage.com/104.png/09f/fff' id='button1' button='true'/>
        <customimg src='https://dummyimage.com/105.png/09f/fff' id='button1' button='true'/>
        <customimg src='https://dummyimage.com/106.png/09f/fff' id='button1' button='true'/>
        <customimg src='https://dummyimage.com/107.png/09f/fff' id='button1' button='true'/>
        <customimg src='https://dummyimage.com/108.png/09f/fff' id='button1' button='true'/>
        <customimg src='https://dummyimage.com/109.png/09f/fff' id='button1' button='true'/>
        <tma href='href test' hidden='true' listen='button1'>link</tma>
    </tmbody>";

        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator Sample2()
    {
        var source = @"
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
    	    		<updatetext hidden='true' listen='readmore'>omake!<img src='https://dummyimage.com/100.png/07f/fff'/><img src='https://dummyimage.com/100.png/07f/fff'/><img src='https://dummyimage.com/100.png/07f/fff'/><img src='https://dummyimage.com/100.png/07f/fff'/><img src='https://dummyimage.com/100.png/07f/fff'/><img src='https://dummyimage.com/100.png/07f/fff'/><img src='https://dummyimage.com/100.png/07f/fff'/><img src='https://dummyimage.com/100.png/07f/fff'/><img src='https://dummyimage.com/100.png/07f/fff'/><img src='https://dummyimage.com/100.png/07f/fff'/><img src='https://dummyimage.com/100.png/07f/fff'/><img src='https://dummyimage.com/100.png/07f/fff'/></updatetext>
                    <img src='https://dummyimage.com/100.png/09f/fff' button='true' id='readmore'/>
    	    	</textbox>
    	    </textbg>
        </bg>
    </tmbody>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(300, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator HideThenShow()
    {
        var source = @"
<!DOCTYPE uuebview href='resources://Views/TMMyInfoView/UUebTags'>
<bg>
    <textbg>
        <textbox>
            <updatetext>koko ni nihongo ga iikanji ni hairu.<br> good thing. long text will make large window. like this.</updatetext>
            <updatetext hidden='true' listen='readmore'>omake!</updatetext>
        </textbox>
    </textbg>
</bg>";
        UUebView.UUebViewComponent uUebView = null;

        var done = false;
        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            uUebView = view.GetComponent<UUebViewComponent>();
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(300, 100));

        var shown = false;
        Show(view, () => { shown = true; });

        yield return WaitUntil(
            () => shown && done,
            () => { throw new TimeoutException("too late."); },
            5
        );
        {
            var tree = uUebView.Core.layoutedTree;
            var targetTextBox = tree.GetChildren()[0].GetChildren()[0].GetChildren()[0].GetChildren()[0].GetChildren()[0].GetChildren()[0].GetChildren()[1];
            True(targetTextBox.offsetY.ToString() == "19.55124", "not match, targetTextBox.offsetY:" + targetTextBox.offsetY);
        }

        // show hidden contents.
        {
            var updated = false;
            eventReceiverGameObj.GetComponent<TestReceiver>().OnUpdated = ids =>
            {
                updated = true;
            };
            uUebView.EmitButtonEventById(null, string.Empty, "readmore");

            yield return WaitUntil(
                () => updated,
                () => { throw new TimeoutException("too late."); },
                5
            );
        }

        // hide hidden contents again.
        {
            var updated = false;
            eventReceiverGameObj.GetComponent<TestReceiver>().OnUpdated = ids =>
            {
                updated = true;
            };
            uUebView.EmitButtonEventById(null, string.Empty, "readmore");

            yield return WaitUntil(
                () => updated,
                () => { throw new TimeoutException("too late."); }
            );
        }
        {
            var tree = uUebView.Core.layoutedTree;
            var targetTextBox = tree.GetChildren()[0].GetChildren()[0].GetChildren()[0].GetChildren()[0].GetChildren()[0].GetChildren()[0].GetChildren()[1];
            True(targetTextBox.offsetY.ToString() == "19.55124", "not match, targetTextBox.offsetY:" + targetTextBox.offsetY);
        }
        // ShowLayoutRecursive(tree, uUebView.Core.resLoader);
    }

    [MTest]
    public IEnumerator UnityRichTextColorSupport()
    {
        var source = @"<!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>
        <color=#ff0000ff>red</color> <color=#008000ff>green</color> <color=#0000ffff>blue</color>
    </tmbody>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(300, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator UnityRichTextSizeSupport()
    {
        var source = @"<!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmp>
        a<size=50>large string</size>b
    </tmp>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(300, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator SetViewName()
    {
        var viewName = "SetViewName";
        var source = @"<!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>
        <color=#ff0000ff>red</color> <color=#008000ff>green</color> <color=#0000ffff>blue</color>
    </tmbody>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(300, 100), null, null, viewName);

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator BrOnHeadOfContents()
    {
        var source = @"<!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>
        <tmp>
            <br>
            aaa
        </tmp>
    </tmbody>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(300, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator BrOnHeadOfContents2()
    {
        var source = @"<!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmbody>
        <tmh1>Miyamasu Runtime Console</tmh1><br>
    </tmbody>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(300, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator BrOnHeadOfContents3()
    {
        var source = @"<!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmh1>Miyamasu Runtime Console</tmh1><br>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(300, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator BrOnHeadOfContents4()
    {
        var source = @"<!DOCTYPE uuebview href='resources://Views/TMDefault/UUebTags'>
    <tmh1>Miyamasu Runtime Console</tmh1><br>
    <tmp>
    	<br>ddd<br>
    </tmp>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(300, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator MassiveView()
    {
        var source = @"
    <!DOCTYPE uuebview href='resources://Views/TMLayoutHTMLWithCustomTag/UUebTags'>
    <tmbody>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    <customtag><custombg><textbg><customtext>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </customtext></textbg></custombg></customtag>
    else
    <customimg src='https://dummyimage.com/10x20/000/fff'/>
    </tmbody>";

        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(800, 1000));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }

    [MTest]
    public IEnumerator ErrorString()
    {
        var source = @"
    <!DOCTYPE uuebview href='resources://Views/TMLayoutHTMLWithCustomTag/UUebTags'>
    <tmbody>
    <customtag><custombg><textbg><customtext>
    日本語と半角スペースで何かが起きるっぽい？( )さて？ (~) と、 (〜)と、以上。
    </customtext></textbg></custombg></customtag>
    </tmbody>
    ";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(800, 1000));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 5
        );
    }
}