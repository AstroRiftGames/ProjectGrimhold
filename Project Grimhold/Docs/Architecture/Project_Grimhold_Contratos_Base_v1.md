# Especificación de Contratos, Interfaces y Estructuras

> **Proyecto:** Project Grimhold\
> **Estado:** Borrador v1

## Objetivo

Este documento define los contratos base que utilizarán todos los
sistemas de gameplay del proyecto.

### Principios

-   Independientes de Photon Fusion.
-   Independientes de clases concretas (`Player`, `Enemy`, `NPC`).
-   Sin lógica visual.
-   Sin lógica de red.
-   Reutilizables.

------------------------------------------------------------------------

# Arquitectura

``` text
Personaje
    │
    ├── Solicita ataque
    ├── Recibe daño
    └── Interactúa
```

## Componentes conceptuales

  Sistema        Responsabilidad
  -------------- ----------------------------------
  Personajes     Solicitan acciones
  Ataques        Detectan objetivos
  Daño           Valida y aplica daño
  Interacción    Procesa acciones sobre objetos
  Presentación   Reproduce VFX, SFX y animaciones

------------------------------------------------------------------------

# Flujo de ataque

``` text
Character
    │
    ▼
AttackRequest
    │
    ▼
IAttack
    │
    ▼
DamageRequest
    │
    ▼
IDamageable
    │
    ▼
DamageResult
    │
    ▼
Presentación
```

------------------------------------------------------------------------

# Interfaces

## ICharacter

Responsabilidad: - Representar cualquier personaje del juego.

## IDamageable

Responsabilidad: - Recibir una solicitud de daño y devolver un
resultado.

## IAttacker

Responsabilidad: - Solicitar la ejecución de un ataque.

## IAttack

Responsabilidad: - Detectar objetivos y generar un `DamageRequest`.

## IInteractable

Responsabilidad: - Procesar una interacción.

## IPickup

Especialización de `IInteractable` para objetos recogibles.

------------------------------------------------------------------------

# Estructuras

## DamageRequest

Debe contener:

-   Identificador del atacante
-   Identificador del objetivo
-   Cantidad de daño
-   Tipo de daño
-   Dirección del impacto
-   Punto de impacto
-   Timestamp

## DamageResult

Debe indicar:

-   Si el daño fue aplicado
-   Daño aplicado
-   Vida restante
-   Si el objetivo murió

## AttackRequest

Debe contener:

-   Atacante
-   Tipo de ataque
-   Origen
-   Dirección
-   Alcance
-   Timestamp

## AttackResult

Debe indicar:

-   Si el ataque fue ejecutado
-   Si encontró un objetivo
-   Objetivo detectado

## InteractionRequest

Información necesaria para iniciar una interacción.

## InteractionResult

Debe indicar:

-   Éxito
-   Si fue consumida
-   Resultado lógico

------------------------------------------------------------------------

# Enumeraciones

-   DamageType
-   AttackType
-   InteractionType

------------------------------------------------------------------------

# Sincronización (Photon Fusion)

-   Los contratos no conocen Photon Fusion.
-   La lógica autorizada se ejecuta únicamente en la instancia con
    **State Authority**.
-   Los clientes solo solicitan acciones.
-   El atacante nunca confirma el daño.
-   La autoridad valida y sincroniza el resultado.

------------------------------------------------------------------------

# Criterios de aceptación

-   ✅ Compilan independientemente.
-   ✅ No dependen de clases concretas.
-   ✅ Reutilizables por jugadores, enemigos y NPCs.
-   ✅ El daño identifica atacante y objetivo.
-   ✅ El resultado informa aplicación y muerte.
-   ✅ Sin presentación visual.
-   ✅ El cliente atacante no confirma el daño.
