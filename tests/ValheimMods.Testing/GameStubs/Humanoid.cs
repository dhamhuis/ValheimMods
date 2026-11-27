using System.Collections.Generic;

public class Humanoid
{
    public ItemDrop.ItemData RightItem { get; set; }

    public string LastMessage { get; set; }

    public List<string> Messages { get; } = new();

    public ItemDrop.ItemData GetRightItem() => RightItem;

    public void Message(MessageHud.MessageType type, string message, int amount, object icon, bool sound)
    {
        LastMessage = message;
        Messages.Add(message);
    }
}