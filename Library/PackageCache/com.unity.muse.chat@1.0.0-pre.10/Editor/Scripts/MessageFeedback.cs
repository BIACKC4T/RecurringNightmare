using Unity.Muse.Chat.Model;

namespace Unity.Muse.Chat
{
    internal struct MessageFeedback
    {
        public MuseMessageId MessageId;
        public bool FlagInappropriate;
        public Category Type;
        public string Message;
        public Sentiment Sentiment;
    }
}
