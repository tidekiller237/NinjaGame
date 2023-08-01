# NinjaGame
A multiplayer arena fps

This is a project inspired by games like Overwatch and Paladins. Though it does not have a proper title yet, it is meant to be a character based arena shooter. The combat will be focused on priarily projectiles and melee combat mixed with advanced movement that includes wall running, climbing, air jumping, and dashing.

The characters are yet to be developed but they will all have the same basic tools at their desposal which include the movement system.

__*Important note: I am currently developing the functionality of the game, the visuals are far from final and are only for the purposes of testing. It is not indicative of what the game is meant to look like*__

## Movement System

The movement system includes wall running, wall climbing, air jumps and dashes.

### Wall Running

Wall running is infinate duration, however once you leave a wall you must either touch a walkable surface or run on another wall before you can run on that same wall again.


https://github.com/tidekiller237/NinjaGame/assets/42755734/e60469d1-71f0-4d53-ab55-94eb1d44dd25



### Wall Climbing

Wall climbing is timed and you will fall if you run out of time.


https://github.com/tidekiller237/NinjaGame/assets/42755734/8a0ef265-f59d-45ae-b052-d632d63694c5



While wall climbing, you can seamlessly transition into a wall run on the same surface.


https://github.com/tidekiller237/NinjaGame/assets/42755734/c0eeebf6-9f90-44f6-b4a0-e46e275c2270



When you are wall climbing and you jump, you will be propelled away from the wall slightly, this can allow you to easily disengage one surface and transition to another.


https://github.com/tidekiller237/NinjaGame/assets/42755734/0120f7c6-841f-46f9-b757-cdca121367ed



### Air Jump

You can jump once while in the air. This jump is reset if you wallrun, climb, or touch the ground


https://github.com/tidekiller237/NinjaGame/assets/42755734/53229c2a-e2e7-4bf9-98c2-7bb703e1833c



### Dashing

On a short cooldown, you can perform a dash. The behavior of the dash is dependant on if you are on the ground or not.


https://github.com/tidekiller237/NinjaGame/assets/42755734/54daa5bb-d67d-4231-8f74-bf08b1e20280



If you are on the ground, you will dash a short distance in the direction you are moving, or forward if you are stationary.


https://github.com/tidekiller237/NinjaGame/assets/42755734/f8538efd-2083-4457-96eb-b8f9cf64aeaa



While in the air, you will dash in the direction you are looking. This adds a vertical component to the dash, meaning if you're aiming slightly up then you will dash slightly up as well.


https://github.com/tidekiller237/NinjaGame/assets/42755734/92e1d5dc-016e-4686-81a7-181924686785



Lastly, you cannot get your dash back until you touch a surface while the cooldown is completed, meaning if you are still mid-air when the cooldown ends you will not get your dash back until you touch the ground.


https://github.com/tidekiller237/NinjaGame/assets/42755734/6e075eca-03e7-4ee7-b46c-33372302ded8

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
