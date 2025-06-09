// EmailVerificationService.cs
using System;
using System.Data;
using Dapper;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;


public interface IEmailVerificationService
{
    DateTime? GetLastSentTime(string email);
    bool SendVerificationCode(string email, string? ip);
    bool CanSendEmail(string email, string? ip);

    bool VerifyCode(string email, string code);
    bool CanSendEmailThisMonth();

    bool VerifyCodeAndUpdateOrders(string oldEmail, string newEmail, string code);
}

public class EmailVerificationService : IEmailVerificationService
{
    private readonly IDbConnection _dbConnection;
    private readonly string smtpUser = "allen100812@gmail.com";
    private readonly string smtpPass = "ozek myje gzfs ycvm"; // 請填入 Gmail 應用程式密碼

    public EmailVerificationService(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public DateTime? GetLastSentTime(string email)
    {
        return _dbConnection.QueryFirstOrDefault<DateTime?>(
            "SELECT TOP 1 SentAt FROM EmailSendLogTB WHERE Email = @Email ORDER BY SentAt DESC",
            new { Email = email });
    }

    public bool CanSendEmail(string email, string? ip)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var hourAgo = now.AddHours(-1);

        var emailCountToday = _dbConnection.ExecuteScalar<int>(
            @"SELECT COUNT(*) FROM EmailSendLogTB 
          WHERE Email = @Email AND SentAt >= @TodayStart",
            new { Email = email, TodayStart = todayStart });

        if (emailCountToday >= 5) return false;

        if (!string.IsNullOrWhiteSpace(ip))
        {
            var ipCountToday = _dbConnection.ExecuteScalar<int>(
                @"SELECT COUNT(*) FROM EmailSendLogTB 
              WHERE IPAddress = @IP AND SentAt >= @TodayStart",
                new { IP = ip, TodayStart = todayStart });

            var ipCountHour = _dbConnection.ExecuteScalar<int>(
                @"SELECT COUNT(*) FROM EmailSendLogTB 
              WHERE IPAddress = @IP AND SentAt >= @HourAgo",
                new { IP = ip, HourAgo = hourAgo });

            if (ipCountToday >= 20 || ipCountHour >= 10) return false;
        }

        return true;
    }

    public bool CanSendEmailThisMonth()
    {
        var now = DateTime.UtcNow;
        var firstDay = new DateTime(now.Year, now.Month, 1);
        var count = _dbConnection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM EmailSendLogTB WHERE SentAt >= @FirstDay",
            new { FirstDay = firstDay });

        return count < 300;
    }

    public string GenerateCode()
    {
        var rand = new Random();
        return rand.Next(100000, 999999).ToString();
    }

    public bool SendVerificationCode(string email, string? ip)
    {
        if (!CanSendEmail(email, ip)) return false;

        var code = GenerateCode();
        var sentAt = DateTime.UtcNow;
        var expiresAt = sentAt.AddMinutes(5);

        _dbConnection.Execute("DELETE FROM EmailSendLogTB WHERE SentAt < DATEADD(DAY, -7, GETUTCDATE())");

        _dbConnection.Execute(@"
        INSERT INTO EmailSendLogTB (Email, VerificationCode, SentAt, ExpiresAt, IsVerified, IPAddress)
        VALUES (@Email, @Code, @SentAt, @ExpiresAt, 0, @IP)",
            new { Email = email, Code = code, SentAt = sentAt, ExpiresAt = expiresAt, IP = ip ?? "-" });

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("預約通知服務", smtpUser));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = "【您的註冊驗證碼】請於 5 分鐘內完成驗證";
        message.Body = new TextPart("plain")
        {
            Text = $"您好，\n\n您的驗證碼為：{code}（有效 5 分鐘）\n請勿與他人分享此驗證碼。如非您本人操作，請忽略此郵件。\n\n---\n本信件為系統自動發送，請勿直接回覆。"
        };

        using var client = new SmtpClient();
        try
        {
            client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            client.Authenticate(smtpUser, smtpPass);
            client.Send(message);
            client.Disconnect(true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool VerifyCode(string email, string code)
    {
        var record = _dbConnection.QueryFirstOrDefault<dynamic>(@"
            SELECT TOP 1 VerificationCode, ExpiresAt
            FROM EmailSendLogTB
            WHERE Email = @Email
            ORDER BY SentAt DESC",
            new { Email = email });

        if (record == null) return false;

        var correctCode = (string)record.VerificationCode;
        var expiresAt = (DateTime)record.ExpiresAt;

        if (code != correctCode || DateTime.UtcNow > expiresAt)
            return false;

        _dbConnection.Execute("UPDATE EmailSendLogTB SET IsVerified = 1 WHERE Email = @Email", new { Email = email });
        return true;
    }


    /// <summary>
    /// 驗證驗證碼，成功時更新所有訂單中使用者 ID
    /// </summary>
    public bool VerifyCodeAndUpdateOrders(string oldEmail, string newEmail, string code)
    {
        var record = _dbConnection.QueryFirstOrDefault<dynamic>(@"
            SELECT TOP 1 VerificationCode, ExpiresAt
            FROM EmailSendLogTB
            WHERE Email = @Email
            ORDER BY SentAt DESC",
            new { Email = newEmail });

        if (record == null) return false;

        var correctCode = (string)record.VerificationCode;
        var expiresAt = (DateTime)record.ExpiresAt;

        if (code != correctCode || DateTime.UtcNow > expiresAt)
            return false;

        _dbConnection.Execute("UPDATE EmailSendLogTB SET IsVerified = 1 WHERE Email = @Email", new { Email = newEmail });


        return true;
    }
}