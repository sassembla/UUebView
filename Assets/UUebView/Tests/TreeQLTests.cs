
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
        var canvas = GameObject.Find("Canvas/UUebViewCoreTestPlace");
        if (canvas == null) {
            var prefab = Resources.Load<GameObject>("TestPrefabs/Canvas");
            var canvasBase = GameObject.Instantiate(prefab);
            canvasBase.name = "Canvas";
            canvas = GameObject.Find("Canvas/MaterializeTestPlace");
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
        
        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = () => {
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

//     [MTest] public IEnumerator GetTree () {
//         var source = @"
// <body>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        
//         var done = false;
        
//         eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = () => {
//             done = true;
//         };
//         view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
        
//         Show(view);

//         yield return WaitUntil(
//             () => done, () => {throw new TimeoutException("too late.");}, 5
//         );

//         var comp = view.GetComponent<UUebViewComponent>();
//         var treePoint = comp.TreePointOf("/body");
//         NotNull(treePoint);
//     }

    [MTest] public IEnumerator AppendContent () {
        var source = @"
<body>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        
        var done = false;
        
        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = () => {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
        
        Show(view);

        yield return WaitUntil(
            () => done, () => {throw new TimeoutException("too late.");}, 5
        );

        var comp = view.GetComponent<UUebViewComponent>();
        comp.AppendContentToLast("<p>test</p>");
    }

//     [MTest] public IEnumerator AppendContentToLast () {
//         var source = @"
// <body>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        
//         var done = false;
        
//         eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = () => {
//             done = true;
//         };
//         view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
        
//         Show(view);

//         yield return WaitUntil(
//             () => done, () => {throw new TimeoutException("too late.");}, 5
//         );

//         var comp = view.GetComponent<UUebViewComponent>();
//         var treePoint = comp.TreePointOf("/body");
//         treePoint.AppendContentToLast("<p>last<p>");
//     }

    [MTest] public IEnumerator Delete () {
        var source = @"
<body>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        
        var done = false;
        
        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = () => {
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
}