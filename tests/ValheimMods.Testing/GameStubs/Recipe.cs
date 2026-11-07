public class Recipe
{
    public CraftingStation m_craftingStation = new();
    public bool m_requireOnlyOneIngredient;
    public Piece.Requirement[] m_resources = [];
}