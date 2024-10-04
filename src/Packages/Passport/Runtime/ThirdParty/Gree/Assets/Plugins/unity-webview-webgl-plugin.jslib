mergeInto(LibraryManager.library, {
	_gree_unity_webview_init: function(name) {
		var stringify = (UTF8ToString === undefined) ? Pointer_stringify : UTF8ToString;
		unityWebView.init(stringify(name));
	},

	_gree_unity_webview_loadURL: function(name, url) {
		var stringify = (UTF8ToString === undefined) ? Pointer_stringify : UTF8ToString;
		unityWebView.loadURL(stringify(name), stringify(url));
	},

	_gree_unity_webview_evaluateJS: function(name, js) {
		var stringify = (UTF8ToString === undefined) ? Pointer_stringify : UTF8ToString;
		unityWebView.evaluateJS(stringify(name), stringify(js));
	},

	_gree_unity_webview_destroy: function(name) {
		var stringify = (UTF8ToString === undefined) ? Pointer_stringify : UTF8ToString;
		unityWebView.destroy(stringify(name));
	},
	
	_gree_unity_webview_launchAuthURL: function(name, url) {
        var stringify = (UTF8ToString === undefined) ? Pointer_stringify : UTF8ToString;
        unityWebView.launchAuthURL(stringify(name), stringify(url));
    },
});