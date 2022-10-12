using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Vuplex.WebView;

class WebGLWebViewDemo : MonoBehaviour {

    public CanvasWebViewPrefab AddressBar;
    public CanvasWebViewPrefab MainWebView;

    [Serializable]
    class IFrameTestResult {
        public string status;
        public string message;
        public string url;
    }

    async void Start() {

        // Wait for the prefab to initialize because its WebView property is null until then.
        // https://developer.vuplex.com/webview/WebViewPrefab#WaitUntilInitialized
        await AddressBar.WaitUntilInitialized();

        // Listen for a message from the address bar webview indicating that a URL was submitted.
        // https://support.vuplex.com/articles/how-to-send-messages-from-javascript-to-c-sharp
        AddressBar.WebView.MessageEmitted += (sender, eventArgs) => {
            var message = eventArgs.Value;
            var messagePrefix = "url_submitted:";
            if (message.StartsWith(messagePrefix)) {
                var url = message.Substring(messagePrefix.Length);
                MainWebView.WebView.LoadUrl(url);
                StartCoroutine(_testIFrameUrl(url));
            }
        };

        // Wait for the address bar UI to load.
        // https://developer.vuplex.com/webview/IWebView#WaitForNextPageLoadToFinish
        await AddressBar.WebView.WaitForNextPageLoadToFinish();

        // Initialize the address bar with the initial URL.
        AddressBar.WebView.PostMessage($"set_url:" + MainWebView.InitialUrl);
    }

    IEnumerator _testIFrameUrl(string url) {

        // Only make the API request when running in the browser.
        #if UNITY_WEBGL && !UNITY_EDITOR
            // Note: this is an internal Vuplex API that only works from vuplex.com domains.
            var request = UnityWebRequest.Get($"https://api.vuplex.com/support/webview/webgl/iframe-test?url={UnityWebRequest.EscapeURL(url)}");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.ConnectionError) {
                yield break;
            }
            if (request.responseCode >= 400) {
                yield break;
            }
            var result = JsonUtility.FromJson<IFrameTestResult>(request.downloadHandler.text);
            if (result.status == "FAIL" || result.status == "WARN") {
                MainWebView.WebView.LoadHtml(@$"
                    <style>
                        html, body {{
                            /* Add a background because otherwise the iframe page load background will show. */
                            background-color: white;
                            font-family: sans-serif;
                        }}
                        p {{
                            padding: 2em 2em 0;
                        }}
                        .url {{
                            color: rgba(0, 0, 0, 0.5);
                        }}
                    </style>
                    <p>
                        {result.message.Replace("<", "&lt;").Replace(">", "&gt;")}
                        For more info, please see this article: <a href='https://support.vuplex.com/articles/webgl-limitations'>https://support.vuplex.com/articles/webgl-limitations</a>
                    </p>
                    <p class='url'>
                        URL: {result.url}
                    </p>
                ");
            }
        #else
            yield break;
        #endif
    }
}
