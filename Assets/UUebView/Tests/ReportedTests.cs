using System;
using System.Collections;
using Miyamasu;
using UnityEngine;

public class ReportedTests : MiyamasuTestRunner
{
    GameObject eventReceiverGameObj;
    GameObject view;
    private static int index;
    private void Show(GameObject view, Action loaded = null)
    {
        var canvas = GameObject.Find("Canvas/UUebViewCoreTestPlace");
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

    [MTest]
    public IEnumerator RankerInfoVIew()
    {
        var viewWidth = 1600;

        var source = @"
    <!DOCTYPE uuebview href='resources://Views/HelpListView/UUebTags'>
<body>
    <bg>
        <titlebg>
            <titlebox>
                <titletext align='center'>ヘルプ</titletext>
            </titlebox>
        </titlebg>
    </bg>

    <spacerbg/>
    <br>

    <bg>
        <heading button='true' id='stage'>
            <headingbg>
                <headingbox>
                    <headingtext align='center'>ステージ</headingtext>
                </headingbox>
            </headingbg>
        </heading>
    </bg>

    <bg>
        <subheadingcontainer hidden='true' listen='stage'>
            <subheading>
                <subheadingtitle>
                    <subheadingtitlebox>
                        <captiontext align='left'>ステージとは1みたいなこの文章が十分に長くて2行に渡る場合、下につくコンテンツはずれなければいけない。そのズレをまず作成してみよう</captiontext>
                    </subheadingtitlebox>
                </subheadingtitle>
                <subheadingdescbox>
                    <p align='left'>
                        aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
                        aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
                    </p>
                    <p align='left'>
                        caaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
                        aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
                    </p>
                </subheadingdescbox>
                <subheadingdescbox>
                    <p align='left'>
                        baaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
                        aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
                    </p>
                </subheadingdescbox>
            </subheading>

            <subheading>
                <subheadingtitle>
                    <subheadingtitlebox>
                        <captiontext align='left'>ステージとは2</captiontext>
                    </subheadingtitlebox>
                </subheadingtitle>
                <subheadingdescbox>
                    <p align='left'>
                        bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb
                        bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb
                    </p>
                </subheadingdescbox>
            </subheading>
        </subheadingcontainer>
    </bg>
    <!-- 
    <bg>
        <subheadingcontainer hidden='true' listen='stage'>
        </subheadingcontainer>
    </bg> -->

</body>";
        var done = false;

        eventReceiverGameObj.GetComponent<TestReceiver>().OnLoaded = ids =>
        {
            done = true;
        };
        view = UUebView.UUebViewComponent.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(viewWidth, 100));

        Show(view);

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("too late."); }, 50
        );
    }
}