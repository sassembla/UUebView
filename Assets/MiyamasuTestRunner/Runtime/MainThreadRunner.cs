using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UUebView;

namespace Miyamasu {
	public class MainThreadRunner : MonoBehaviour, IUUebViewEventHandler {
		private int index = 0;
		private bool started;

		private UUebViewComponent currentUUebViewComponent;

		private string htmlContent = @"
<!DOCTYPE uuebview href='resources://Views/Console/UUebTags'>
";

		IEnumerator Start () {
			while (iEnumGens == null) {
				// wait to set enumGens;
				yield return null;
			}

			this.index = 0;
			var totalCount = iEnumGens.Length;
			
			// wait for check UnityTest is running or not.
			yield return new WaitForSeconds(1);
			
			if (Miyamasu.Recorder.isRunning) {
				Destroy(this);
				yield break;
			}

			var canvasCor = Resources.LoadAsync<GameObject>("MiyamasuPrefabs/MiyamasuCanvas");

			while (!canvasCor.isDone) {
				yield return null;
			}

			var canvasPrefab = canvasCor.asset as GameObject;
			var canvas = Instantiate(canvasPrefab);
			canvas.name = "MiyamasuCanvas";

			var scrollViewRectCandidates = canvas.transform.GetComponentsInChildren<RectTransform>();
			GameObject attachTargetView = null;
			foreach (var rect in scrollViewRectCandidates) {
				if (rect.name == "Content") {
					attachTargetView = rect.gameObject;
					break;
				}
			}

			var scrollViewWidth = canvas.GetComponent<RectTransform>().sizeDelta.x;
			Recorder.logAct = this.AddLog;

			var view = UUebViewComponent.GenerateSingleViewFromHTML(this.gameObject, htmlContent, new Vector2(scrollViewWidth, 100));
			view.name = "MiyamasuRuntimeConsole";
			view.transform.SetParent(attachTargetView.transform, false);

			currentUUebViewComponent = view.GetComponent<UUebViewComponent>();

			started = true;
			yield return ContCor();
		}


		void Update () {
			if (started && Recorder.isStoppedByFail) {
				Recorder.isStoppedByFail = false;

				// continue test.
				StartCoroutine(ContCor());
			}

			if (loaded) {
				if (logList.Any()) {
					loaded = false;

					var message = string.Join("", logList.ToArray());
					logList.Clear();

					currentUUebViewComponent.AppendContentToLast(message);
				}
			}
		}
		
		private Func<IEnumerator>[] iEnumGens;
		public void SetTests (Func<IEnumerator>[] iEnumGens) {
			this.iEnumGens = iEnumGens;
        }

		private IEnumerator ContCor () {
			while (index < iEnumGens.Length) {
				yield return iEnumGens[index++]();
			}

			Debug.Log("maybe all tests passed.");
		}
		
		private bool loaded;
		private List<string> logList = new List<string>();
		/**
			this method will be called from jumper lib.
		 */
		public void AddLog (string[] message, Recorder.ReportType type, Exception e) {
			var icon = "pass";

			switch (type) {
				case Recorder.ReportType.AssertionFailed: {
					icon = "fail";
					break;
				}
				case Recorder.ReportType.FailedByTimeout: {
					icon = "timeout";
					break;
				}
				case Recorder.ReportType.Error: {
					icon = "error";
					break;
				}
				case Recorder.ReportType.Passed: {
					icon = "pass";
					break;
				}
				default: {
					throw new Exception("まだ解決してないsetupとteardownエラー");
					break;
				}
			}

			var messageBlock = message[0] + " / " + message[1];
			if (2 < message.Length) {
				messageBlock += " line:" + message[2];
			}

			var error = string.Empty;
			if (e != null) {
				var id = Guid.NewGuid().ToString();
				error =  @" button='true' src='" + Base64Encode(e.ToString()) + @"' id='" + id + @"'";
			}
			
			logList.Add(@"
				<bg" + error + @">
					<textbg>
						<contenttext>" + messageBlock + @"</contenttext>
					</textbg>
					<iconbg><" + icon + @"/></iconbg>
				</bg><br>");

		}

		private static string Base64Encode(string plainText) {
			var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
			return Convert.ToBase64String(plainTextBytes);
		}

		private static string Base64Decode(string base64EncodedData) {
			var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
			return Encoding.UTF8.GetString(base64EncodedBytes);
		}

        void IUUebViewEventHandler.OnLoadStarted()
        {
			// throw new NotImplementedException();
        }

        void IUUebViewEventHandler.OnProgress(double progress)
        {
            // throw new NotImplementedException();
        }

        void IUUebViewEventHandler.OnLoaded(string[] treeIds)
        {
			loaded = true;

			if (logList.Any()) {
				loaded = false;

				var message = string.Join("", logList.ToArray());
				logList.Clear();

				currentUUebViewComponent.AppendContentToLast(message);
			}

			// ここで、idが入っているのはエラーのtreeで、idからy位置を取得し、スクロールバーの位置に表示する。あ、スクロールバー消したな。。
			// idは一意なので、idに対して最初の一つのみを扱う。
			foreach (var contentId in treeIds) {
				var yPos = currentUUebViewComponent.GetTreeById(contentId)[0].offsetY;
				Debug.Log("error yPos:" + yPos);
			}
        }


        void IUUebViewEventHandler.OnUpdated(string[] newTreeIds)
        {
			loaded = true;
			if (logList.Any()) {
				loaded = false;

				var message = string.Join("", logList.ToArray());
				logList.Clear();

				currentUUebViewComponent.AppendContentToLast(message);
			}

			// ここで、idが入っているのはエラーのtreeで、idからy位置を取得し、スクロールバーの位置に表示する。あ、スクロールバー消したな。。
			// idは一意なので、idに対して最初の一つのみを扱う。
			foreach (var contentId in newTreeIds) {
				var yPos = currentUUebViewComponent.GetTreeById(contentId)[0].offsetY;
				Debug.Log("error yPos:" + yPos);
			}
        }

        void IUUebViewEventHandler.OnLoadFailed(ContentType type, int code, string reason)
        {
			Debug.LogError("loadFailed:" + type + " code:" + code + " reason:" + reason);
        }

		private Text detailText;
        void IUUebViewEventHandler.OnElementTapped(ContentType type, GameObject element, string param, string id)
        {
			var e = Base64Decode(param);
			// で、エラー詳細を表示する。
			if (detailText == null) {
				detailText = GameObject.Find("MiyamasuCanvas/DetailBG/DetailText").GetComponent<Text>();
			}
			detailText.text = e;
        }

        void IUUebViewEventHandler.OnElementLongTapped(ContentType type, string param, string id)
        {
            // throw new NotImplementedException();
        }
    }
}