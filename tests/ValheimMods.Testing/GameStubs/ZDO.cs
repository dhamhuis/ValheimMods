public class ZDO
{
    public int Busy { get; set; }

    public float Fuel { get; set; }

    public int GetInt(int key, int fallback) => key == ZDOVars.s_inUse ? Busy : fallback;

    public float GetFloat(int key, float fallback) => key == ZDOVars.s_fuel ? Fuel : fallback;

    public void Set(int key, float value)
    {
        if (key == ZDOVars.s_fuel)
        {
            Fuel = value;
        }
    }
}