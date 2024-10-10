var unityWebView =
    {
        loaded: [],

        init: function(name) {
            $containers = $('.webviewContainer');
            if ($containers.length === 0) {
                $('<div style="position: absolute; left: 0px; width: 100%; height: 100%; top: 0px; pointer-events: none;"><div class="webviewContainer" style="overflow: hidden; position: relative; width: 100%; height: 100%; z-index: 1;"></div></div>')
                    .appendTo($('#unity-container'));
            }
            var $last = $('.webviewContainer:last');
            var clonedTop = parseInt($last.css('top')) - 100;
            var $clone = $last.clone().insertAfter($last).css('top', clonedTop + '%');
            var $iframe =
                $('<iframe style="position:relative; width:100%; height100%; border-style:none; display:none; pointer-events:auto;"></iframe>')
                    .attr('id', 'webview_' + name)
                    .appendTo($last)
            $iframe.on('load', function () {
                $(this).attr('loaded', 'true');

                var js = `window.Unity = {
                            call:function(msg) {
                                        parent.unityWebView.sendMessage('${name}', msg);
                                    }
                            };`;

                this.contentWindow.eval(js);

                window.addEventListener('message', function(event) {
                    if ((event.data.type === 'callback' || event.data.type === 'logout') && event.isTrusted) {
                        unityInstance.SendMessage(name, 'CallOnAuth', event.data.url);
                    }
                }, false);
            });
            return $iframe;
        },

        sendMessage: function (name, message) {
            unityInstance.SendMessage(name, "CallFromJS", message);
        },

        loadURL: function(name, url) {
            var baseUrl = window.location.origin + window.location.pathname.replace(/\/[^\/]*$/, "");
            const host = window.location.origin.includes('localhost') ? window.location.origin : baseUrl;
            this.iframe(name).attr('loaded', 'false')[0].contentWindow.location.replace(host + url);
        },

        evaluateJS: function (name, js) {
            $iframe = this.iframe(name);
            if ($iframe.attr('loaded') === 'true') {
                $iframe[0].contentWindow.eval(js);
            } else {
                $iframe.on('load', function(){
                    $(this)[0].contentWindow.eval(js);
                });
            }
        },

        destroy: function (name) {
            this.iframe(name).parent().parent().remove();
        },

        iframe: function (name) {
            return $('#webview_' + name);
        },

        launchAuthURL: function(name, url) {
            window.open(url, '_blank', 'width=460,height=660');
        },
    };
