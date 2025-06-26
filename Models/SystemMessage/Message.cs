namespace Web0524.Models.SystemMessage
{
    public enum MessageType
    {
        Warning = 1,
        System = 2,
        Store = 3
    }

    public class Message
    {
        public int MessageId { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public MessageType MessageType { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpireAt { get; set; }
    }

}
