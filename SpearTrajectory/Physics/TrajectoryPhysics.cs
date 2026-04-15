using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

public class TrajectoryPhysics
{
    //physics for each type of weapon known to man (I hope I don't have to add more else here)
    public double GravityPerSecond = GlobalConstants.GravityPerSecond * 0.75;
    public double AirDragValue = 1 - (1 - GlobalConstants.AirDragAlways) * 0.25;
    public float Velocity = 1f;
    public float DeltaTime = 1f / 60f;
    public int MaxSteps = 1000;
    public bool UseCOPhysics = true;

    public static TrajectoryPhysics For(Item item, bool isCOItem)
    {
        if (item is null) return new TrajectoryPhysics();

        return item switch
        {
            ItemBow => new TrajectoryPhysics // Vanilla Bow
            {
                Velocity = 1f,
                UseCOPhysics = false
            },
            ItemSpear => new TrajectoryPhysics // Vanilla Spear
            {
                Velocity = 0.65f,
                UseCOPhysics = false
            },
            _ when item is not ItemSpear && item.FirstCodePart(0) is "spear" => new TrajectoryPhysics // CO Spear
            {
                Velocity = 0.56f

            },
            _ when isCOItem && item.FirstCodePart(0) is "javelin" => new TrajectoryPhysics // CO Javelin
            {
                Velocity = 0.685f,
            },
            _ when item is not ItemBow && item.Code.SecondCodePart().Contains("crude") => new TrajectoryPhysics // CO Crude Bow
            {
                Velocity = 1.196f,
            },
            _ when item is not ItemBow && item.Code.SecondCodePart().Contains("simple") => new TrajectoryPhysics // CO Simple Bow
            {
                Velocity = 1.49f,
            },
            _ when item is not ItemBow && item.Code.SecondCodePart().Contains("long") => new TrajectoryPhysics // CO Long Bow
            {
                Velocity = 2.6f,
            },
            _ when item is not ItemBow && item.Code.SecondCodePart().Contains("recurve") => new TrajectoryPhysics // CO Recurve Bow
            {
                Velocity = 2f,
            },
            _ => new TrajectoryPhysics()
        };
    }
}