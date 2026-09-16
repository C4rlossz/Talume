namespace Talume.Web.Services;

public static class RegistrationPolicy
{
    public static bool CanRegisterDeveloper(string email, IConfiguration config, IWebHostEnvironment env) =>
        config.GetValue("Registration:AllowPublicFreelancers", env.IsDevelopment()) ||
        (config["Registration:AllowedEmails"] ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(email.Trim(), StringComparer.OrdinalIgnoreCase);
}
