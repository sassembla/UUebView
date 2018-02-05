using System;
using System.Collections;
using System.Collections.Generic;
using Miyamasu;
using UnityEngine;

public class TMProExtensionTests : MiyamasuTestRunner
{
    GameObject eventReceiverGameObj;
    GameObject view;

    private static int index;
    private void Show(GameObject view, Action loaded = null)
    {
        var canvas = GameObject.Find("Canvas/TMProExtensionTestPlace");
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

    [MTeardown]
    public void Teardown()
    {

    }

    [MTest]
    public IEnumerator SingleTMProPrefab()
    {
        var source = @"
    <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
    <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>
    <textmeshtxt>Miyamasu Runtime Console2</textmeshtxt>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator MultipleTMProPrefab()
    {
        var source = @"
        <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
        <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>
        <textmeshtxt2>Miyamasu Runtime Console2</textmeshtxt2>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator SmallMiddleLarge()
    {
        var source = @"
        <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
        <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>
        <textmeshtxt2>Miyamasu Runtime Console2</textmeshtxt2>
        <textmeshtxt3>Miyamasu Runtime Console3</textmeshtxt3>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator MultipleTMProPrefabComplex()
    {
        var source = @"
        <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
        <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>
        <textmeshtxt2>Miyamasu Runtime Console2</textmeshtxt2>
        <textmeshtxt>Miyamasu Runtime Console3</textmeshtxt>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator MultipleTMProPrefabComplexVariety()
    {
        var source = @"
        <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
        <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>
        <textmeshtxt2>Miyamasu Runtime Console2</textmeshtxt2>
        <textmeshtxt3>Miyamasu Runtime Console3</textmeshtxt3>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator MultipleTMProPrefabComplexWithImage()
    {
        var source = @"
        <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
        <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>
        <textmeshtxt2>Miyamasu Runtime Console2</textmeshtxt2>
        <textmeshtxt3>Miyamasu Runtime Console3</textmeshtxt3>
        <img src='https://dummyimage.com/30.png/09f/fff'/>
        <textmeshtxt>Miyamasu Runtime Console4</textmeshtxt>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator MultipleTMProPrefabComplexWithImageOnHeadOfLine()
    {
        var source = @"
        <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
        <img src='https://dummyimage.com/30.png/09f/fff'/>
        <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>
        <textmeshtxt2>Miyamasu Runtime Console2</textmeshtxt2>
        <textmeshtxt3>Miyamasu Runtime Console3</textmeshtxt3>
        <textmeshtxt>Miyamasu Runtime Console4</textmeshtxt>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator MultipleTMProPrefabComplexWithImageOnHeadOfLine40()
    {
        var source = @"
        <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
        <img src='https://dummyimage.com/40.png/09f/fff'/>
        <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>
        <textmeshtxt2>Miyamasu Runtime Console2</textmeshtxt2>
        <textmeshtxt3>Miyamasu Runtime Console3</textmeshtxt3>
        <textmeshtxt>Miyamasu Runtime Console4</textmeshtxt>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator MultipleTMProPrefabComplexWithImageOnHeadOfLine40_2()
    {
        var source = @"
        <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
        <textmeshtxt>M</textmeshtxt>
        <img src='https://dummyimage.com/40.png/09f/fff'/>
        <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>
        <textmeshtxt2>Miyamasu Runtime Console2</textmeshtxt2>
        <textmeshtxt3>Miyamasu Runtime Console3</textmeshtxt3>
        <textmeshtxt>Miyamasu Runtime Console4</textmeshtxt>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator MultipleTMProPrefabComplexWithImageOnHeadOfLine40_3()
    {
        var source = @"
        <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
        <textmeshtxt>Miyamasu</textmeshtxt>
        <img src='https://dummyimage.com/40.png/09f/fff'/>
        <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>
        <textmeshtxt2>Miyamasu Runtime Console2</textmeshtxt2>
        <textmeshtxt3>Miyamasu Runtime Console3</textmeshtxt3>
        <textmeshtxt>Miyamasu Runtime Console4</textmeshtxt>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator MultipleTMProPrefabComplexWithImageOnHeadOfLine60()
    {
        var source = @"
        <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
        <img src='https://dummyimage.com/60.png/09f/fff'/>
        <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>
        <textmeshtxt2>Miyamasu Runtime Console2</textmeshtxt2>
        <textmeshtxt3>Miyamasu Runtime Console3</textmeshtxt3>
        <textmeshtxt>Miyamasu Runtime Console4</textmeshtxt>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator MultipleTMProPrefabComplexWithImageOnHeadOfLine100()
    {
        var source = @"
        <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
        <img src='https://dummyimage.com/100.png/09f/fff'/>
        <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>
        <textmeshtxt2>Miyamasu Runtime Console2</textmeshtxt2>
        <textmeshtxt3>Miyamasu Runtime Console3</textmeshtxt3>
        <textmeshtxt>Miyamasu Runtime Console4</textmeshtxt>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator LargeImageWithTMProText()
    {
        var source = @"
        <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
        <img src='https://dummyimage.com/200.png/09f/fff'/>
        <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator TMProTextWithLargeImageWithTMProText()
    {
        var source = @"
        <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
        <textmeshtxt>Miyamasu</textmeshtxt>
        <img src='https://dummyimage.com/200.png/09f/fff'/>
        <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator TMProTextWithLargeImageWithTMProText2()
    {
        var source = @"
        <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
        <textmeshtxt>Miyamasu</textmeshtxt>
        <img src='https://dummyimage.com/200.png/09f/fff'/>
        <textmeshtxt>Miyamasu</textmeshtxt>
        <img src='https://dummyimage.com/200.png/09f/fff'/>
        <textmeshtxt>MiyamaX</textmeshtxt>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }

    [MTest]
    public IEnumerator TMProTextWithLargeImageWithTMProText3()
    {
        var source = @"
            <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
            <textmeshtxt>Miyamasu
            <img src='https://dummyimage.com/200.png/09f/fff'/>
            Miyamasu
            <img src='https://dummyimage.com/200.png/09f/fff'/>
            MiyamaX</textmeshtxt>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }
}