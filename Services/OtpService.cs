using System;
using System.Security.Cryptography;

namespace Services
{
    public static class OtpService
    {
        public static string GenerateOtp()
        {
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            int value = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF;
            return (value % 1000000).ToString("D6");
        }
    }
}
