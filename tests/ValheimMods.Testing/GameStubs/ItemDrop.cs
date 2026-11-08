using UnityEngine;

public class ItemDrop
{
    public string name;
    public ItemData m_itemData = new();

    public class ItemData
    {
        public SharedData m_shared = new();
        public GameObject m_dropPrefab = new();
        public int m_stack = 1;
        public int m_quality = 1;
        public int m_worldLevel;
        public bool m_cheated;

        public class SharedData
        {
            public string m_name;
            public int m_maxQuality = 1;
        }
    }
}