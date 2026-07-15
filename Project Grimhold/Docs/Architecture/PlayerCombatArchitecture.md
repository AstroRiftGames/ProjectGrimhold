# Player Combat Architecture

This document describes the design, components, network authority, data contracts, and simulation mechanics of the Player Combat System in Project Grimhold.

## Architectural Overview

The combat system is built on a modular, strategy-based architecture designed to support deterministic and authoritative multiplayer combat using **Photon Fusion 2.1** in Host/Client mode. It separates:
1. Input capture and transport.
2. Network boundaries and state tracking.
3. Attack execution strategies (Melee and Ranged).
4. Projectile spawning and physical simulation.
5. Entity registration and collision resolution.

```text
PlayerInputReader (Local Input)
   │
   ▼
FusionInputProvider (Transport)
   │
   ▼
PlayerCombatNetworkController (Network Boundary)
   │
   ├── [AttackSequence, Cooldown Timer]
   ▼
Active Strategy (IAttack: MeleeAttack / RangedAttack)
   │
   ▼ [Ranged Strategy]
FusionProjectileSpawner (Network Spawner)
   │
   ▼
NetworkProjectile (Authoritative Simulation) ──► EntityRegistry & IDamageResolver
```

---

## Key Components

### 1. Data Contracts and Interface Definitions (`IAttack`)
All combat behaviors implement the common strategy contract:
* **`IAttack`**: Interface defining the execution strategy for any weapon/ability.
  * `AttackType Type { get; }` (Melee, Ranged, etc.)
  * `float CooldownSeconds { get; }`
  * `AttackInputMode InputMode { get; }` (Press or Hold)
  * `AttackResult Execute(in AttackRequest request)`
* **`AttackRequest`**: Encapsulates attacker context:
  * `EntityId AttackerId` (resolved from `CharacterBase`)
  * `Vector2 Origin`: The world-space attack origin provided by the character combat controller. For ranged attacks, the final projectile origin may include the configured spawn offset.
  * `Vector2 Direction` (normalized shoot direction)
  * `int SimulationTick` (the exact Fusion tick of execution)
* **`AttackResult`**: Captures execution success or detailed failure reasons (Cooldown, MissingConfiguration, InvalidDirection).

### 2. Network Controller (`PlayerCombatNetworkController`)
Serves as the network boundary for character combat:
* Extends `NetworkBehaviour` and processes combat input during Fusion simulation ticks.
* Only State Authority validates and executes attacks.
* Listens to player input commands (e.g., `PrimaryAttack` button and `AimWorldPosition`).
* Synchronizes `AttackSequence` using a `[Networked]` state variable to ensure clients replicate visual presentation smoothly.
* Handles combat cooldowns authoritatively via network tick timers (`TickTimer`).
* Delegates the actual attack execution to the currently active strategy component (`IAttack`).

### 3. Melee Attack Strategy (`MeleeAttack` & `MeleeAttackConfig`)
Executes instant damage detection in a localized area:
* Reads static data parameters from `MeleeAttackConfig` (ScriptableObject).
* Delegates short-range target detection to `IAttackTargetQuery` using the settings defined by `MeleeAttackConfig`.
* Passes damage requests directly to the centralized `IDamageResolver`.

### 4. Ranged Attack Strategy (`RangedAttack` & `RangedAttackConfig`)
Generates physical projectiles that traverse the world:
* Reads static parameters (speed, range, lifetime, layers, prefabs) from `RangedAttackConfig` (ScriptableObject).
* Integrates a configurable **`ProjectileSpawnOffset`**, configured according to the combined collision bounds of the shooter and projectile, which offsets the initial projectile spawn coordinate in the direction of the aim vector to clear the shooter's own collider bounds.
* Delegates spawning requests to an `IProjectileSpawner` instance.

### 5. Projectile Simulation (`NetworkProjectile`)

Represents a networked projectile whose gameplay simulation is executed exclusively by State Authority.

- **Authority-only simulation**: Movement, collision queries, damage resolution, range validation, lifetime expiration, and despawn decisions occur only on State Authority. Proxy instances receive replicated state for presentation.
- **Kinematic movement**: Uses a kinematic `Rigidbody2D` and advances using `Runner.DeltaTime`, avoiding non-authoritative collision responses or forces.
- **Continuous collision detection**: Casts the projectile's collider across the requested tick displacement so targets cannot be skipped between ticks.
- **Physical initialization**: Aligns the Rigidbody2D, Transform, and networked spawn position before the first authoritative simulation step.
- **Physics synchronization**: When required by manually updated transforms, synchronizes the Unity 2D physics state before performing the collider cast.
- **Owner filtering**: Resolves hit colliders through `EntityRegistry` and ignores every collider associated with the projectile owner while continuing to evaluate subsequent hits.
- **Single-impact guarantee**: Consumes an accepted impact before applying damage or requesting despawn, preventing duplicated damage across subsequent ticks or multiple overlapping cast results.
- **Range and lifetime**: Tracks the exact traveled distance and a network `TickTimer`. The final movement step is clamped so the projectile never exceeds its configured maximum range.
- **Obstacle behavior**: A collider without a registered damageable entity still blocks and despawns the projectile but does not produce a damage request. A wall blocks/despawns the projectile without damage.
- **Collision volume**: The projectile prefab defines a gameplay collider whose effective world-space size is independent from unintended visual scaling. The current implementation validates or adjusts the CircleCollider2D radius during initialization to prevent prefab scale from producing an oversized world-space collision volume.

### 6. Entity Identity (`EntityRegistry`)
A fast-lookup database mapping physical colliders (`Collider2D`) to gameplay entity identities (`EntityId`) and damageable contracts (`IDamageable`):
* Allows the projectile simulation to instantly identify targets without expensive `GetComponent` searches.
* Enables precise owner filtering by checking `BelongsToOwner(Collider2D)`, ensuring a projectile never collides with its shooter or any of its child-objects, while allowing impacts against other players/enemies.

---

## Network Authority Model

| System Layer | Execution Authority | Execution Model | Transport / Replication |
| :--- | :--- | :--- | :--- |
| **Input Capture** | Input Authority | Local input collection | Fusion Input Transport |
| **Attack Validation** | State Authority | Authoritative tick processing | Fusion simulation input |
| **Cooldowns / Timers** | State Authority | Deterministic tick-based state | Networked properties |
| **Projectile Spawning** | State Authority | Authoritative spawn | `Runner.TrySpawn` |
| **Projectile Simulation** | State Authority | Authority-only tick simulation | Networked transform state |
| **Damage Resolution** | State Authority | Authority-only resolution | Direct host-side method call |

---

## Physics and Synchronization Rules

1. **Authoritative Physics**  
   Only State Authority may move gameplay projectiles, select impacts, apply damage, or despawn them. Proxies never apply damage.

2. **Physics State Alignment**  
   The Transform, Rigidbody2D, networked spawn position, and collider query origin must represent the same world-space position before the first cast.

3. **Explicit Synchronization**  
   When transforms are changed manually and a physics query must observe those changes in the same simulation step, synchronize the 2D physics state before running the query. Avoid unnecessary global synchronization calls.

4. **Owner Filtering**  
   Resolve ownership through `EntityId` and `EntityRegistry`. Do not infer ownership from tags, layers, prefab names, or hierarchy names.

5. **Valid Cast Results**  
   Process only the number of results returned by the physics query. Ignore owner colliders and continue searching for the closest valid blocking hit.

6. **Configured Collision Volume**  
   Projectile collider size and spawn offset must be based on gameplay collision requirements rather than the apparent size of the rendered sprite.
