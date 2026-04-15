namespace EngagementTracker.Helpers;

public static class SessionHelper
{
    public static bool IsLoggedIn(ISession session) =>
        !string.IsNullOrEmpty(session.GetString("uid"));

    public static string GetRole(ISession session) =>
        session.GetString("role") ?? "";

    public static string GetName(ISession session) =>
        session.GetString("name") ?? "";

    public static string GetUid(ISession session) =>
        session.GetString("uid") ?? "";

    public static string GetSection(ISession session) =>
        session.GetString("section") ?? "";
}