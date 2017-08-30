using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UUebView {
    /**
		UUebView component.

		testing usage:
			attach this component to gameobject and set preset urls and event receiver.

		actual usage:
			let's use UUebView.GenerateSingleViewFromHTML or UUebView.GenerateSingleViewFromUrl.
	 */
	public class UUebViewComponent : MonoBehaviour, IUUebView {
		/*
			preset parameters.
			you can use this UUebView with preset paramters for testing.
		 */
		public string presetUrl;
		public GameObject presetEventReceiver;


		public UUebViewCore Core {
			get; private set;
		}

		void Start () {
			if (!string.IsNullOrEmpty(presetUrl) && presetEventReceiver != null) {
				Debug.Log("show preset view.");
				var view = UUebViewComponent.GenerateSingleViewFromUrl(presetEventReceiver, presetUrl, GetComponent<RectTransform>().sizeDelta);
				view.transform.SetParent(this.transform, false);
			}
		}

		public static GameObject GenerateSingleViewFromHTML(
            GameObject eventReceiverGameObj, 
            string source, 
            Vector2 viewRect, 
            Autoya.HttpRequestHeaderDelegate requestHeader=null,
            Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate=null,
            string viewName=ConstSettings.ROOTVIEW_NAME
        ) {
            var viewObj = new GameObject("UUebView");
            viewObj.AddComponent<RectTransform>();
			viewObj.name = viewName;

            var uuebView = viewObj.AddComponent<UUebViewComponent>();
			var uuebViewCore = new UUebViewCore(uuebView, requestHeader, httpResponseHandlingDelegate);
			uuebView.SetCore(uuebViewCore);
            uuebViewCore.LoadHtml(source, viewRect, eventReceiverGameObj);

            return viewObj;
        }

        public static GameObject GenerateSingleViewFromUrl(
            GameObject eventReceiverGameObj, 
            string url, 
            Vector2 viewRect, 
            Autoya.HttpRequestHeaderDelegate requestHeader=null,
            Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate=null,
            string viewName=ConstSettings.ROOTVIEW_NAME
        ) {
            var viewObj = new GameObject("UUebView");
            viewObj.AddComponent<RectTransform>();
			viewObj.name = viewName;
			
            var uuebView = viewObj.AddComponent<UUebViewComponent>();
            var uuebViewCore = new UUebViewCore(uuebView, requestHeader, httpResponseHandlingDelegate);
			uuebView.SetCore(uuebViewCore);
            uuebViewCore.DownloadHtml(url, viewRect, eventReceiverGameObj);

            return viewObj;
        }

        public void SetCore (UUebViewCore core) {
            this.Core = core;
        }

		void Update () {
			Core.Dequeue(this);
		}

        public void EmitButtonEventById (string elementId) {
            Core.OnImageTapped(elementId);
        }

		public void EmitLinkEventById (string elementId) {
            Core.OnLinkTapped(elementId);
        }

        void IUUebView.AddChild (Transform transform) {
            transform.SetParent(this.transform);
        }

        void IUUebView.UpdateSize (Vector2 size) {
            var parentRectTrans = this.transform.parent.GetComponent<RectTransform>();
			parentRectTrans.sizeDelta = size;
        }

        GameObject IUUebView.GetGameObject () {
            return this.gameObject;
        }

        void IUUebView.StartCoroutine (IEnumerator iEnum) {
            this.StartCoroutine(iEnum);
        }
    }
}