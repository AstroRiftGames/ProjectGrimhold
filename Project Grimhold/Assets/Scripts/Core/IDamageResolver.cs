/// <summary>
/// Contrato para el pipeline de daño encargado de localizar y resolver solicitudes de daño.
/// </summary>
public interface IDamageResolver
{
    DamageResult Resolve(in DamageRequest request);
}
