using Immutable.Passport;
using Immutable.Passport.Utility;
using Newtonsoft.Json;

namespace Immutable.Passport.Auth {
    public class User {
        public string idToken;
        public string accessToken;
        public string? refreshToken;

        public UserProfile? profile;

        public string? etherKey;
        public string? starkKey;
        public string? userAdminKey;

        public User(string idToken, string accessToken, string? refreshToken) {
            this.idToken = idToken;
            this.accessToken = accessToken;
            this.refreshToken = refreshToken;

            // Get values from id token
            string? idTokenJson = JwtUtility.decodeJwt(idToken);
            if (idTokenJson != null) {
                IdTokenPayload idTokenPayload = JsonConvert.DeserializeObject<IdTokenPayload>(idTokenJson);
                profile = new UserProfile(idTokenPayload.email, idTokenPayload.nickname, idTokenPayload.sub);
                etherKey = idTokenPayload.passport?.ether_key;
                starkKey = idTokenPayload.passport?.stark_key;
                userAdminKey = idTokenPayload.passport?.user_admin_key;
            }
        }

        public bool MetadatExists() {
            return etherKey != null && starkKey != null && userAdminKey != null;
        }
    }

    public static class UserExtensions {
        public static User ToUser(this TokenResponse response) {
            return new User(
                response.id_token,
                response.access_token,
                response.refresh_token
            );
        }
    }
}