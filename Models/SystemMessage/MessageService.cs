using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;

namespace Web0524.Models.SystemMessage
{
    public interface IMessageService
    {
        void SendMessage(string userId, string title, string content, MessageType type, TimeSpan? expireAfter = null);
        IEnumerable<Message> GetUserMessages(string userId, bool onlyUnread = false);
        void MarkAsRead(int messageId);
        void DeleteExpiredMessages();
        void SendMessageToAllUsers(string title, string content, MessageType type, TimeSpan? expireAfter = null);
        void MarkAllUserMessagesAsRead(string userId);
    }

    public class MessageService : IMessageService
    {
        private readonly IDbConnection _db;

        public MessageService(IDbConnection dbConnection)
        {
            _db = dbConnection;
        }

        public void SendMessage(string userId, string title, string content, MessageType type, TimeSpan? expireAfter = null)
        {
            var now = DateTime.UtcNow;
            var expireAt = expireAfter.HasValue ? now.Add(expireAfter.Value) : (DateTime?)null;

            string sql = @"
                INSERT INTO MessageTB (UserId, Title, Content, MessageType, IsRead, CreatedAt, ExpireAt)
                VALUES (@UserId, @Title, @Content, @MessageType, 0, @CreatedAt, @ExpireAt)";

            _db.Execute(sql, new
            {
                UserId = userId,
                Title = title,
                Content = content,
                MessageType = (int)type,
                CreatedAt = now,
                ExpireAt = expireAt
            });
        }

        public IEnumerable<Message> GetUserMessages(string userId, bool onlyUnread = false)
        {
            string sql = @"
                SELECT * FROM MessageTB
                WHERE UserId = @UserId
                  AND (ExpireAt IS NULL OR ExpireAt > @Now)";

            if (onlyUnread)
                sql += " AND IsRead = 0";

            return _db.Query<Message>(sql, new { UserId = userId, Now = DateTime.UtcNow });
        }

        public void MarkAsRead(int messageId)
        {
            string sql = "UPDATE MessageTB SET IsRead = 1 WHERE MessageId = @MessageId";
            _db.Execute(sql, new { MessageId = messageId });
        }

        public void DeleteExpiredMessages()
        {
            string sql = "DELETE FROM MessageTB WHERE ExpireAt IS NOT NULL AND ExpireAt <= @Now";
            _db.Execute(sql, new { Now = DateTime.UtcNow });
        }

        public void SendMessageToAllUsers(string title, string content, MessageType type, TimeSpan? expireAfter = null)
        {
            var now = DateTime.UtcNow;
            var expireAt = expireAfter.HasValue ? now.Add(expireAfter.Value) : (DateTime?)null;

            string getUserSql = "SELECT UserId FROM UserTB";
            var allUserIds = _db.Query<string>(getUserSql).ToList();

            string insertSql = @"
                INSERT INTO MessageTB (UserId, Title, Content, MessageType, IsRead, CreatedAt, ExpireAt)
                VALUES (@UserId, @Title, @Content, @MessageType, 0, @CreatedAt, @ExpireAt)";

            foreach (var userId in allUserIds)
            {
                _db.Execute(insertSql, new
                {
                    UserId = userId,
                    Title = title,
                    Content = content,
                    MessageType = (int)type,
                    CreatedAt = now,
                    ExpireAt = expireAt
                });
            }
        }

        public void MarkAllUserMessagesAsRead(string userId)
        {
            string sql = "UPDATE MessageTB SET IsRead = 1 WHERE UserId = @UserId AND IsRead = 0";
            _db.Execute(sql, new { UserId = userId });
        }
    }
}
