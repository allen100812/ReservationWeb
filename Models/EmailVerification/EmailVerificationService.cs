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
    bool SendVerificationCode(string email);
    bool VerifyCode(string email, string code);
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

    public bool SendVerificationCode(string email)
    {
        if (!CanSendEmailThisMonth()) return false;

        var code = GenerateCode();
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        _dbConnection.Execute("DELETE FROM EmailSendLogTB WHERE Email = @Email", new { Email = email });

        _dbConnection.Execute(@"
            INSERT INTO EmailSendLogTB (Email, VerificationCode, SentAt, ExpiresAt, IsVerified)
            VALUES (@Email, @Code, GETDATE(), @ExpiresAt, 0)",
            new { Email = email, Code = code, ExpiresAt = expiresAt });

        var message = new MimeKit.MimeMessage();
        message.From.Add(new MimeKit.MailboxAddress("預約通知服務", smtpUser));
        message.To.Add(new MimeKit.MailboxAddress("", email));
        message.Subject = "【您的註冊驗證碼】請於 5 分鐘內完成驗證";
        message.Body = new MimeKit.TextPart("plain")
        {
            Text = $"您好，\n\n您的驗證碼為：{code}（有效 5 分鐘）\n請勿與他人分享此驗證碼。如非您本人操作，請忽略此郵件。\n\n---\n本信件為系統自動發送，請勿直接回覆。"
        };

        using var client = new MailKit.Net.Smtp.SmtpClient();
        try
        {
            client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
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
}