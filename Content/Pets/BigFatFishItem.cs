using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DeepFishAI.Content.Pets;

public class BigFatFishItem : ModItem
{
    public override string Texture => "DeepFishAI/Assets/BigFatFishItem";

    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.ZephyrFish);
        Item.shoot = ModContent.ProjectileType<BigFatFishPet>();
        Item.buffType = ModContent.BuffType<DeepFishPetBuff>();
        Item.maxStack = 1;
    }

    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer) player.AddBuff(Item.buffType, 18000);
        return true;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Fish, 3)
            .AddIngredient(ItemID.SoulofLight, 5)
            .AddTile(TileID.DemonAltar)
            .Register();
    }
}
