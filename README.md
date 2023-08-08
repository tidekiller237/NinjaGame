# NinjaGame
A multiplayer arena fps that is a personal project of mine

This is a project inspired by games like Overwatch and Paladins. Though it does not have a proper title yet, it is meant to be a character based arena shooter. The combat will be focused on priarily projectiles and melee combat mixed with advanced movement that includes wall running, climbing, air jumping, and dashing.

The characters are yet to be developed but they will all have the same basic tools at their desposal which include the movement system.

__*Important note: I did not create any of the models shown in the project. I am currently developing the functionality of the game, the visuals are far from final and are only for the purposes of testing. It is not indicative of what the game is meant to look like*__

## Movement System

The movement system includes wall running, wall climbing, air jumps and dashes.

### Wall Running

Wall running is infinate duration, however once you leave a wall you must either touch a walkable surface or run on another wall before you can run on that same wall again.


![WallRunDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/5566e831-688f-439d-9a88-e8529a7f2bbc)


### Wall Climbing

Wall climbing is timed and you will fall if you run out of time.


![ClimbDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/7857db12-e000-496c-96d8-c54011cf42d7)



While wall climbing, you can seamlessly transition into a wall run on the same surface.


![ClimbToWallRunDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/9e1d0d61-55c6-4127-9f47-3d222163064e)



When you are wall climbing and you jump, you will be propelled away from the wall slightly, this can allow you to easily disengage one surface and transition to another.


![ChainClimbDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/a1ad3e18-5afc-4fc4-ab3a-e9d9d3646d56)



### Air Jump

You can jump once while in the air. This jump is reset if you wallrun, climb, or touch the ground


![AirJumpDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/0d5a44fc-359a-4525-b5fd-c967e34be968)



### Dashing

On a short cooldown, you can perform a dash. The behavior of the dash is dependant on if you are on the ground or not.


![DashDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/83a70ae8-8d81-4e3e-818d-2c40f2626091)



If you are on the ground, you will dash a short distance in the direction you are moving, or forward if you are stationary.


![GroundDashDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/993aff2e-14b0-4b4e-ad56-5a29a02339f7)



While in the air, you will dash in the direction you are looking. This adds a vertical component to the dash, meaning if you're aiming slightly up then you will dash slightly up as well.


![AirDashDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/d1cd825f-da0a-4eb7-b201-16e0c9248537)



Lastly, you cannot get your dash back until you touch a surface while the cooldown is completed, meaning if you are still mid-air when the cooldown ends you will not get your dash back until you touch the ground.


![DelayedDashDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/29b523de-73d9-4ecd-9e5c-204e6c689405)



## Multiplayer

The game uses UnityRelay service to enable peer to peer multiplayer. I temporarily am utilizing join codes, however, I intend to implement a lobby system where you can create/join and invite others to an open lobby.

A demo of the lobby, where each client shows up on the list.


![LobbyDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/3100c344-34b1-45ec-8e99-20784d51e444)


A demo of the host starting the game, all of the clients successfully begin the game as well.


![LobbyStartDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/5eb7770b-0ce4-4021-ae50-f30c8480c638)


And finally, a demo showing that the clients sync the movement and you can see each other player.


![MultiplayerSyncDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/c5121afe-6d6c-47d3-ad4d-ab3fe3ad06ab)



## Melee Combat

There are 3 melee attacks you can make regardless of character. A light, medium, or heavy attack. You also have access to a block/counter attack system.

Note: The videos below are meant to include the wind-up time and the health bars to indicate damage taken. In the block section it is especially important.

Light attacks are fast but deal little damage.


![LightAttackDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/505dc93b-1c44-48d5-8487-94bd5eb1b9d0)


Medium attacks are slower and deal a moderate amount of damage.


![MediumAttackDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/5ea3582b-fdbe-461b-bd1b-39720d9a6ec5)


Heavy attacks are the slowest but deal the most damage.


![HeavyAttackDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/df3867cb-dbfc-4185-8528-b04e7b4d0b1a)


You can also block and counter attack.

There is a window at the beginning of your block where if you are hit, you can perfrom a counter attack and stun the enemy for briefly.


After the counter window has passed, you will continue to block and will take reduced damage.


![BlockCounterDemo](https://github.com/tidekiller237/NinjaGame/assets/42755734/66e88889-ac5a-4e94-8991-0a8b2bf5c3c5)


## Ranged Combat

While I do have the projectile logic implemented, I do not have any models/animations for it yet so I will have to show you in the future.
