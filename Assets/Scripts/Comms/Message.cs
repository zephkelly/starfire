namespace Starfire
{
    public class Message
    {
        public string Text { get; private set; }
        public Transponder Sender { get; private set; }
        public Transponder Recipient { get; private set; }
        public TransponderStatus Status { get; private set; }

        public Message(Transponder sender, Transponder recipient, string message)
        {
            Sender = sender;
            Recipient = recipient;
            Text = message;
            Status = sender.Status;
        }
    }
}