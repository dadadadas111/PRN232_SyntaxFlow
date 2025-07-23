namespace Models
{
    public class RequestEmailVerificationDto
    {
        public string Email { get; set; } = string.Empty;
    }
    public class VerifyOtpDto
    {
        public string Otp { get; set; } = string.Empty;
    }
    public class EmailVerificationResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? BanTtlSeconds { get; set; }
    }
    public class RequestPasswordResetDto
    {
        public string Email { get; set; } = string.Empty;
    }
    public class ResetPasswordDto
    {
        public string Otp { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
