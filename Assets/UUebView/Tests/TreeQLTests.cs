
using System;
using System.Collections;
using Miyamasu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UUebView;

/**
    test for UUebView generator.
*/
public class TreeQLTests : MiyamasuTestRunner {
    GameObject eventReceiverGameObj;
    GameObject view;
    
    private static int index;
    private void Show (GameObject view, Action loaded=null) {
        var canvas = GameObject.Find("Canvas/TreeQLTestPlace");
        if (canvas == null) {
            var prefab = Resources.Load<GameObject>("TestPrefabs/Canvas");
            var canvasBase = GameObject.Instantiate(prefab);
            canvasBase.name = "Canvas";
            canvas = GameObject.Find("Canvas/TreeQLTestPlace");
        }

        var baseObj = new GameObject("base");
        

        // ベースオブジェクトを見やすい位置に移動
        var baseObjRect = baseObj.AddComponent<RectTransform>();
        baseObjRect.anchoredPosition = new Vector2(100 * index, 0);
        baseObjRect.anchorMin = new Vector2(0,1);
        baseObjRect.anchorMax = new Vector2(0,1);
        baseObjRect.pivot = new Vector2(0,1);
        

        baseObj.transform.SetParent(
            canvas.transform, false);

        view.transform.SetParent(baseObj.transform, false);

        index++;
        if (loaded != null) {
            loaded();
        }
    }

    [MSetup] public void Setup () {
        eventReceiverGameObj = new GameObject("controller");
        eventReceiverGameObj.AddComponent<TestReceiver>();
    }

    private void ShowLayoutRecursive (TagTree tree, ResourceLoader loader) {
        Debug.Log("tree:" + loader.GetTagFromValue(tree.tagValue) + " offsetX:" + tree.offsetX + " offsetY:" + tree.offsetY + " width:" + tree.viewWidth + " height:" + tree.viewHeight);
        foreach (var child in tree.GetChildren()) {
            ShowLayoutRecursive(child, loader);
        }
    }

    [MTest] public IEnumerator GetComponent () {
        var source = @"
<body>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        
        var done = false;
        
        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids => {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
        
        Show(view);

        yield return WaitUntil(
            () => done, () => {throw new TimeoutException("too late.");}, 5
        );

        var comp = view.GetComponent<UUebViewComponent>();
        NotNull(comp);
    }

    [MTest] public IEnumerator AppendContent () {
        var source = @"
<body>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        
        var done = false;
        
        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids => {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
        
        Show(view);

        yield return WaitUntil(
            () => done, () => {throw new TimeoutException("too late.");}, 5
        );

        var done2 = false;
        eventReceiverGameObj.GetComponent<TestReceiver>().OnUpdated = ids => {
            done2 = true;
        };

        var comp = view.GetComponent<UUebViewComponent>();
        comp.AppendContentToLast("<p>test</p>");

        yield return WaitUntil(
            () => done2, () => {throw new TimeoutException("too late.");}, 5
        );

        var layoutedTree = comp.Core.layoutedTree;
        True(layoutedTree.GetChildren().Count == 2);
    }

    [MTest] public IEnumerator AppendContentToContent () {
        var source = @"
<body>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        
        var done = false;
        
        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids => {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
        
        Show(view);
        yield return WaitUntil(
            () => done, () => {throw new TimeoutException("too late.");}, 5
        );

        var comp = view.GetComponent<UUebViewComponent>();

        {
            var layoutedTree = comp.Core.layoutedTree;
            True(layoutedTree.GetChildren().Count == 1);
            True(layoutedTree.GetChildren()[0].GetChildren().Count == 2);
        }

        var done2 = false;
        eventReceiverGameObj.GetComponent<TestReceiver>().OnUpdated = ids => {
            done2 = true;
        };

        comp.AppendContentToTree("<p>last</p>", "/body");

        yield return WaitUntil(
            () => done2, () => {throw new TimeoutException("too late.");}, 5
        );

        {
            var layoutedTree = comp.Core.layoutedTree;
            True(layoutedTree.GetChildren().Count == 1);            
            True(layoutedTree.GetChildren()[0].GetChildren().Count == 3);
        }
    }

    [MTest] public IEnumerator DeleteBody () {
        var source = @"
<body>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        
        var done = false;
        
        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids => {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
        
        Show(view);

        yield return WaitUntil(
            () => done, () => {throw new TimeoutException("too late.");}, 5
        );

        var comp = view.GetComponent<UUebViewComponent>();
        comp.DeleteByPoint("/body");

        var layoutedTree = comp.Core.layoutedTree;
        True(layoutedTree.GetChildren().Count == 0);
    }

    [MTest] public IEnumerator DeleteContentOfBody () {
        var source = @"
<body>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        
        var done = false;
        
        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids => {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
        
        Show(view);

        yield return WaitUntil(
            () => done, () => {throw new TimeoutException("too late.");}, 5
        );

        var comp = view.GetComponent<UUebViewComponent>();
        
        {
            var layoutedTree = comp.Core.layoutedTree;
            True(layoutedTree.GetChildren().Count == 1);
            True(layoutedTree.GetChildren()[0].GetChildren().Count == 2);
        }

        var done2 = false;
        eventReceiverGameObj.GetComponent<TestReceiver>().OnUpdated = ids => {
            done2 = true;
        };

        
        comp.DeleteByPoint("/body/img");

        yield return WaitUntil(
            () => done2, () => {throw new TimeoutException("too late.");}, 5
        );

        {
            var layoutedTree = comp.Core.layoutedTree;
            True(layoutedTree.GetChildren().Count == 1);
            True(layoutedTree.GetChildren()[0].GetChildren().Count == 1);
        }
    }


    [MTest] public IEnumerator GetNewContentIds () {
        var source = @"
<h1 id='test'>Miyamasu Runtime Console</h1>";
        var done = false;
        
        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids => {
            True(ids.Length == 1);
            AreEqual(ids[0], "test");
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(300,100));
        
        Show(view);

        yield return WaitUntil(
            () => done, () => {throw new TimeoutException("too late.");}, 5
        );
    }

    [MTest] public IEnumerator AddNewContentIds () {
        var source = @"
<body>something1.<img src='https://dummyimage.com/100.png/09f/fff' id='test'/></body>";
        
        var done = false;
        var currentIds = new string[0];
        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids => {
            currentIds = ids;
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
        
        Show(view);
        yield return WaitUntil(
            () => done, () => {throw new TimeoutException("too late.");}, 5
        );

        var comp = view.GetComponent<UUebViewComponent>();

        var done2 = false;
        var appendedIds = new string[0];
        eventReceiverGameObj.GetComponent<TestReceiver>().OnUpdated = ids => {
            appendedIds = ids;
            True(appendedIds.Length == 1);
            AreEqual(appendedIds[0], "appended");
            done2 = true;
        };

        comp.AppendContentToTree("<p id='appended'>last</p>", "/body");

        yield return WaitUntil(
            () => done2, () => {throw new TimeoutException("too late.");}, 5
        );
    }

    // 何件目、っていうテストしたい。


    // boxをスキップしたりしないといけない



}