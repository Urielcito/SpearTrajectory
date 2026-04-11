# SpearTrajectory
- (Right now) removes all spread from throwing spears
- Draws the estimated trajectory and impact point of spears when you aim with them
- Draws a ghost trajectory when you are close to hitting an entity
- Turns the trajectory red when a collision with an entity is detected

## TO-DO
- Integrate some settings to be configured with ConfigLib
- Copy Vintage Story's spread system and implement it in a way that lets the code access the random direction every time.
- Make the trajectory rendered line follow this spread in real time, giving a sense of difficulty "real player accuracy"
- Make the spread slower/smaller when crouching, faster when moving, even faster when running/jumping
- Somehow store the selected amount of spear throws that ended in an entity being hit and had a good difficulty score (big distance), set fixed goals for amount of throws that make the general accuracy penalty lower, until the player reaches perfect accuracy in any situation (to reward the player for succesful throws with a leveling system that makes it easier to land future spears)