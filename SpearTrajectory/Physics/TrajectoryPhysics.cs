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

    public static TrajectoryPhysics For(Item item, bool isCOItem)
    {
        if (item is null) return new TrajectoryPhysics();

        return item switch
        {
            ItemBow => new TrajectoryPhysics
            {
                GravityPerSecond = GlobalConstants.GravityPerSecond * 0.75,
                AirDragValue = 1 - (1 - GlobalConstants.AirDragAlways) * 0.25,
            },
            ItemSpear => new TrajectoryPhysics
            {
                Velocity = 0.62f,
                GravityPerSecond = GlobalConstants.GravityPerSecond * 0.75,
            },
            _ when isCOItem && item.FirstCodePart(0) is "spear" => new TrajectoryPhysics
            {
                Velocity = 0.56f,
                GravityPerSecond = GlobalConstants.GravityPerSecond * 0.75,
            },
            _ when isCOItem && item.FirstCodePart(0) is "javelin" => new TrajectoryPhysics
            {
                Velocity = 0.7f,
                GravityPerSecond = GlobalConstants.GravityPerSecond * 0.75,
            },
            _ when isCOItem && item.FirstCodePart(0) is "bow" => new TrajectoryPhysics
            {
                Velocity = 2.6f,
            },
            _ => new TrajectoryPhysics()
        };
    }
}