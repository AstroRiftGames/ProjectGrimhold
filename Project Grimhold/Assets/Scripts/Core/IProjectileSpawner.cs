/// <summary>
/// Contrato para el spawner de proyectiles. Aísla la lógica core del gameplay
/// de los detalles de red e infraestructura.
/// </summary>
public interface IProjectileSpawner
{
    /// <summary>
    /// Intenta spawnear un proyectil con base en los datos de la solicitud.
    /// </summary>
    /// <param name="request">La solicitud con los parámetros del proyectil.</param>
    /// <returns>El resultado indicando si el spawn fue exitoso.</returns>
    ProjectileSpawnResult Spawn(in ProjectileSpawnRequest request);
}
