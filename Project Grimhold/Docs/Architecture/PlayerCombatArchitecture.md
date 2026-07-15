# Player Combat Architecture

This document describes the design, components, network authority, data contracts, and simulation mechanics of the Player Combat System in Project Grimhold.

## Architectural Overview

The combat system is built on a modular, strategy-based architecture designed to support predictable multiplayer combat using **Photon Fusion 2.1** in Host/Client mode. It separates:
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
  * `Vector2 Origin` (world coordinates of the weapon nozzle/pivot)
  * `Vector2 Direction` (normalized shoot direction)
  * `int SimulationTick` (the exact Fusion tick of execution)
* **`AttackResult`**: Captures execution success or detailed failure reasons (Cooldown, MissingConfiguration, InvalidDirection).

### 2. Network Controller (`PlayerCombatNetworkController`)
Serves as the network boundary for character combat:
* Extends `NetworkBehaviour` and runs during predicted ticks (`FixedUpdateNetwork`).
* Listens to player input commands (e.g., `PrimaryAttack` button and `AimWorldPosition`).
* Synchronizes `AttackSequence` using a `[Networked]` state variable to ensure clients replicate visual presentation smoothly.
* Handles combat cooldowns authoritatively via network tick timers (`TickTimer`).
* Delegates the actual attack execution to the currently active strategy component (`IAttack`).

### 3. Melee Attack Strategy (`MeleeAttack` & `MeleeAttackConfig`)
Executes instant damage detection in a localized area:
* Reads static data parameters from `MeleeAttackConfig` (ScriptableObject).
* Uses overlap tests to detect target colliders.
* Passes damage requests directly to the centralized `IDamageResolver`.

### 4. Ranged Attack Strategy (`RangedAttack` & `RangedAttackConfig`)
Generates physical projectiles that traverse the world:
* Reads static parameters (speed, range, lifetime, layers, prefabs) from `RangedAttackConfig` (ScriptableObject).
* Integrates a configurable **`ProjectileSpawnOffset`** (default `0.7f`) which offsets the initial projectile spawn coordinate in the direction of the aim vector to clear the shooter's own collider bounds.
* Delegates spawning requests to an `IProjectileSpawner` instance.

### 5. Projectile Simulation (`NetworkProjectile`)
A networked component representing an authoritatively simulated projectile in Fusion:
* **Physically Kinematic**: Utilizes `Rigidbody2D` set to kinematic to avoid unpredicted collision bounces.
* **Continuous Collision Detection**: Performs `Collider2D.Cast` during its tick-driven update (`FixedUpdateNetwork`) to find obstacles and targets.
* **Physical Alignment on Spawn**: Synchronizes Rigidbody position with Transform position dynamically upon network initialization.
* **Auto-Scaling Protection**: Dynamically resizes the `CircleCollider2D` local radius (e.g. `0.125f / localScale`) upon spawning to match the visual scale of the bullet sprite, preventing oversized bounding boxes (which previously caused instant explosions).
* **Deterministic Physics Sync**: Calls `Physics2D.SyncTransforms()` explicitly before making physics queries. This is mandatory because Fusion disables Unity's default physics auto-sync.

### 6. Entity Identity (`EntityRegistry`)
A fast-lookup database mapping physical colliders (`Collider2D`) to gameplay entity identities (`EntityId`) and damageable contracts (`IDamageable`):
* Allows the projectile simulation to instantly identify targets without expensive `GetComponent` searches.
* Enables precise owner filtering by checking `BelongsToOwner(Collider2D)`, ensuring a projectile never collides with its shooter or any of its child-objects, while allowing impacts against other players/enemies.

---

## Network Authority Model

| System Layer | Execution Authority | Predictable | Transport Method |
| :--- | :--- | :--- | :--- |
| **Input Capture** | Input Authority (Client) | Yes | Fusion Input Transport |
| **Cooldowns / Timers** | State Authority (Host) | Yes | Networked properties |
| **Projectile Spawning**| State Authority (Host) | No (Spawned on Host) | `Runner.TrySpawn` |
| **Projectile Simulation**| State Authority (Host) | No | Authority-only tick updates |
| **Damage Resolution** | State Authority (Host) | No | Direct method call on Host |

---

## Physics and Synchronization Rules

1. **Unity Transform Sychronization**: Always invoke `Physics2D.SyncTransforms()` before calling `Collider2D.Cast` or any manual physics overlap query. Without this, the physics engine will query using outdated collider positions, resulting in false zero-distance overlaps.
2. **Owner Filtering**: Filter hits based on `EntityId` from the `EntityRegistry`. Do not filter based on layer masks, tag names, or GameObject hierarchies.
3. **No Visual Interpenetration**: Projectiles must spawn outside the shooter's collider using a valid offset and move authority-first. Visual representation matches the networked simulation position exactly.
