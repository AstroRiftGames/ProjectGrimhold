/// <summary>
/// Representa una entidad viva controlable o autónoma.
/// </summary>
public interface ICharacter : IEntity
{
    bool IsAlive { get; }
}
