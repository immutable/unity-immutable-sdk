// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using System;
using System.Collections.Generic;
using System.Linq;
using VoltstroStudios.UnityWebBrowser.Events;
using VoltstroStudios.UnityWebBrowser.Logging;
using VoltstroStudios.UnityWebBrowser.Shared.Popups;

namespace VoltstroStudios.UnityWebBrowser.Core.Popups
{
    internal class WebBrowserPopupService : IPopupEngineControls, IPopupClientControls
    {
        private readonly WebBrowserCommunicationsManager communicationsManager;

        public WebBrowserPopupService(WebBrowserCommunicationsManager communicationsManager)
        {
            this.communicationsManager = communicationsManager;
        }

        #region Engine

        public void OnPopup(Guid guid)
        {
        }

        public void OnPopupDestroy(Guid guid)
        {
        }

        #endregion

        #region Client

        public void PopupClose(Guid guid)
        {
        }

        public void PopupExecuteJs(Guid guid, string js)
        {
        }

        #endregion
    }
}