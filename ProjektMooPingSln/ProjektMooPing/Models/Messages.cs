using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.Messaging.Messages;
using ProjektMooPing.Models;

namespace ProjektMooPing.Models
{
    public class NewRecipeMessage : ValueChangedMessage<Recipe>
    {
        public NewRecipeMessage(Recipe value) : base(value) { }
    }
    public class RecipeUpdatedMessage : ValueChangedMessage<Recipe>
    {
        public RecipeUpdatedMessage(Recipe value) : base(value) { }
    }
    public class RecipeDeletedMessage : ValueChangedMessage<Recipe>
    {
        public RecipeDeletedMessage(Recipe value) : base(value) { }
    }
    public class ResetGameMessage { }

    public class AddRatingMessage { public int Amount { get; init; } }

    public class AddMoneyMessage { public double Amount { get; init; } }

    public class IngredientUnlockedMessage : ValueChangedMessage<Ingredient>
    {
        public IngredientUnlockedMessage(Ingredient value) : base(value) { }
    }
}