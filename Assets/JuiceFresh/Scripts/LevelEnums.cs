// Level description enums (read from the level files and the level editor). Moved here from InitScript.cs.

public enum Target
{
    SCORE,
    COLLECT,
    ITEMS,
    BLOCKS,
    CAGES,
    BOMBS,
}

public enum LIMIT
{
    MOVES,
    TIME
}

public enum Ingredients
{
    None = 0,
    Ingredient1,
    Ingredient2,
    Ingredient3,
    Ingredient4
}

public enum CollectItems
{
    None = 0,
    Item1,
    Item2,
    Item3,
    Item4,
    Item5,
    Item6
}

public enum CollectStars
{
    STAR_1 = 1,
    STARS_2 = 2,
    STARS_3 = 3
}
