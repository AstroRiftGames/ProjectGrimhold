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

`PlayerLootReceiver` almacena localmente el inventario autoritativo en State Authority. 

> [!WARNING]
> La réplica del inventario hacia el cliente propietario (por ejemplo, a través de variables de red compartidas o un inventario completo sincronizado) queda fuera de alcance para esta fase. La persistencia actual reside de manera autoritativa local en la State Authority.
