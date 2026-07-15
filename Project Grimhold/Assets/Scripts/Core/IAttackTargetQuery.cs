using System.Collections.Generic;

/// <summary>
/// Contrato para la consulta física de objetivos en el mundo de juego.
/// Permite desacoplar el sistema de combate de Physics2D.
/// </summary>
public interface IAttackTargetQuery
{
    IReadOnlyList<AttackTarget> FindTargets(in AttackTargetQuery query);
}
