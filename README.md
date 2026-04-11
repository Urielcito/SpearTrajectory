# SpearTrajectory
- (Right now) removes all spread from using most ranged weapons (spears, javelins, bows)
- Draws the estimated trajectory and impact point of spears when you aim with them
- Draws a ghost trajectory when you are close to hitting an entity
- Turns the trajectory red when a collision with an entity is detected

## Config
- Trajectory color on entity collision
- Trajectory circle radius
- Ghost Assist toggle on/off
- Ghost Assist color
- Ghost Assist search radius (smaller radius = less help from the assist, bigger radius = basically ESP)

## TO-DO
- Integrate some settings to be configured with ConfigLib
- Copy Vintage Story's spread system and implement it in a way that lets the code access the random direction every time.
- Make the trajectory rendered line follow this spread in real time, giving a sense of difficulty "real player accuracy"
- Make the spread slower/smaller when crouching, faster when moving, even faster when running/jumping
- Somehow store the selected amount of spear throws that ended in an entity being hit and had a good difficulty score (big distance), set fixed goals for amount of throws that make the general accuracy penalty lower, until the player reaches perfect accuracy in any situation (to reward the player for succesful throws with a leveling system that makes it easier to land future spears)

## FAQ
- Will you add stones to the mod?
    - NO
	
- Why does the trajectory sometimes not match the actual hit?
    - The trajectory does not account for entity movement, and some mods implement their own accuracy system (combat overhaul)
	
- Why?
    - When I started playing I thought that the spear throwing was too much RNG (besides the actual aiming part), and I realized that no matter how accurate I was with the throw and the timing of it to get the best accuracy value possible, it still had a crazy amount of deviation, making the overall experience pretty frustraing (at least to me) I first made the simple trajectory render to validate my thoughts, and then decided to fully develop the mod as a QoL upgrade to the vanilla spear throwing.
	
- Do you consider this a cheat?
    - Yes and No, it really depends on the type of player that you are; If you are having a hardcore playthrough then yes, this may be considered cheating, besides from the actual state of the mod (completely removing the accuracy mechanic is, in fact, cheating).
