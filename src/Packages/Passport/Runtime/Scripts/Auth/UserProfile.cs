namespace Immutable.Passport.Auth {
    public class UserProfile {
        public string? email;
        public string? nickname;
        public string sub;

        public UserProfile(string? email, string? nickname, string sub) {
            this.email = email;
            this.nickname = nickname;
            this.sub = sub;
        }
    }
}