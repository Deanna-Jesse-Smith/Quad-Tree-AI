# Superhero City Simulation

A top-down 2D AI-driven city simulation where heroes, villains, civilians, and 
vigilantes interact dynamically across a destructible urban environment, built as 
a graded Honours assignment exploring game AI techniques.

![screenshot or GIF](docs/demo.gif)

## Overview
- **Problem:** Demonstrate emergent AI behaviour through entity interaction, 
  coordinated movement, adaptive decision-making, and spatial indexing in a 
  real-time simulation
- **Approach:** Finite state machines per entity type, Reynolds steering for 
  flocking, probabilistic combat resolution, and a custom quad-tree for all 
  proximity queries — no Unity physics or NavMesh used for AI detection
- **Status:** Complete

## Tech stack
- Unity 6000.3.8f1
- C#
- Unity GL (immediate mode rendering for quad-tree and district overlays)
- docx (npm) for report generation

## Key features
- Custom quad-tree spatial index with dynamic entity registration, broad-phase 
  rejection, and inclusive boundary queries — queried in 11 distinct locations 
  across the codebase
- Hidden villain mechanic: villains are visually identical to civilians until 
  revealed by crime completion or sustained hero proximity discovery rolls
- Power differential combat: heroes evaluate relative power and civilian 
  proximity before deciding to engage or flee, with probabilistic engagement 
  chance and decision locking to prevent oscillation
- Reynolds flocking for both villain gangs (separation + alignment + cohesion) 
  and civilian crowds (separation + cohesion only, no alignment — deliberately 
  erratic)
- District reputation system: 3x3 world grid tracking live crime levels that 
  drive villain incentivisation scoring, hero patrol waypoint bias, and civilian 
  spawn avoidance
- Civilian outcome distinction: robbed, injured, or killed based on villain 
  power level at moment of targeting, each with distinct visuals and morale 
  penalties
- Civilian and hero population response: hero defeats despawn nearby civilians 
  proportional to hero power; supervillain defeat triggers a delayed civilian 
  return wave
- Morale as a global simulation state driving hero patrol speed, villain 
  incentivisation threshold, civilian wander radius, and camera background colour
- Villain collaboration: leader/follower pair movement with amplified crime damage
- Supervillain grouped hero response with ally wait timer and backup hero 
  spawning
- Civilian conversion to vigilantes and weak villains via accumulated exposure 
  counters
- Autograph distraction: civilians probabilistically stop patrolling heroes, 
  suppressing detection for a duration

## My role
Solo project — all AI design, implementation, debugging, and report writing.

## Getting started
### Prerequisites
- Unity 6000.3.8f1
- No additional packages required beyond Unity built-ins

### Setup
1. Clone the repository
2. Open the project folder in Unity Hub
3. Open the scene at `Assets/Scenes/Main.unity`

### Configuration
No API keys or environment variables required.

### Run
Press Play in the Unity Editor. Debug keys active during Play mode:

| Key | Action |
|-----|--------|
| Q | Toggle quad-tree overlay |
| Z | Toggle district crime overlay |
| M | Force supervillain spawn |
| R | Force villain reveal |
| G | Force villain flock |
| C | Force villain collaboration |
| V | Force civilian → vigilante conversion |
| B | Force civilian → weak villain conversion |
| K | Force hero defeat |
| X | Force supervillain defeat |
| 1 | Force morale to 10 |
| 2 | Force morale to 90 |
| WASD | Pan camera |
| Scroll wheel | Zoom camera |

## Architecture / project structure
Assets/
|— Scripts/ <br>
| |— Core/ <br>
| | |— QuadTree.cs # Spatial index — insert, remove, queryRadius <br>
| | |— QTManager.cs # Singleton wrapper, GL rendering, debug toggle <br>
| | |— Entity.cs # Base class — registration, UpdateRegistration, Die() <br>
| | |— CombatManager.cs # Combat resolution, defeat outcomes <br>
| |— Entities/ <br>
| | |— Hero.cs # 7-state FSM, power differential, pile-in, aid <br>
| | |— HeroPatrol.cs # Waypoint generation, crime bias, hero separation <br>
| | |— Villain.cs # 8-state FSM, crime scoring, flock, collaboration <br>
| | |— Civilian.cs # 7-state FSM, outcome victimisation, conversion <br>
| | |— Vigilante.cs # 2-state seek/wander <br>
| | |— Building.cs # 3-state machine, health, repair boost <br>
| |— Flocking/ <br>
| | |— VillainFlockManager.cs <br>
| | |— FlockGroup.cs # Reynolds rules, breakup broadcast <br>
| | |— CivilianFlockManager.cs <br>
| | |— CivilianFlockGroup.cs # Cohesion + separation only, threat broadcast <br>
| | |— WanderBehaviour.cs # Wander steering + flocking forces for civilians <br>
| |— Systems/ <br>
| | |— MoraleManager.cs # Global morale, district drain, supervillain spawn <br>
| | |— DistrictManager.cs # 3x3 grid, crime raise/decay, GL overlay <br>
| | |— CivilianTracker.cs # Population floor, emergency respawn <br>
| | |— HeroTracker.cs # Hero population floor, backup spawning <br>
| | |— CombatResolution.cs # Win probability, power differential formula <br>
| |— UI/ <br>
| | |— SimulationUI.cs # Entity counts, morale bar, event log <br>
| | |— MoraleAmbientEffect.cs # Camera background colour interpolation <br>
| | |— CameraController.cs # WASD pan, scroll zoom <br>
| |— Spawning.cs # Entity instantiation at simulation start <br>
|— Scenes/ <br>
|— Main.unity <br>


## Technical decisions

**Custom quad-tree over Unity physics queries**
`Physics.OverlapSphere` was the obvious alternative but routes through Unity's 
physics engine, requiring colliders on all entities and coupling detection to the 
physics timestep. A custom quad-tree keeps spatial indexing entirely separate from 
physics, allows deregistration on death without waiting for collider cleanup, and 
makes the structure visible and debuggable via GL rendering. The tradeoff is 
manual registration management on every position update and death.

**Inclusive boundary comparison over Unity's Rect.Contains**
Unity's `Rect.Contains` excludes the right and top edges. Entities positioned 
exactly on subdivision boundaries fail all four child contains-checks and are 
silently dropped during subdivision. Replacing it with a custom `ContainsPoint` 
using `<=` on all edges, combined with a small overlap between child rects at 
their shared boundaries, eliminates this class of bug entirely. The alternative 
of snapping entity positions away from boundaries was rejected as it would corrupt 
movement behaviour.

**Probabilistic FSM over deterministic state transitions for combat**
A deterministic rule — engage if stronger, flee if weaker — produces obviously 
mechanical behaviour that is easy for an observer to predict. Adding a probability 
roll weighted by the power differential, gated by civilian proximity, produces 
non-deterministic outcomes from deterministic inputs. A hero that is severely 
outmatched still has a 30% engagement chance when civilians are nearby, making 
individual hero behaviour feel purposeful rather than scripted. The decision lock 
prevents the roll from re-firing on successive 0.2-second detection ticks, which 
would cause visible oscillation between states.

**Omitting alignment from civilian flocking**
Reynolds' full three-rule formulation (separation + alignment + cohesion) produces 
coordinated formation movement — correct for the villain gang mechanic, wrong for 
civilians. Civilian crowds should look panicked and erratic, not organised. 
Removing alignment while keeping cohesion and separation produces groups that 
cluster loosely and drift toward shared destinations without moving in lockstep. 
This single omission creates a visually legible distinction between the two flock 
types without requiring separate rendering or additional state.

## Challenges and what I learned

**Quad-tree boundary edge cases under floating-point arithmetic**
Entities at positions very close to subdivision boundaries consistently failed 
insertion during the push-down phase of subdivision. The root cause was that 
Unity's `Rect.Contains` is exclusive on two edges, meaning a position of exactly 
`midX` belongs to neither the left nor the right child. This required replacing 
the built-in contains check, adding overlap between child rects, adding a stale 
entity purge before subdivision, and adding a closest-child fallback for any 
entity that still fails all four children. Each fix addressed a different 
manifestation of the same underlying precision problem.

**Axis mismatch between Unity's 2D Rect system and the 3D XZ world plane**
The simulation runs on the XZ plane (top-down 3D) but Unity's `Rect` uses XY. 
Every coordinate passed to the quad-tree required explicit `new Vector2(x, z)` 
construction rather than implicit `Vector2` cast from `Vector3`, which casts XY. 
This caused intermittent silent failures where entities appeared registered but 
were never found by queries, because their registered position had the wrong 
second component. The fix required auditing every Vector2 construction in the 
codebase and establishing `WorldToTreePosition()` as the single conversion point 
in `Entity.cs`.

**Hero state oscillation between pursuit and building aid**
Heroes would visibly flicker between yellow (RespondingToCrime) and cyan 
(AidingBuilding) while pursuing a villain. The cause was a compound boolean 
operator precedence bug in `RunDetection`: `|| State == HeroState.TeamUp` was 
evaluated as a separate condition from the hero type check, making `fightDetected` 
true for every entity in the loop when the hero's own state was TeamUp. This 
caused `commenceTeamUp` to fire on the same tick as villain detection, overriding 
the pursuit. The fix was explicit parenthesisation and a pursuit lock that 
suppresses building detection entirely while a villain target is active.

**Unity's fake null and delayed object destruction**
`Destroy(gameObject)` queues destruction for end of frame. Scripts on other 
entities holding references to the destroyed object receive a Unity "fake null" 
that passes `== null` checks inconsistently across Unity versions. This caused 
MissingReferenceExceptions when the quad-tree's subdivision push-down iterated 
a leaf node containing a just-destroyed entity. The fix required replacing 
`== null` checks on entity references with explicit `gameObject == null || 
!gameObject.activeInHierarchy` checks in collaboration and flock code, adding a 
null guard at the top of `subdivide()` via `RemoveAll(e => e == null)`, and 
establishing `Die()` as the single entity removal path that deregisters 
synchronously before calling `Destroy`.

## Limitations / future work
- No pathfinding — entities move in straight lines and clamp at world boundaries 
  rather than navigating around obstacles. NavMesh integration would allow proper 
  street-level movement and make building obstacles meaningful
- District overlay visualisation implemented but non-functional in the submitted 
  build due to a rendering pipeline incompatibility; numeric district values are 
  visible in the UI instead
- Villain collaboration partner selection is proximity-only — a more sophisticated 
  implementation would match partners by complementary power levels or crime type 
  preference
- Civilian flock group formation is opportunistic rather than need-driven — 
  civilians form groups when they happen to be near each other rather than 
  deliberately seeking safety in numbers
- No persistence — simulation state resets on every play session; a serialisation 
  layer would allow long-running simulation observation

## Credits and licence
- All code written by Deanna Smith (224079158) for a graded Honours assignment 
  at Nelson Mandela University, 2026
- Reynolds steering behaviour formulations: Craig Reynolds, "Steering Behaviors 
  for Autonomous Characters" (GDC 1999)
- Quad-tree structure based on: Finkel & Bentley, "Quad trees: a data structure 
  for retrieval on composite keys" (1974)
- No third-party Unity Asset Store assets used in the final build
- Not licensed for reuse — submitted for academic assessment
