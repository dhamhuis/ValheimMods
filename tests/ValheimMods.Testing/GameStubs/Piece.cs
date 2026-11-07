public class Piece
{
    public class Requirement
    {
        public ItemDrop m_resItem;
        public bool m_upgraderResource;
        public int m_amount;
        public int m_amountPerLevel;
        public int m_extraAmountOnlyOneIngredient;

        public int GetAmount(int quality) => quality == 1 ? m_amount : m_amountPerLevel * (quality - 1);
    }
}