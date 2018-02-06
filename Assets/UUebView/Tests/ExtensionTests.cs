// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Miyamasu;
// using UnityEngine;

// public class ExtensionTests : MiyamasuTestRunner
// {

//     GameObject eventReceiverGameObj;
//     GameObject view;

//     private static int index;
//     private void Show(GameObject view, Action loaded = null)
//     {
//         var canvas = GameObject.Find("Canvas/UUebViewCoreTestPlace");
//         if (canvas == null)
//         {
//             var prefab = Resources.Load<GameObject>("TestPrefabs/Canvas");
//             var canvasBase = GameObject.Instantiate(prefab);
//             canvasBase.name = "Canvas";
//             canvas = GameObject.Find("Canvas/MaterializeTestPlace");
//         }
//         var baseObj = new GameObject("base");


//         // ベースオブジェクトを見やすい位置に移動
//         var baseObjRect = baseObj.AddComponent<RectTransform>();
//         baseObjRect.anchoredPosition = new Vector2(100 * index, 0);
//         baseObjRect.anchorMin = new Vector2(0, 1);
//         baseObjRect.anchorMax = new Vector2(0, 1);
//         baseObjRect.pivot = new Vector2(0, 1);


//         baseObj.transform.SetParent(
//             canvas.transform, false);

//         view.transform.SetParent(baseObj.transform, false);

//         index++;
//         if (loaded != null)
//         {
//             loaded();
//         }
//     }

//     [MSetup]
//     public void Setup()
//     {
//         eventReceiverGameObj = new GameObject("controller");
//         eventReceiverGameObj.AddComponent<TestReceiver>();
//     }

//     [MTest]
//     public IEnumerator DefaultText()
//     {
//         var source = @"
//     <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
//     <body>Miyamasu Runtime Console</body>";
//         var done = false;

//         eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
//         {
//             done = true;
//         };
//         view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

//         Show(view);

//         yield return WaitUntil(
//             () => done, () => { throw new TimeoutException("too late."); }, 50
//         );
//     }

//     [MTest]
//     public IEnumerator UseTextMeshPro()
//     {
//         var source = @"
//     <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
//     <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>";
//         var done = false;

//         eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
//         {
//             done = true;
//         };
//         view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

//         Show(view);

//         yield return WaitUntil(
//             () => done, () => { throw new TimeoutException("too late."); }, 50
//         );
//     }

//     [MTest]
//     public IEnumerator UseText2()
//     {
//         var source = @"
//     <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
//     <body>Miyamasu Runtime Console</body>
//     <body>Miyamasu Runtime Console2</body>";
//         var done = false;

//         eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
//         {
//             done = true;
//         };
//         view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

//         Show(view);

//         yield return WaitUntil(
//             () => done, () => { throw new TimeoutException("too late."); }, 50
//         );
//         // Debug.LogWarning("改行による文字列コンテンツの順不同が発生している。ふーむ。");
//     }

//     [MTest]
//     public IEnumerator UseTextMeshPro2()
//     {
//         var source = @"
//     <!DOCTYPE uuebview href='resources://Views/TextMeshPro/UUebTags'>
//     <textmeshtxt>Miyamasu Runtime Console</textmeshtxt>
//     <textmeshtxt>Miyamasu Runtime Console2</textmeshtxt>";
//         var done = false;

//         eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
//         {
//             done = true;
//         };
//         view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100, 100));

//         Show(view);

//         yield return WaitUntil(
//             () => done, () => { throw new TimeoutException("too late."); }, 50
//         );
//     }
// }
