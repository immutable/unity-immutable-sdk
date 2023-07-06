using UnityEngine;
using Immutable.Passport.Utility;
using Newtonsoft.Json;

namespace Immutable.Passport.Auth
{
    public class User
    {
        public string idToken;
        public string accessToken;
        public string? refreshToken;

        public UserProfile? profile;
        public UserImx? imx;

        public User(string idToken, string accessToken, string? refreshToken)
        {
            this.idToken = idToken;
            this.accessToken = accessToken;
            this.refreshToken = refreshToken;

            // Get values from id token
            string? idTokenJson = JwtUtility.DecodeJwt(idToken);
            if (idTokenJson != null)
            {
                IdTokenPayload? idTokenPayload = JsonConvert.DeserializeObject<IdTokenPayload>(idTokenJson);
                profile = new UserProfile(idTokenPayload?.email, idTokenPayload?.nickname, idTokenPayload?.sub);
                imx = new UserImx()
                {
                    ethAddress = idTokenPayload?.passport?.imx_eth_address,
                    starkAddress = idTokenPayload?.passport?.imx_stark_address,
                    userAdminAddress = idTokenPayload?.passport?.imx_user_admin_address
                };
            }
        }

        public bool MetadatExists()
        {
            return imx?.ethAddress != null && imx?.starkAddress != null && imx?.userAdminAddress != null;
        }
    }

    public class UserImx
    {
        public string? ethAddress;
        public string? starkAddress;
        public string? userAdminAddress;
    }

    public static class UserExtensions
    {
        public static User ToUser(this TokenResponse response)
        {
            return new User(
                response.id_token,
                response.access_token,
                response.refresh_token
            );
        }
    }
}