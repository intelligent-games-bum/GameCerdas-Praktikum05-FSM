<div align="center">

# Intelligent Games Labs

The lab work for the Intelligent Games course, one Unity project that grows with each lab.
The newest is a skeleton driven by a finite state machine: it patrols, hunts you by sight,
fights, runs for safety when it is hurt, and dies.

![Unity](https://img.shields.io/badge/Unity-6.3%20LTS-000000?logo=unity)
![URP](https://img.shields.io/badge/Render-URP-2196F3)
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![Input System](https://img.shields.io/badge/Input%20System-New-orange)
![AI Navigation](https://img.shields.io/badge/AI%20Navigation-2.0-4CAF50)
![FSM](https://img.shields.io/badge/AI-Finite%20State%20Machine-E91E63)

</div>

---

## Labs in this repo

| Lab | Scene | Topic |
|---|---|---|
| 1 to 3 | `Assets/Scenes/NPCDetector.unity` | Perception, a NavMesh guard, and a steering farmhand |
| 4 | `Assets/Scenes/Praktikum4/P4A_AStarGrid.unity` | A* written by hand on a grid, with the open and closed sets drawn on the floor |
| 4 | `Assets/Scenes/Praktikum4/P4B_NavMeshChase.unity` | NavMesh chasing, repathing, and `NavMeshObstacle` carving |
| 5 | `Assets/Scenes/Praktikum5/Praktikum05_FSM.unity` | Enemy AI as a finite state machine |

## Running it from source

Clone the repo and open the folder with Unity 6.3 LTS. Unity rebuilds the `Library` folder on first launch, which takes a few minutes. Open any scene from the table above and press Play.

The scenes for labs 4 and 5 are generated. The **Praktikum 4** and **Praktikum 5** menus in the editor rebuild them from scratch and bake their NavMesh, so a scene broken by an experiment is one click from new. Rebuilding overwrites the scene file, including anything you changed in it by hand.

---

## Lab 5: Enemy FSM

A skeleton guards a walled field with four waypoints and two safe points. It runs on a finite state machine with five states.

### Controls

| Input | Action |
|---|---|
| WASD | Walk |
| Mouse | Orbit the camera |
| Left click | Strike whatever stands just in front of you, 20 damage, twice a second at most |
| L | Kill the enemy outright |
| H | Restore your health |
| R | Restart the scene |

Click the Game view once first, so the camera takes the cursor. Turn on the **Gizmos** button in the Game view too, otherwise the view cone and ranges stay hidden.

### States and transitions

| From | Condition | To |
|---|---|---|
| Patrol | You are seen | Chase |
| Chase | You come within `attackRange` (2 m) | Attack |
| Chase | It has not seen you for `loseSightTime` (3 s) | Patrol |
| Attack | You move beyond `attackExitRange` (3 m) | Chase |
| Patrol, Chase, Attack | Its health falls to `fleeThreshold` (30%) | Flee |
| Flee | It reaches a safe point | Patrol |
| Any state | Its health reaches 0 | Dead |

The order of the checks matters as much as the table. Every frame [`EnemyFSM.cs`](Assets/Scripts/Praktikum5/EnemyFSM.cs) asks about death first, then about fleeing, and only then hands control to the active state. Dead therefore wins on the very frame health hits zero, and an enemy that just took a fatal blow cannot land one last strike.

| State | What the skeleton does |
|---|---|
| Patrol | Walks the waypoint loop at speed 2, pausing a second at each point |
| Chase | Runs at you at speed 4.5. Once it loses sight of you it heads for the last place it saw you |
| Attack | Stops, turns to face you, and hits for 10 every 1.5 seconds |
| Flee | Sprints at speed 6 to a safe point, then heals to 70% there |
| Dead | Disables its agent and its senses, and falls over |

Attack is left at `attackExitRange`, not at `attackRange`. With a single threshold, a player standing right on the 2 metre line would flip the enemy between Chase and Attack every frame. The extra metre gives it room to settle.

Flee picks between the two safe points by how far each one takes it from you, less how far it has to run. That keeps it from choosing a refuge whose path goes straight through you. The heal on arrival lifts it clear of the flee threshold, otherwise it would reach safety, start patrolling, and flee again on the next frame.

### How it sees you

[`EnemyPerception.cs`](Assets/Scripts/Praktikum5/EnemyPerception.cs) uses the same three tests as the guard from lab 2, cheapest first.

1. **Vision range.** Are you within 12 metres?
2. **Field of view.** Are you inside the 110 degree cone ahead of it?
3. **Line of sight.** Does a ray from its eyes, 1.6 metres up, reach you, a metre off the ground, without hitting anything on the `Obstacle` layer?

The stone walls are three metres tall so you can hide behind them. The perception script only reports what it sees; every decision lives in the FSM.

### Reading it while it runs

| Where | What you see |
|---|---|
| Console | One colour-coded line per transition, with the reason, plus a line for every hit |
| Inspector, on `Enemy_Skeleton (FSM)` | `Current State`, `Previous State` and `Last Transition` |
| Top-left panel | Current state, both health bars, the attack cooldown, and the flee destination |
| Over each head | A health bar, with the enemy's state written above its own |
| Ball over the skeleton | Green for Patrol, yellow Chase, red Attack, blue Flee, grey Dead. It flashes white when the skeleton is hit |

| Gizmo | Meaning |
|---|---|
| Large circle and shaded cone | Vision range and field of view. Yellow while it cannot see you, red once it can |
| Line to you | Green when line of sight is clear, red when an obstacle blocks it |
| Solid red ring | Attack range |
| Thin orange ring | Attack exit range |
| Cyan loop | The patrol route |
| Green rings | Safe points |

### About the skeleton

The Synty skeleton ships without animations. Its rig is Humanoid, so it borrows the Supersoldier's Idle and Running clips by retargeting, blended by the agent's speed in a controller the scene builder generates. There is no attack or death clip, so those are done in code. The model lunges forward on each hit and topples backwards when it dies.

### Scripts

| File | Job |
|---|---|
| `EnemyFSM.cs` | The state machine, its transitions, the console log and the range gizmos |
| `EnemyPerception.cs` | Vision range, field of view, line of sight |
| `Health.cs` | Health for both sides, with damaged, healed and died events |
| `SafePoint.cs` | A flee destination that heals on arrival |
| `EnemyVisualFeedback.cs` | Animator speed, the state ball, the lunge and the fall |
| `PlayerMeleeAttack.cs` | The player's left-click strike |
| `FSMDebugHUD.cs` | The on-screen panel, the overhead bars and the test keys |
| `Editor/Praktikum5SceneBuilder.cs` | Builds the scene, the enemy animator and the NavMesh |

---

## Labs 1 to 3: Autonomous steering agent

A farmhand that finds its way around the island with no map at all,
sharing it with a guard who hunts you by sight and loses you when you break cover.

**[Play it in your browser](https://play.unity.com/en/games/e2cd4937-3be9-4cda-90f3-91a958a63660/web)**

![Video demo](public/demo-video.gif)

### Controls

Click the game window once before you start. Browsers hand over the mouse cursor after a click, so the camera will not respond until you give it one.

| Input | On the ground | In flight |
|---|---|---|
| WASD or arrows | Walk | Steer |
| Mouse | Orbit the camera | Orbit the camera |
| Space | Jump | Gain height |
| Space twice, quickly | Take off | Cancel and drop |
| Shift | — | Descend |

A flight runs for 10 seconds. Touching the ground ends it early, and the next one waits 3 seconds.

Tapping Space twice to climb faster will cancel the flight instead, since that gesture is also the cancel command. Leave a gap between taps.

### How the farmhand steers

[`SteeringAgent.cs`](Assets/Scripts/SteeringAgent.cs) is this lab, carried in the scene by `Pengelola Sawit`. It walks the same island as the guard from the previous lab and shares none of its code. It never touches the NavMesh. The guard asks Unity for a path and follows it; the farmhand knows only what its sensors report this frame.

Every frame it works out the velocity it wants, subtracts the one it has, and divides by `responseTime` to get a steering force. That force is capped by `maxForce`, added to velocity, capped by `maxSpeed`, and applied to its position.

| State | When | What it does |
|---|---|---|
| Wander | No target | Drifts a point around a circle projected ahead of it and steers at that point |
| Seek | Target beyond `slowRadius` | Runs at it at full speed |
| Arrive | Target inside `slowRadius` | Scales speed down with distance, brakes to a halt inside `stopRadius` |

Wander stays smooth because that point moves a little each frame instead of jumping somewhere new. `wanderStrength` sets how fast it drifts. Small values give long straight walks, large ones make it fidget.

Give it an `NPCSensor` and it finds its own target: it chases whatever the sensor sees and drops back to wander `loseTargetDelay` seconds after losing sight of it. Leave that slot empty and the target is whatever you drag into the Inspector.

### How the farmhand avoids things

[`SteeringSensor.cs`](Assets/Scripts/SteeringSensor.cs) fires three rays along the direction of travel rather than the direction the body faces. The body turns with a slerp and lags behind during a turn, so facing is the wrong thing to ask.

Surfaces tilted less than `maxWalkableSlope` count as ground and are ignored. The island pieces sit on the `Obstacle` layer along with the crates, so without that test every hill ahead would read as a wall.

Three more probes point down at the ground ahead. A probe that finds nothing is treated like a wall, which is what keeps it on the island. The circular `boundsRadius` is a second net behind that one, useful because the island is not a circle.

Two habits stop it dithering. It remembers which way it chose to turn and only switches when the other side is clearer by `sideSwitchMargin`, and the avoidance force is damped over `avoidanceSmoothing` so it cannot reverse within a frame.

The avoidance force does not add to the behaviour force. It takes over:

```
takeover = threatLevel × avoidanceWeight
force    = lerp(behaviour force, avoidance force, takeover)
```

Adding the two was the obvious approach and it failed. A Seek force aimed straight at you wins the sum, so the farmhand pressed into the wall and shook. Handing avoidance full control at the moment it is cornered lets it turn.

None of that is a guarantee. Steering is advice, and advice can arrive too late. Before each step lands, a spherecast as wide as its body checks the path; if something blocks it, the step is cut short and the remainder slides along the surface. It runs twice, because sliding off one wall can bury it in the next. This is what stops it walking through the house. The steering is what makes going around the house look deliberate.

### Tuning the farmhand

Every number below is on the agent or its sensor, and changing one alone is the quickest way to see what it owns.

| Parameter | Turn it down | Turn it up |
|---|---|---|
| `maxSpeed` | Ambles | Overshoots targets and corners |
| `maxForce` | Wide lazy turns | Sharp turns, harder braking |
| `turnSpeed` | Body trails behind the path it walks | Snaps to face the path |
| `responseTime` | Stiff, almost robotic | Slides about as if on ice |
| `slowRadius` | Brakes late and abruptly | Crawls the last stretch |
| `stopRadius` | Stands on top of the target | Halts well short of it |
| `wanderStrength` | Long straight walks | Fidgets |
| `sensorDistance` | Notices walls too late | Swerves around things that were never in the way |
| `avoidanceWeight` | Ignores obstacles and leans on collision instead | Timid, bends away early |

Two of them are linked. The distance needed to stop is roughly `maxSpeed² ÷ (2 × maxForce)`, so raising `maxSpeed` without raising `maxForce` eventually pushes that distance past `slowRadius`, and the farmhand starts sailing through targets again.

### How the guard sees you

The second lab is still here, and the third is best read against it.

[`NPCSensor.cs`](Assets/Scripts/NPCSensor.cs) runs three tests every frame, ordered so the cheap ones reject you before the expensive one runs.

1. **Distance.** Are you inside the 10 metre radius?
2. **Angle.** Are you inside the 61 degree cone in front of the guard?
3. **Line of sight.** Does a ray from the guard's eyes reach yours without hitting anything on the `Obstacle` layer?

Fail any one of them and the guard perceives nothing.

That third test is what lets you hide. Crates, trees and hills sit on a layer the raycast collides with, so putting one between yourself and the guard breaks the chain. The ray leaves the guard a metre off the ground and arrives at the same height on you, which means cover shorter than a metre will not save you.

The farmhand borrows this same script to find you. Its own sensor only reads walls and ledges.

### How the guard decides

[`NPCBrain.cs`](Assets/Scripts/NPCBrain.cs) drives a `NavMeshAgent` through three states.

| State | What the guard does |
|---|---|
| Patrol | Walks the waypoint loop at speed 5 |
| Chase | Runs straight at you at speed 10 |
| Search | Goes to the spot where it last saw you, hunts for 3 seconds, gives up |

Search carries the memory. The guard records your position on every frame it can see you, so breaking line of sight never erases what it already knows. It walks to that spot first, then returns to patrol after finding nothing.

### How you move

[`PlayerController.cs`](Assets/Scripts/PlayerController.cs) moves a `CharacterController`, so walls and crates stop you. It integrates vertical motion by hand rather than leaving it to gravity, because flight needs control that gravity alone will not give.

W sends you wherever the camera faces, and the character turns to meet the direction it travels. [`CameraFollow.cs`](Assets/Scripts/CameraFollow.cs) orbits on a spring arm that shortens whenever scenery gets between the camera and your back.

[`PlayerAnimatorDriver.cs`](Assets/Scripts/PlayerAnimatorDriver.cs) writes one integer into the Animator, and three Any State transitions read it to pick the clip. A jump keeps the ground pose, since the character ships without a jump animation.

### Scripts

| File | Job |
|---|---|
| `SteeringAgent.cs` | Seek, Arrive, Wander, ground snapping, collision blocking |
| `SteeringSensor.cs` | Obstacle rays, ledge probes, threat level |
| `SteeringDebug.cs` | Gizmos for every part of the steering |
| `SimplePlayerController.cs` | Bare manual controller, kept as a reference point |
| `NPCSensor.cs` | Distance, field of view, line of sight |
| `NPCBrain.cs` | Patrol, Chase and Search over a NavMesh |
| `EnemyDetector.cs` | Colours the guard's light to match its state |
| `PlayerController.cs` | Walking, jumping, and the timed flight |
| `CameraFollow.cs` | Orbit camera that dodges scenery |
| `PlayerAnimatorDriver.cs` | Turns the locomotion state into Animator parameters |

### Gizmos

Keep the Scene view in sight while you play. Reading the farmhand's gizmos is the fastest way to tell a tuning problem from a reference you forgot to fill in, and [`SteeringDebug.cs`](Assets/Scripts/SteeringDebug.cs) draws all of them.

| Gizmo | Meaning |
|---|---|
| Label overhead | The state it is in right now |
| Cyan line | Velocity, the direction it is actually travelling |
| Yellow line | The direction the active behaviour wants |
| Magenta circle | The wander circle, drawn only while wandering |
| Three rays | Green is clear, red has hit something |
| Three small spheres | Ground probes ahead. Red means no ground there |
| Orange line | The avoidance force after damping |
| Grey circle at chest height | The collision body. Red while something is holding it back |
| White circle | The wander boundary |

The guard has its own set. `NPCSensor` draws the detection sphere, the edges of the view cone, and a line to you that turns red the moment the guard has you and grey when it does not. Select the guard and the `Has Line Of Sight` box in the Inspector tells you the same thing at a glance.

### Notes

Anything works as cover if it carries a collider, sits on the `Obstacle` layer, and stands taller than a metre. The single crates land right on that limit, so reach for the stacked pair or the trees.

`EnemyDetector.cs` is the distance-only detector from the first lab, kept for comparison. It now consults `NPCSensor` before switching state, so the light stops turning red while you are behind cover.

The guard and the farmhand are the comparison the third lab is really about. Put them beside the same wall and the guard rounds it without hesitating, because the NavMesh was baked before it took a step. The farmhand pauses, decides, then commits, because three metres of ray is everything it knows. Neither approach is the better one. The guard cannot react to anything that was not there at bake time; the farmhand needs no bake at all.

Two limits worth knowing. The collision check is a single sphere at chest height, so anything much shorter gets walked straight over. And `Physics.queriesHitBackfaces` is switched on globally, so walls register from inside a building, which the guard's line of sight tests inherit whether they want them or not.
