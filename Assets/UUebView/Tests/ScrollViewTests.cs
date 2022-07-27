
using System;
using System.Collections;
using System.Linq;
using Miyamasu;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

    public static void SetUUebViewOnScrollView(GameObject scrollView, string html, float offsetY, string viewName = ConstSettings.ROOTVIEW_NAME)
    {
        var eventReceiverCandidate = scrollView.GetComponents<Component>().Where(component => component is IUUebViewEventHandler).FirstOrDefault();
        if (eventReceiverCandidate == null)
        {
            throw new Exception("information scroll view should have IUUebViewEventHandler implemented component.");
        }

        var scrollRect = scrollView.GetComponent<ScrollRect>();
        var content = scrollRect.content;

        // 完了するまで見えない
        scrollView.GetComponent<CanvasGroup>().alpha = 0;
        var viewSize = scrollView.GetComponent<RectTransform>().sizeDelta;

        var view = UUebViewComponent.GenerateSingleViewFromHTML(scrollView, html, viewSize, null, null, viewName);

        // scrollEventに対してのハンドラをセットする。
        // var uuebViewComponent = view.GetComponent<UUebViewComponent>();
        // var scrollEvent = uuebViewComponent.GetScrollEvent();

        // scrollRect.onValueChanged.AddListener(
        //     (v) =>
        //     {
        //         scrollEvent(content.anchoredPosition.y);
        //     }
        // );

        view.transform.SetParent(content.gameObject.transform, false);
    }

    private void Scroll(GameObject scrollView, float scrollAdd)
    {
        var content = scrollView.GetComponentsInChildren<RectTransform>().Where(t => t.gameObject.name == "Content").FirstOrDefault();
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, content.anchoredPosition.y + scrollAdd);
    }


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

        SetUUebViewOnScrollView(targetScrollView, source, 0f);

        yield return WaitUntil(() => done, () => { throw new TimeoutException("timeout."); }, 0.5);
    }

    [MTest]
    public IEnumerator L6_FullFirstView()
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

        SetUUebViewOnScrollView(targetScrollView, source, 0f);
        yield return WaitUntil(() => done, () => { throw new TimeoutException("timeout."); }, 0.5);
    }

    [MTest]
    public IEnumerator L10()
    {
        // 7まで映る。で、ここから先はイベントの取得と、レンジの移動。
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

        SetUUebViewOnScrollView(targetScrollView, source, 0f);
        yield return WaitUntil(() => done, () => { throw new TimeoutException("timeout."); }, 0.5);
    }

    [MTest]
    public IEnumerator L50TimeAttack()
    {
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
        <body>something10</body><br>
        <body>something11</body><br>
        <body>something12</body><br>
        <body>something13</body><br>
        <body>something14</body><br>
        <body>something15</body><br>
        <body>something16</body><br>
        <body>something17</body><br>
        <body>something18</body><br>
        <body>something19</body><br>
        <body>something20</body><br>
        <body>something21</body><br>
        <body>something22</body><br>
        <body>something23</body><br>
        <body>something24</body><br>
        <body>something25</body><br>
        <body>something26</body><br>
        <body>something27</body><br>
        <body>something28</body><br>
        <body>something29</body><br>
        <body>something30</body><br>
        <body>something31</body><br>
        <body>something32</body><br>
        <body>something33</body><br>
        <body>something34</body><br>
        <body>something35</body><br>
        <body>something36</body><br>
        <body>something37</body><br>
        <body>something38</body><br>
        <body>something39</body><br>
        <body>something40</body><br>
        <body>something41</body><br>
        <body>something42</body><br>
        <body>something43</body><br>
        <body>something44</body><br>
        <body>something45</body><br>
        <body>something46</body><br>
        <body>something47</body><br>
        <body>something48</body><br>
        <body>something49</body><br>
        <body>something50</body><br>
        ";

        var done = false;

        var eventReceiverGameObj = targetScrollView.GetComponent<TestReceiver>();
        eventReceiverGameObj.OnLoaded = ids =>
        {
            targetScrollView.GetComponent<CanvasGroup>().alpha = 1;
            done = true;
        };

        SetUUebViewOnScrollView(targetScrollView, source, 0f);
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        yield return WaitUntil(() => done, () => { throw new TimeoutException("timeout."); });
        sw.Stop();
        Debug.Log("L50TimeAttack sw:" + sw.ElapsedMilliseconds);
    }



    [MTest]
    public IEnumerator L500TimeAttack()
    {
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
        <body>something10</body><br>
        <body>something11</body><br>
        <body>something12</body><br>
        <body>something13</body><br>
        <body>something14</body><br>
        <body>something15</body><br>
        <body>something16</body><br>
        <body>something17</body><br>
        <body>something18</body><br>
        <body>something19</body><br>
        <body>something20</body><br>
        <body>something21</body><br>
        <body>something22</body><br>
        <body>something23</body><br>
        <body>something24</body><br>
        <body>something25</body><br>
        <body>something26</body><br>
        <body>something27</body><br>
        <body>something28</body><br>
        <body>something29</body><br>
        <body>something30</body><br>
        <body>something31</body><br>
        <body>something32</body><br>
        <body>something33</body><br>
        <body>something34</body><br>
        <body>something35</body><br>
        <body>something36</body><br>
        <body>something37</body><br>
        <body>something38</body><br>
        <body>something39</body><br>
        <body>something40</body><br>
        <body>something41</body><br>
        <body>something42</body><br>
        <body>something43</body><br>
        <body>something44</body><br>
        <body>something45</body><br>
        <body>something46</body><br>
        <body>something47</body><br>
        <body>something48</body><br>
        <body>something49</body><br>
        <body>something50</body><br>
        <body>something</body><br>
        <body>something2</body><br>
        <body>something3</body><br>
        <body>something4</body><br>
        <body>something5</body><br>
        <body>something6</body><br>
        <body>something7</body><br>
        <body>something8</body><br>
        <body>something9</body><br>
        <body>something10</body><br>
        <body>something11</body><br>
        <body>something12</body><br>
        <body>something13</body><br>
        <body>something14</body><br>
        <body>something15</body><br>
        <body>something16</body><br>
        <body>something17</body><br>
        <body>something18</body><br>
        <body>something19</body><br>
        <body>something20</body><br>
        <body>something21</body><br>
        <body>something22</body><br>
        <body>something23</body><br>
        <body>something24</body><br>
        <body>something25</body><br>
        <body>something26</body><br>
        <body>something27</body><br>
        <body>something28</body><br>
        <body>something29</body><br>
        <body>something30</body><br>
        <body>something31</body><br>
        <body>something32</body><br>
        <body>something33</body><br>
        <body>something34</body><br>
        <body>something35</body><br>
        <body>something36</body><br>
        <body>something37</body><br>
        <body>something38</body><br>
        <body>something39</body><br>
        <body>something40</body><br>
        <body>something41</body><br>
        <body>something42</body><br>
        <body>something43</body><br>
        <body>something44</body><br>
        <body>something45</body><br>
        <body>something46</body><br>
        <body>something47</body><br>
        <body>something48</body><br>
        <body>something49</body><br>
        <body>something50</body><br>
        <body>something</body><br>
        <body>something2</body><br>
        <body>something3</body><br>
        <body>something4</body><br>
        <body>something5</body><br>
        <body>something6</body><br>
        <body>something7</body><br>
        <body>something8</body><br>
        <body>something9</body><br>
        <body>something10</body><br>
        <body>something11</body><br>
        <body>something12</body><br>
        <body>something13</body><br>
        <body>something14</body><br>
        <body>something15</body><br>
        <body>something16</body><br>
        <body>something17</body><br>
        <body>something18</body><br>
        <body>something19</body><br>
        <body>something20</body><br>
        <body>something21</body><br>
        <body>something22</body><br>
        <body>something23</body><br>
        <body>something24</body><br>
        <body>something25</body><br>
        <body>something26</body><br>
        <body>something27</body><br>
        <body>something28</body><br>
        <body>something29</body><br>
        <body>something30</body><br>
        <body>something31</body><br>
        <body>something32</body><br>
        <body>something33</body><br>
        <body>something34</body><br>
        <body>something35</body><br>
        <body>something36</body><br>
        <body>something37</body><br>
        <body>something38</body><br>
        <body>something39</body><br>
        <body>something40</body><br>
        <body>something41</body><br>
        <body>something42</body><br>
        <body>something43</body><br>
        <body>something44</body><br>
        <body>something45</body><br>
        <body>something46</body><br>
        <body>something47</body><br>
        <body>something48</body><br>
        <body>something49</body><br>
        <body>something50</body><br>
        <body>something</body><br>
        <body>something2</body><br>
        <body>something3</body><br>
        <body>something4</body><br>
        <body>something5</body><br>
        <body>something6</body><br>
        <body>something7</body><br>
        <body>something8</body><br>
        <body>something9</body><br>
        <body>something10</body><br>
        <body>something11</body><br>
        <body>something12</body><br>
        <body>something13</body><br>
        <body>something14</body><br>
        <body>something15</body><br>
        <body>something16</body><br>
        <body>something17</body><br>
        <body>something18</body><br>
        <body>something19</body><br>
        <body>something20</body><br>
        <body>something21</body><br>
        <body>something22</body><br>
        <body>something23</body><br>
        <body>something24</body><br>
        <body>something25</body><br>
        <body>something26</body><br>
        <body>something27</body><br>
        <body>something28</body><br>
        <body>something29</body><br>
        <body>something30</body><br>
        <body>something31</body><br>
        <body>something32</body><br>
        <body>something33</body><br>
        <body>something34</body><br>
        <body>something35</body><br>
        <body>something36</body><br>
        <body>something37</body><br>
        <body>something38</body><br>
        <body>something39</body><br>
        <body>something40</body><br>
        <body>something41</body><br>
        <body>something42</body><br>
        <body>something43</body><br>
        <body>something44</body><br>
        <body>something45</body><br>
        <body>something46</body><br>
        <body>something47</body><br>
        <body>something48</body><br>
        <body>something49</body><br>
        <body>something50</body><br>
        <body>something</body><br>
        <body>something2</body><br>
        <body>something3</body><br>
        <body>something4</body><br>
        <body>something5</body><br>
        <body>something6</body><br>
        <body>something7</body><br>
        <body>something8</body><br>
        <body>something9</body><br>
        <body>something10</body><br>
        <body>something11</body><br>
        <body>something12</body><br>
        <body>something13</body><br>
        <body>something14</body><br>
        <body>something15</body><br>
        <body>something16</body><br>
        <body>something17</body><br>
        <body>something18</body><br>
        <body>something19</body><br>
        <body>something20</body><br>
        <body>something21</body><br>
        <body>something22</body><br>
        <body>something23</body><br>
        <body>something24</body><br>
        <body>something25</body><br>
        <body>something26</body><br>
        <body>something27</body><br>
        <body>something28</body><br>
        <body>something29</body><br>
        <body>something30</body><br>
        <body>something31</body><br>
        <body>something32</body><br>
        <body>something33</body><br>
        <body>something34</body><br>
        <body>something35</body><br>
        <body>something36</body><br>
        <body>something37</body><br>
        <body>something38</body><br>
        <body>something39</body><br>
        <body>something40</body><br>
        <body>something41</body><br>
        <body>something42</body><br>
        <body>something43</body><br>
        <body>something44</body><br>
        <body>something45</body><br>
        <body>something46</body><br>
        <body>something47</body><br>
        <body>something48</body><br>
        <body>something49</body><br>
        <body>something50</body><br>
        <body>something</body><br>
        <body>something2</body><br>
        <body>something3</body><br>
        <body>something4</body><br>
        <body>something5</body><br>
        <body>something6</body><br>
        <body>something7</body><br>
        <body>something8</body><br>
        <body>something9</body><br>
        <body>something10</body><br>
        <body>something11</body><br>
        <body>something12</body><br>
        <body>something13</body><br>
        <body>something14</body><br>
        <body>something15</body><br>
        <body>something16</body><br>
        <body>something17</body><br>
        <body>something18</body><br>
        <body>something19</body><br>
        <body>something20</body><br>
        <body>something21</body><br>
        <body>something22</body><br>
        <body>something23</body><br>
        <body>something24</body><br>
        <body>something25</body><br>
        <body>something26</body><br>
        <body>something27</body><br>
        <body>something28</body><br>
        <body>something29</body><br>
        <body>something30</body><br>
        <body>something31</body><br>
        <body>something32</body><br>
        <body>something33</body><br>
        <body>something34</body><br>
        <body>something35</body><br>
        <body>something36</body><br>
        <body>something37</body><br>
        <body>something38</body><br>
        <body>something39</body><br>
        <body>something40</body><br>
        <body>something41</body><br>
        <body>something42</body><br>
        <body>something43</body><br>
        <body>something44</body><br>
        <body>something45</body><br>
        <body>something46</body><br>
        <body>something47</body><br>
        <body>something48</body><br>
        <body>something49</body><br>
        <body>something50</body><br>
        <body>something</body><br>
        <body>something2</body><br>
        <body>something3</body><br>
        <body>something4</body><br>
        <body>something5</body><br>
        <body>something6</body><br>
        <body>something7</body><br>
        <body>something8</body><br>
        <body>something9</body><br>
        <body>something10</body><br>
        <body>something11</body><br>
        <body>something12</body><br>
        <body>something13</body><br>
        <body>something14</body><br>
        <body>something15</body><br>
        <body>something16</body><br>
        <body>something17</body><br>
        <body>something18</body><br>
        <body>something19</body><br>
        <body>something20</body><br>
        <body>something21</body><br>
        <body>something22</body><br>
        <body>something23</body><br>
        <body>something24</body><br>
        <body>something25</body><br>
        <body>something26</body><br>
        <body>something27</body><br>
        <body>something28</body><br>
        <body>something29</body><br>
        <body>something30</body><br>
        <body>something31</body><br>
        <body>something32</body><br>
        <body>something33</body><br>
        <body>something34</body><br>
        <body>something35</body><br>
        <body>something36</body><br>
        <body>something37</body><br>
        <body>something38</body><br>
        <body>something39</body><br>
        <body>something40</body><br>
        <body>something41</body><br>
        <body>something42</body><br>
        <body>something43</body><br>
        <body>something44</body><br>
        <body>something45</body><br>
        <body>something46</body><br>
        <body>something47</body><br>
        <body>something48</body><br>
        <body>something49</body><br>
        <body>something50</body><br>
        <body>something</body><br>
        <body>something2</body><br>
        <body>something3</body><br>
        <body>something4</body><br>
        <body>something5</body><br>
        <body>something6</body><br>
        <body>something7</body><br>
        <body>something8</body><br>
        <body>something9</body><br>
        <body>something10</body><br>
        <body>something11</body><br>
        <body>something12</body><br>
        <body>something13</body><br>
        <body>something14</body><br>
        <body>something15</body><br>
        <body>something16</body><br>
        <body>something17</body><br>
        <body>something18</body><br>
        <body>something19</body><br>
        <body>something20</body><br>
        <body>something21</body><br>
        <body>something22</body><br>
        <body>something23</body><br>
        <body>something24</body><br>
        <body>something25</body><br>
        <body>something26</body><br>
        <body>something27</body><br>
        <body>something28</body><br>
        <body>something29</body><br>
        <body>something30</body><br>
        <body>something31</body><br>
        <body>something32</body><br>
        <body>something33</body><br>
        <body>something34</body><br>
        <body>something35</body><br>
        <body>something36</body><br>
        <body>something37</body><br>
        <body>something38</body><br>
        <body>something39</body><br>
        <body>something40</body><br>
        <body>something41</body><br>
        <body>something42</body><br>
        <body>something43</body><br>
        <body>something44</body><br>
        <body>something45</body><br>
        <body>something46</body><br>
        <body>something47</body><br>
        <body>something48</body><br>
        <body>something49</body><br>
        <body>something50</body><br>
        <body>something</body><br>
        <body>something2</body><br>
        <body>something3</body><br>
        <body>something4</body><br>
        <body>something5</body><br>
        <body>something6</body><br>
        <body>something7</body><br>
        <body>something8</body><br>
        <body>something9</body><br>
        <body>something10</body><br>
        <body>something11</body><br>
        <body>something12</body><br>
        <body>something13</body><br>
        <body>something14</body><br>
        <body>something15</body><br>
        <body>something16</body><br>
        <body>something17</body><br>
        <body>something18</body><br>
        <body>something19</body><br>
        <body>something20</body><br>
        <body>something21</body><br>
        <body>something22</body><br>
        <body>something23</body><br>
        <body>something24</body><br>
        <body>something25</body><br>
        <body>something26</body><br>
        <body>something27</body><br>
        <body>something28</body><br>
        <body>something29</body><br>
        <body>something30</body><br>
        <body>something31</body><br>
        <body>something32</body><br>
        <body>something33</body><br>
        <body>something34</body><br>
        <body>something35</body><br>
        <body>something36</body><br>
        <body>something37</body><br>
        <body>something38</body><br>
        <body>something39</body><br>
        <body>something40</body><br>
        <body>something41</body><br>
        <body>something42</body><br>
        <body>something43</body><br>
        <body>something44</body><br>
        <body>something45</body><br>
        <body>something46</body><br>
        <body>something47</body><br>
        <body>something48</body><br>
        <body>something49</body><br>
        <body>something50</body><br>
        ";

        var done = false;

        var eventReceiverGameObj = targetScrollView.GetComponent<TestReceiver>();
        eventReceiverGameObj.OnLoaded = ids =>
        {
            targetScrollView.GetComponent<CanvasGroup>().alpha = 1;
            done = true;
        };

        SetUUebViewOnScrollView(targetScrollView, source, 0f);
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        yield return WaitUntil(() => done, () => { throw new TimeoutException("timeout."); });
        sw.Stop();
        Debug.Log("L500TimeAttack sw:" + sw.ElapsedMilliseconds);

        // 表示が終わったので、スクロールを行う。
        Scroll(targetScrollView, 16f);// 1行ぶんの高さ
    }

}