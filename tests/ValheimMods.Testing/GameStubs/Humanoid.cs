public class Humanoid
{
    public string LastMessage { get; set; }

    public void Message(MessageHud.MessageType type, string message, int amount, object icon, bool sound) => LastMessage = message;
}