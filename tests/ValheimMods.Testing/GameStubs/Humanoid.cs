using System.Collections.Generic;

public class Humanoid
{
    public string LastMessage { get; set; }

    public List<string> Messages { get; } = new();

    public void Message(MessageHud.MessageType type, string message, int amount, object icon, bool sound)
    {
        LastMessage = message;
        Messages.Add(message);
    }
}