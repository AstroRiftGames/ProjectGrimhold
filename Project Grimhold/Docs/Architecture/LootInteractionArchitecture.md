# Especificación de Contratos, Interfaces e Interacción de Loot

## 1. Identificador de Loot

`LootId` es un identificador de valor de tipo struct inmutable, pequeño y comparable, adecuado para claves de diccionarios e independiente de ScriptableObjects y APIs de red:

```csharp
public readonly struct LootId : IEquatable<LootId>
{
    public string Value { get; }
    // ...
}
```

## 2. Definiciones de Loot y Catálogo de Datos

Para desacoplar los datos estáticos de la lógica runtime, el juego utiliza activos de configuración basados en `ScriptableObject`:

* **`LootDefinition`**: Define los metadatos estáticos de un item de loot (ej. nombre, descripción, rareza de tipo `LootRarity`, categoría de tipo `LootCategory`, y peso o volumen).
* **`LootDefinitionCatalog`**: Contiene la base de datos completa de todos los `LootDefinition` registrados en el proyecto. 
  * Sirve como fuente de verdad única para validar identificadores de loot.
  * Implementa métodos de validación (`OnValidate` en editor) para asegurar que no existan IDs duplicados o vacíos, evitando errores de configuración antes de compilar.
  * Permite la búsqueda rápida de definiciones mediante `LootId`.

## 3. Contratos de Entrega

La entrega de loot está desacoplada de la clase jugador mediante las siguientes abstracciones:

- **`ILootReceiver`**: Contrato que implementa el receptor para recibir loot de forma transaccional.
- **`LootGrantRequest`**: Contiene `SourceId`, `ReceiverId`, `LootId`, `Amount` y `SimulationTick`.
- **`LootReceiveResult`**: Resultado tipado con factories `Accepted()` y `Rejected(reason)`.

## 4. Transacción del Pickup

`NetworkLootPickup` (bajo State Authority) ejecuta una secuencia de reserva transaccional estricta:

1. **Validación inicial**: Confirma que posee State Authority y que el pickup está disponible (`IsAvailable`).
2. **Reserva**: Marca el pickup como consumido (`IsConsumed = true`) antes de entregar el loot para evitar colisiones multijugador concurrentes en redes con latencia.
3. **Entrega**: Invoca al receptor (`TryGrantLoot`) exactamente una vez.
4. **Validación de resultado**:
   - Si la entrega falla o es rechazada, se restaura la disponibilidad (`IsConsumed = false`) y no se destruye el pickup.
   - Si la entrega es aceptada, se destruye el objeto mediante `Runner.Despawn(Object)` de forma autoritativa.

## 5. Almacenamiento y Sincronización

`PlayerLootReceiver` mantiene la colección temporal de la incursión en una `NetworkDictionary<int, int>` asociada al objeto de red del jugador. State Authority es el único escritor. El propietario y los demás peers que observan el objeto consumen el mismo estado replicado por snapshots de Fusion.

Las claves son índices deterministas generados por `LootDefinitionCatalog`, ordenando los IDs mediante comparación ordinal. Hosts y clientes deben utilizar el mismo catálogo. Sólo se sincronizan el índice y la cantidad; nombre, icono, rareza y valor se resuelven localmente.

La colección tiene una capacidad técnica fija de 64 definiciones distintas debido al formato de `NetworkDictionary`. `PlayerLootReceiver` valida el tamaño completo del catálogo antes de registrarse, por lo que este límite no representa peso, slots ni una regla de capacidad durante gameplay.

Las cantidades son la fuente de verdad sincronizada. El valor total de extracción se deriva bajo demanda usando las definiciones del catálogo y no se replica como un segundo estado mutable.

## 6. Presentación provisional

`PlayerLootReceiver` incrementa `LootChangeSequence` después de cada mutación aceptada. La UI del propietario usa esa secuencia para refrescar la lista y el valor únicamente cuando cambia el estado replicado. Al vincularse, toma la secuencia actual como baseline y muestra el contenido inicial sin interpretarlo como una recogida nueva.

Una entrega aceptada produce además un RPC fiable dirigido al Input Authority con secuencia, source, índice de definición, cantidad y tick. El receptor resuelve el índice mediante el catálogo local, encola un `LootGrantPresentationEvent` y lo publica desde `Render`. El toast se deduplica por secuencia; correcciones o cambios de snapshot actualizan el resumen pero no producen feedback de pickup.

La presentación consume `TryGetLootContent`, `TryCalculateTotalValue` y `TryResolveDefinition`. Si alguna definición no puede resolverse, el HUD muestra `Loot no disponible` y `Valor: —`; nunca presenta un total parcial como válido.

Nombres, iconos, colores, textos y valor derivado permanecen locales. El `Runner.Despawn` del pickup continúa siendo la única fuente de verdad para su desaparición; ningún presenter retrasa ni condiciona el consumo autoritativo.

## 7. Ciclo de Vida

La colección nace vacía con el `NetworkObject` del jugador, permanece durante su participación en la incursión y se elimina al despawnear ese objeto. `PlayerLootReceiver` sólo se registra como capacidad de recepción en el `EntityRegistry` de State Authority y elimina el registro en `Despawned`. Un runner nuevo crea un jugador y una colección nuevos; no existe persistencia hacia stash, equipamiento u otra sesión.
