using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Vuplex.WebView;
using Vuplex.WebView.Demos;

class WebGLWebViewDemo : MonoBehaviour {

    public CanvasWebViewPrefab AddressBar;
    public CanvasWebViewPrefab MainWebView;

    CanvasWebViewPrefab _focusedWebViewPrefab;
    HardwareKeyboardListener _hardwareKeyboardListener;

    [Serializable]
    class IFrameTestResult {
        public string status;
        public string message;
        public string url;
    }

    async void Start() {

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

        // Initialize the address bar with the initial URL.
        AddressBar.WebView.LoadProgressChanged += (sender, eventArgs) => {
            if (eventArgs.Type == ProgressChangeType.Finished) {
                AddressBar.WebView.PostMessage($"set_url:" + MainWebView.InitialUrl);
            }
        };

        _setUpHardwareKeyboard();
    }

    void _handleWebViewClicked(object sender, ClickedEventArgs args) {

        _focusedWebViewPrefab = (CanvasWebViewPrefab)sender;
    }

    // This code for setting up the hardware keyboard isn't needed for WebGL, but it
    // is needed for running in the editor.
    void _setUpHardwareKeyboard() {

        _focusedWebViewPrefab = MainWebView;
        MainWebView.Clicked += _handleWebViewClicked;
        AddressBar.Clicked += _handleWebViewClicked;
        _hardwareKeyboardListener = HardwareKeyboardListener.Instantiate();
        _hardwareKeyboardListener.KeyDownReceived += (sender, eventArgs) => {
            _focusedWebViewPrefab.WebView.HandleKeyboardInput(eventArgs.Value);
        };
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
