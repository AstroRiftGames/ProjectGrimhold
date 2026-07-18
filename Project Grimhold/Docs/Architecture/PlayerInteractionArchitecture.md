# Arquitectura de Interacción del Jugador (Player Interaction Architecture)

Este documento define la arquitectura y el flujo del sistema de interacción en **Project Grimhold**.

El sistema de interacción permite a los personajes realizar acciones contextuales sobre objetos del mundo (como recoger loot de cofres o suelo, activar palancas, abrir puertas, etc.) de forma autoritativa en red y tolerante a la resimulación de Fusion.

---

## 1. Estructura y Flujo de Datos

El flujo sigue el principio de separación estricta entre:
1. **Captura de Entrada Local (Input Authority):** Detección de la tecla de interacción (`PlayerInputButton.Interact`) y empaquetado en `PlayerNetworkInput`.
2. **Control de Simulación de Red (State Authority):** El controlador de red procesa y filtra la intención en `FixedUpdateNetwork`.
3. **Resolución Lógica Pura (Independent):** El `InteractionResolver` estático y puro decide a qué candidato apuntar basándose en reglas espaciales y de prioridad.
4. **Objetos Interactuables (`IInteractable`):** Implementaciones individuales de las reglas de negocio de cada objeto interactivo (ej. cofres, loot pickups).
5. **Presentación Visual (Render):** Sincronización del estado de interacción mediante variables `[Networked]` para notificar a los presenters locales o remotos.

```text
PlayerInputReader (Input Local)
       │ (Interact Button Pressed)
       ▼
FusionInputProvider (Transporte de Input)
       │
       ▼
PlayerInteractionNetworkController.FixedUpdateNetwork (State Authority)
       │
       ├───► Detección de candidatos (IInteractionTargetQuery / Physics2D)
       ├───► Resolución lógica estática (InteractionResolver)
       │        ├───► Comprueba distancias y exclusiones
       │        └───► Invoca IInteractable.Interact() autoritativamente
       ▼
[Networked] InteractionSequence ++ (Sincronización de Red)
       │
       ▼
PlayerInteractionNetworkController.Render (Todos los Clientes)
       └───► Invoca evento local InteractionResolved
```

---

## 2. Componentes Clave

### 2.1 Controlador de Red (`PlayerInteractionNetworkController`)
Responsable de coordinar la interacción en ticks de simulación:
* Se ejecuta exclusivamente bajo **State Authority** en `FixedUpdateNetwork`.
* Detecta pulsaciones del botón mediante el método `WasPressed` de Fusion (`input.Buttons.WasPressed(PreviousButtons, PlayerInputButton.Interact)`), lo cual garantiza que mantener presionado el botón no spamee interacciones.
* Valida condiciones básicas del personaje (por ejemplo, si el jugador está vivo o si tiene habilitado el control de movimiento).
* Utiliza una interfaz `IInteractionTargetQuery` para obtener candidatos en el espacio 2D.
* Delega en `InteractionResolver` para validar y aplicar la interacción.
* Mantiene propiedades de red (`[Networked]`) para sincronizar el último objetivo, resultado, tick y un contador de secuencia (`InteractionSequence`).

### 2.2 Consulta de Objetivos (`IInteractionTargetQuery`)
Define cómo se encuentran los objetivos en el mundo:
* **`Physics2DInteractionTargetQuery`**: Implementa esta interfaz usando `Physics2D.OverlapCircleNonAlloc` para evitar asignaciones de memoria (heap allocations) en la simulación caliente.
* Filtra candidatos basándose en capas (`LayerMask`) y la distancia configurada.

### 2.3 Resolutor Lógico (`InteractionResolver`)
Una clase puramente lógica y estática que contiene las reglas de selección de interacción:
1. Excluye al propio personaje interactuante de los candidatos.
2. Comprueba que las distancias reportadas sean válidas y estén dentro del rango permitido.
3. Solicita la interfaz `IInteractable` del objetivo.
4. Verifica si el objeto permite la interacción actualmente (`CanInteract`).
5. Ejecuta la interacción autoritativa (`Interact`) sobre el primer candidato válido y detiene la búsqueda inmediatamente (garantía de interacción única).

### 2.4 Contratos e Interfaces
* **`IInteractable`**: Interfaz base para cualquier entidad en el juego con la que se pueda interactuar.
  * `bool CanInteract(in InteractionRequest request)`
  * `InteractionResult Interact(in InteractionRequest request)`
* **`InteractionRequest`**: Estructura de solo lectura que transporta el contexto (`InteractorId`, `TargetId`, `SimulationTick`).
* **`InteractionResult`**: Estructura inmutable que indica si la interacción tuvo éxito, si el objeto fue consumido (destruido) y, en caso de fallo, la razón (`InteractionFailureReason`).

---

## 3. Integración con Loot (Pickups)

El sistema de pickups de loot (`NetworkLootPickup`) implementa `IInteractable` de la segregation de la siguiente manera:
1. Al recibir `CanInteract`, comprueba que el pickup no esté marcado como consumido.
2. Al recibir `Interact`, aplica una transacción de reserva estricta:
   * Marca el pickup como consumido (`IsConsumed = true`).
   * Solicita al receptor (`ILootReceiver`, usualmente `PlayerLootReceiver`) la entrega del loot.
   * Si la entrega tiene éxito, devuelve `InteractionResult.Succeeded(isConsumed: true)` y se destruye de la simulación del Host mediante `Runner.Despawn(Object)`.
   * Si falla, revierte `IsConsumed = false` y reporta el rechazo.
