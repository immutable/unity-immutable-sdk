namespace Immutable.Passport.Event
{
    public static class PassportAnalytics
    {
        public const string TRACK = "track";
        public const string MODULE_NAME = "unitySdk";

        public static class EventName
        {
            public const string START_INIT_PASSPORT = "startedInitialisePassport";
            public const string INIT_PASSPORT = "initialisedPassport";
        }

        public static class Properties
        {
            public const string SUCCESS = "succeeded";
        }
    }
}