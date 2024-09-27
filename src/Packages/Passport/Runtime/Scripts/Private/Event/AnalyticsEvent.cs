using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immutable.Passport.Core;
using Immutable.Passport.Helpers;
using Immutable.Passport.Model;
using UnityEngine;

namespace Immutable.Passport.Event
{
    public class PassportAnalytics
    {
        public const string TRACK = "track";
        public const string MODULE_NAME = "unitySdk";

        public async UniTask Track(IBrowserCommunicationsManager communicationsManager, string eventName,
            bool? success = null, Dictionary<string, object> properties = null)
        {
            try
            {
                if (properties == null) properties = new Dictionary<string, object>();
                if (success != null) properties.Add(Properties.SUCCESS, success);
                var json = JsonUtility.ToJson(new TrackData
                {
                    moduleName = MODULE_NAME,
                    eventName = eventName,
                    properties = properties.ToJson()
                });
                await communicationsManager.Call(TRACK, json);
            }
            catch (Exception)
            {
                // Ignore tracking errors
            }
        }

        public static class EventName
        {
            public const string INIT_PASSPORT = "initialisedPassport";

            // Login
            public const string START_LOGIN = "startedLogin";
            public const string COMPLETE_LOGIN = "performedLogin";
            public const string START_LOGIN_PKCE = "startedLoginPkce";
            public const string COMPLETE_LOGIN_PKCE = "performedLoginPkce";
            public const string COMPLETE_RELOGIN = "performedRelogin";

            // Connect
            public const string START_CONNECT_IMX = "startedConnectImx";
            public const string COMPLETE_CONNECT_IMX = "performedConnectImx";
            public const string START_CONNECT_IMX_PKCE = "startedConnectImxPkce";
            public const string COMPLETE_CONNECT_IMX_PKCE = "performedConnectImxPkce";
            public const string COMPLETE_RECONNECT = "performedReconnect";

            // Logout
            public const string COMPLETE_LOGOUT = "performedLogout";
            public const string COMPLETE_LOGOUT_PKCE = "performedLogoutPkce";
        }

        public static class Properties
        {
            public const string SUCCESS = "succeeded";
        }
    }
}