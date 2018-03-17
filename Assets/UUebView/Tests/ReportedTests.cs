// using System.Collections;
// using Miyamasu;
// using UnityEngine;

// public class ReportedTests : MiyamasuTestRunner
// {
//     GameObject eventReceiverGameObj;

//     [MSetup]
//     public void Setup()
//     {
//         eventReceiverGameObj = new GameObject("controller");
//         eventReceiverGameObj.AddComponent<TestReceiver>();
//     }
//     [MTest]
//     public IEnumerator RankerInfoVIew()
//     {
//         Debug.Log("あとでセットアップしよう。今は単独でやってしまう。");
//         // UUebView.Generate
//         yield return null;
//     }
// }