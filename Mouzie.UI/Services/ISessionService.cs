namespace Mouzie.UI.Services
{
    public interface ISessionService
    {
        bool IsAuthenticated { get; }
        int? UserId { get; }
        string? Username { get; }

        void SignIn(int userId, string username);
        void SignOut();
    }
}