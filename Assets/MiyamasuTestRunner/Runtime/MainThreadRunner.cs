using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UUebView;

namespace Miyamasu {
	public class MainThreadRunner : MonoBehaviour, IUUebViewEventHandler {
		private int index = 0;
		private bool started;

		private UUebViewComponent targetComponent;

		private string htmlContent = @"
<!DOCTYPE uuebview href='resources://Views/Console/UUebTags'>
";

		IEnumerator Start () {
			while (iEnumGens == null) {
				// wait to set enumGens;
				yield return null;
			}
			
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

			var scrollViewRectCandidates = canvas.transform.GetComponentsInChildren<RectTransform>();
			GameObject attachTargetView = null;
			foreach (var rect in scrollViewRectCandidates) {
				if (rect.name == "Content") {
					attachTargetView = rect.gameObject;
					break;
				}
			}

			var scrollViewWidth = canvas.GetComponent<RectTransform>().sizeDelta.x;
			Debug.Log("fmm:" + scrollViewWidth);

			Recorder.logAct = this.AddLog;

			var view = UUebViewComponent.GenerateSingleViewFromHTML(this.gameObject, htmlContent, new Vector2(scrollViewWidth, 100));
			view.name = "MiyamasuRuntimeConsole";
			view.transform.SetParent(attachTargetView.transform, false);

			targetComponent = view.GetComponent<UUebViewComponent>();

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

					targetComponent.AppendContentToLast(message);
				}
			}
		}
		
		private Func<IEnumerator>[] iEnumGens;
		public void SequentialExecute (Func<IEnumerator>[] iEnumGens) {
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
			this method will be called from jumper lib via SendMessage.
		 */
		public void AddLog (string message, Recorder.ReportType type, Exception e) {
			var icon = "pass";

			switch (type) {
				case Recorder.ReportType.AssertionFailed: {
					icon = "fail";
					break;
				}
				default: {
					icon = "pass";
					break;
				}
				// case (int)LogType.Warning: {
				// 	logList.Add("<bg><textbg><p>" + message + "</p></textbg></bg><br>");
				// 	break;
				// }
				// case (int)LogType.Error: {
				// 	logList.Add("<bg><textbg><p>" + message + "</p></textbg></bg><br>");
				// 	break;
				// }
				// default: {
				// 	logList.Add("<bg><textbg><p>" + message + "</p></textbg></bg><br>");
				// 	break;
				// }
			}

			logList.Add("<bg><textbg><contenttext>" + message + "</contenttext></textbg><iconbg><" + icon + "/></iconbg></bg><br>");
		}

        void IUUebViewEventHandler.OnLoadStarted()
        {
			// throw new NotImplementedException();
        }

        void IUUebViewEventHandler.OnProgress(double progress)
        {
            // throw new NotImplementedException();
        }

        void IUUebViewEventHandler.OnLoaded()
        {
			loaded = true;

			if (logList.Any()) {
				loaded = false;

				var message = string.Join("", logList.ToArray());
				logList.Clear();

				targetComponent.AppendContentToLast(message);
			}

            // throw new NotImplementedException();
        }


        void IUUebViewEventHandler.OnUpdated()
        {
			loaded = true;
			if (logList.Any()) {
				loaded = false;

				var message = string.Join("", logList.ToArray());
				logList.Clear();

				targetComponent.AppendContentToLast(message);
			}

			// Debug.LogWarning("updated:" + logList.Count);
			// loaded = true;
            // throw new NotImplementedException();
        }

        void IUUebViewEventHandler.OnLoadFailed(ContentType type, int code, string reason)
        {
			Debug.LogError("loadFailed:" + type + " code:" + code + " reason:" + reason);
        }

        void IUUebViewEventHandler.OnElementTapped(ContentType type, GameObject element, string param, string id)
        {
            // throw new NotImplementedException();
        }

        void IUUebViewEventHandler.OnElementLongTapped(ContentType type, string param, string id)
        {
            // throw new NotImplementedException();
        }
    }
}