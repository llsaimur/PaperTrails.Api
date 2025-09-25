using Supabase.Gotrue;

namespace PaperTrails.Api.DTOs.UserAuthentication
{
    public class SupabaseResult
    {
        public User User { get; set; }
        public string AccessToken { get; set; }
        public long ExpiresIn { get; set; }
        public string Error { get; set; }
    }
}
