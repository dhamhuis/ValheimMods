using System;

public class Piece
{
    public Requirement[] m_resources = [];

    public GlobalKeys FreeBuildKey() => GlobalKeys.NoCraftCost;

    public class Requirement
    {
        public ItemDrop m_resItem;
        public bool m_upgraderResource;
        public int m_amount;
        public int m_amountPerLevel;
        public int m_extraAmountOnlyOneIngredient;

        public int GetAmount(int qualityLevel)
        {
            if (qualityLevel <= 1)
            {
                return m_amount;
            }

            float quality = qualityLevel < 4 ? qualityLevel - 1 : 4 + ((qualityLevel - 4) / 2f);
            float amount = (quality * m_amountPerLevel) + (m_upgraderResource ? m_amount : 0);

            return (int)Math.Floor(amount);
        }
    }
}