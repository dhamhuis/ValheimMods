using System;

public class ZNetView
{
    public bool Owner { get; set; } = true;

    public bool Valid { get; set; } = true;

    public ZDO Zdo { get; set; } = new();

    public Action<string, object[]> OnRequest { get; set; }

    public bool IsValid() => Valid;

    public ZDO GetZDO() => Zdo;

    public void InvokeRPC(string method, params object[] args) => OnRequest?.Invoke(method, args);
}