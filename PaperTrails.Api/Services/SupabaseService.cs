using PaperTrails.Api.DTOs.UserAuthentication;
using Supabase;
using User = Supabase.Gotrue.User;

namespace PaperTrails.Api.Services
{
    public class SupabaseService
    {
        private readonly Client _supabaseClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseApiKey;
        private bool _initialized = false;
        private readonly object _lock = new();

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;
            }

            await _supabaseClient.InitializeAsync();

            lock (_lock)
            {
                _initialized = true;
            }
        }

        public SupabaseService(IConfiguration config)
        {
            _supabaseUrl = config["Supabase:Url"];
            _supabaseApiKey = config["Supabase:AnonKey"];

            _supabaseClient = new Client(_supabaseUrl, _supabaseApiKey);

            _supabaseClient.InitializeAsync().GetAwaiter().GetResult();
        }

        public async Task<SupabaseResult> SignUpAsync(string email, string password)
        {
            await EnsureInitializedAsync();

            try
            {
                var response = await _supabaseClient.Auth.SignUp(email, password);

                return new SupabaseResult
                {
                    User = response.User
                };
            }
            catch (Exception ex)
            {
                return new SupabaseResult
                {
                    Error = ex.Message
                };
            }
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            await EnsureInitializedAsync();
            return _supabaseClient.Auth.CurrentUser;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            await EnsureInitializedAsync();

            try
            {
                await _supabaseClient.Auth.ResetPasswordForEmail(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<SupabaseResult> LoginAsync(string email, string password)
        {
            await EnsureInitializedAsync();

            try
            {
                var response = await _supabaseClient.Auth.SignIn(email, password);

                return new SupabaseResult
                {
                    User = response.User,
                    AccessToken = response.AccessToken,
                    ExpiresIn = response.ExpiresIn
                };
            }
            catch (Exception ex)
            {
                return new SupabaseResult
                {
                    Error = ex.Message
                };
            }
        }
    }
}