namespace Immutable.Audience.Samples.SampleApp
{
    // Sample-only event names that demonstrate the custom Track pattern.
    internal static class SampleAppCustomEvents
    {
        internal const string SignUp = "sign_up";
        internal const string SignIn = "sign_in";
        internal const string EmailAcquired = "email_acquired";
        internal const string WishlistAdd = "wishlist_add";
        internal const string WishlistRemove = "wishlist_remove";
        internal const string GamePageViewed = "game_page_viewed";
        internal const string LinkClicked = "link_clicked";
        internal const string ScreenViewed = "screen_viewed";
    }

    // Property keys for the sample-app demo events.
    internal static class SampleAppCustomEventPropertyKeys
    {
        internal const string Method = "method";
        internal const string Source = "source";
        internal const string GameId = "gameId";
        internal const string Platform = "platform";
        internal const string GameName = "gameName";
        internal const string Slug = "slug";
        internal const string Url = "url";
        internal const string Label = "label";
        internal const string Path = "path";
    }
}
