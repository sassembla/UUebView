
using System;
using System.Collections;
using System.Linq;
using Miyamasu;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.SceneManagement;
using UUebView;

/**
    test for scroll view.
*/
public class ScrollViewTests : MiyamasuTestRunner
{
    private GameObject targetScrollView;
    private static int index;

    [MSetup]
    public void Setup()
    {
        var scrollViewPrefab = Resources.Load("TestPrefabs/TestScrollView") as GameObject;
        var canvasCandidate = GameObject.Find("Canvas");

        if (canvasCandidate == null)
        {
            canvasCandidate = new GameObject("canvas");
            canvasCandidate.AddComponent<Canvas>();
        }

        targetScrollView = GameObject.Instantiate(scrollViewPrefab, canvasCandidate.transform);
        var rectTrans = targetScrollView.GetComponent<RectTransform>();
        var s = new Vector2(rectTrans.anchoredPosition.x + (index * rectTrans.sizeDelta.x), rectTrans.anchoredPosition.y);

        rectTrans.anchoredPosition = s;

        index++;
    }

    public static void Info_Show(GameObject scrollView, string html, string viewName = ConstSettings.ROOTVIEW_NAME)
    {
        var eventReceiverCandidate = scrollView.GetComponents<Component>().Where(component => component is IUUebViewEventHandler).FirstOrDefault();
        if (eventReceiverCandidate == null)
        {
            throw new Exception("information scroll view should have IUUebViewEventHandler implemented component.");
        }

        var content = scrollView.GetComponentsInChildren<RectTransform>().Where(t => t.gameObject.name == "Content").FirstOrDefault();
        if (content == null)
        {
            throw new Exception("information scroll view should have 'Content' GameObject like uGUI default ScrollView.");
        }

        // 完了するまで見えない
        scrollView.GetComponent<CanvasGroup>().alpha = 0;
        var viewSize = scrollView.GetComponent<RectTransform>().sizeDelta;

        var view = UUebViewComponent.GenerateSingleViewFromHTML(scrollView, html, viewSize, null, null, viewName);
        view.transform.SetParent(content.gameObject.transform, false);
    }

    /*
        layout後、特定のコンポーネント座標が取得できるメソッドがあればいい。
        ・範囲指定して枝からコンポーネントを根こそぎ獲得する
        ・listで取得する
        みたいなのでいい気がする。となるとLayoutMachineのテストか。
     */

    [MTest]
    public IEnumerator L3()
    {
        var source = @"
        <body>something</body><br>
        <body>something2</body><br>
        <body>something3</body><br>";

        var done = false;

        var eventReceiverGameObj = targetScrollView.GetComponent<TestReceiver>();
        eventReceiverGameObj.OnLoaded = ids =>
        {
            targetScrollView.GetComponent<CanvasGroup>().alpha = 1;
            done = true;
        };

        Info_Show(targetScrollView, source);
        yield return WaitUntil(() => done, () => { throw new TimeoutException("timeout."); });
    }

    [MTest]
    public IEnumerator L6()
    {
        var source = @"
        <body>something</body><br>
        <body>something2</body><br>
        <body>something3</body><br>
        <body>something4</body><br>
        <body>something5</body><br>
        <body>something6</body><br>";

        var done = false;

        var eventReceiverGameObj = targetScrollView.GetComponent<TestReceiver>();
        eventReceiverGameObj.OnLoaded = ids =>
        {
            targetScrollView.GetComponent<CanvasGroup>().alpha = 1;
            done = true;
        };

        Info_Show(targetScrollView, source);
        yield return WaitUntil(() => done, () => { throw new TimeoutException("timeout."); });
    }

    [MTest]
    public IEnumerator L10()
    {
        // 7まで映る。で、
        var source = @"
        <body>something</body><br>
        <body>something2</body><br>
        <body>something3</body><br>
        <body>something4</body><br>
        <body>something5</body><br>
        <body>something6</body><br>
        <body>something7</body><br>
        <body>something8</body><br>
        <body>something9</body><br>
        <body>something10</body><br>";

        var done = false;

        var eventReceiverGameObj = targetScrollView.GetComponent<TestReceiver>();
        eventReceiverGameObj.OnLoaded = ids =>
        {
            targetScrollView.GetComponent<CanvasGroup>().alpha = 1;
            done = true;
        };

        Info_Show(targetScrollView, source);
        yield return WaitUntil(() => done, () => { throw new TimeoutException("timeout."); });
    }

}