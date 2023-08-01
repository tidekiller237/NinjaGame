# NinjaGame
A multiplayer arena fps

This is a project inspired by games like Overwatch and Paladins. Though it does not have a proper title yet, it is meant to be a character based arena shooter. The combat will be focused on priarily projectiles and melee combat mixed with advanced movement that includes wall running, climbing, air jumping, and dashing.

The characters are yet to be developed but they will all have the same basic tools at their desposal which include the movement system.

__*Important note: I am currently developing the functionality of the game, the visuals are far from final and are only for the purposes of testing. It is not indicative of what the game is meant to look like*__

## Movement System

The movement system includes wall running, wall climbing, air jumps and dashes.

### Wall Running

Wall running is infinate duration, however once you leave a wall you must either touch a walkable surface or run on another wall before you can run on that same wall again.


[Demo](https://youtu.be/vZ-Vq8mDlNA)


### Wall Climbing

Wall climbing is timed and you will fall if you run out of time.


[Demo](https://youtu.be/SKrUCKCIpTQ)



While wall climbing, you can seamlessly transition into a wall run on the same surface.


[Demo](https://youtu.be/Y4m1nhzH764)



When you are wall climbing and you jump, you will be propelled away from the wall slightly, this can allow you to easily disengage one surface and transition to another.


[Demo](https://youtu.be/izDHmEm0Ijo)



### Air Jump

You can jump once while in the air. This jump is reset if you wallrun, climb, or touch the ground


[Demo](https://youtu.be/kEhkx0c25gk)



### Dashing

On a short cooldown, you can perform a dash. The behavior of the dash is dependant on if you are on the ground or not.


[Demo](https://youtu.be/du0p5m_y498)



If you are on the ground, you will dash a short distance in the direction you are moving, or forward if you are stationary.


[Demo](https://youtu.be/3uGetTzql6M)



While in the air, you will dash in the direction you are looking. This adds a vertical component to the dash, meaning if you're aiming slightly up then you will dash slightly up as well.


[Demo](https://youtu.be/o9sKwZFKCb0)



Lastly, you cannot get your dash back until you touch a surface while the cooldown is completed, meaning if you are still mid-air when the cooldown ends you will not get your dash back until you touch the ground.


[Demo](https://youtu.be/xEyOek_QY0c)



## Multiplayer

The game uses UnityRelay service to enable peer to peer multiplayer. I temporarily am utilizing join codes, however, I intend to implement a lobby system where you can create/join and invite others to an open lobby.

A demo of the lobby, where each client shows up on the list.


https://github.com/tidekiller237/NinjaGame/assets/42755734/f1cdb01e-7dcd-4559-8fc9-5229f1eabe84


A demo of the host starting the game, all of the clients successfully begin the game as well.


https://github.com/tidekiller237/NinjaGame/assets/42755734/c83f773e-61cf-4482-82d2-d93c6f03e325


And finally, a demo showing that the clients sync the movement and you can see each other player.


https://github.com/tidekiller237/NinjaGame/assets/42755734/78cb4065-020b-437d-b599-176240ac5eb3



## Melee Combat

There are 3 melee attacks you can make regardless of character. A light, medium, or heavy attack. You also have access to a block/counter attack system.

Note: The videos below are meant to include the wind-up time and the health bars to indicate damage taken. In the block section it is especially important.

Light attacks are fast but deal little damage.


https://github.com/tidekiller237/NinjaGame/assets/42755734/e4fd8b74-bacc-4a84-8d0c-edef9da89b4a


Medium attacks are slower and deal a moderate amount of damage.


https://github.com/tidekiller237/NinjaGame/assets/42755734/e4048d2b-76c8-4efb-8a12-b8adaf48369c


Heavy attacks are the slowest but deal the most damage.


https://github.com/tidekiller237/NinjaGame/assets/42755734/a573d006-7c2f-487b-9d79-8002a4f83a95


You can also block and counter attack.

There is a window at the beginning of your block where if you are hit, you can perfrom a counter attack and stun the enemy for briefly.


After the counter window has passed, you will continue to block and will take reduced damage.


https://github.com/tidekiller237/NinjaGame/assets/42755734/e848187d-bda2-4dbb-80ea-b382042731da


## Ranged Combat

While I do have the projectile logic implemented, I do not have any models/animations for it yet so I will have to show you in the future.
