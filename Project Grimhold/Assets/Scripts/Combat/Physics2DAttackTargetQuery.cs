using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Implementación basada en Physics2D y el EntityRegistry de la consulta de objetivos.
/// </summary>
[DisallowMultipleComponent]
public sealed class Physics2DAttackTargetQuery : NetworkBehaviour, IAttackTargetQuery
{
    [Header("Configuración de Rendimiento")]
    [SerializeField]
    private int _colliderBufferSize = 64;

    private Collider2D[] _colliderBuffer;
    private EntityRegistry _registry;

    // Estructura interna para realizar la deduplicación y el ordenamiento sin asignaciones de memoria
    private struct CandidateTarget
    {
        public EntityId Id;
        public IDamageable Damageable;
        public Vector2 HitPoint;
        public float SqrDistance;
    }

    private readonly List<CandidateTarget> _candidates = new();
    private readonly List<AttackTarget> _results = new();
    private readonly HashSet<EntityId> _processedIds = new();

    private void Awake()
    {
        _colliderBuffer = new Collider2D[_colliderBufferSize];
    }

    public override void Spawned()
    {
        _registry = Runner.GetComponent<EntityRegistry>();
        if (_registry == null)
        {
            Debug.LogError($"{nameof(Physics2DAttackTargetQuery)}: EntityRegistry component was not found on the NetworkRunner GameObject.", this);
        }
    }

    /// <summary>
    /// Consulta los objetivos dentro del área circular del ataque melee.
    /// Deduplica por EntityId, ordena por distancia al centro del ataque y limita a MaximumTargets.
    /// </summary>
    public IReadOnlyList<AttackTarget> FindTargets(in AttackTargetQuery query)
    {
        _results.Clear();

        if (_registry == null)
        {
            return _results;
        }

        Vector2 attackCenter = query.Origin + query.Direction * query.Range;

        // Configuración de filtro para OverlapCircle
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.SetLayerMask(query.TargetLayerMask);
        filter.useTriggers = true; // Permitimos triggers o colliders según se configure

        int hitCount = Physics2D.OverlapCircle(
            attackCenter,
            query.Radius,
            filter,
            _colliderBuffer
        );

        _candidates.Clear();
        _processedIds.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = _colliderBuffer[i];
            if (col == null)
            {
                continue;
            }

            // Recuperar EntityId a través del registro
            if (!_registry.TryGetEntityId(col, out EntityId targetId))
            {
                continue;
            }

            // Excluir al atacante
            if (targetId == query.AttackerId)
            {
                continue;
            }

            // Evitar duplicados por EntityId en este tick
            if (_processedIds.Contains(targetId))
            {
                continue;
            }

            // Obtener el IDamageable para validar estado vital y disponibilidad de daño
            if (!_registry.TryGetDamageable(targetId, out IDamageable damageable))
            {
                continue;
            }

            // Excluir si no puede recibir daño
            if (!damageable.CanReceiveDamage)
            {
                continue;
            }

            // Excluir entidades muertas (ICharacter)
            if (damageable is ICharacter character && !character.IsAlive)
            {
                continue;
            }

            // Punto de impacto razonable: punto más cercano en el collider, o la posición del objeto
            Vector2 hitPoint = col.ClosestPoint(attackCenter);

            float sqrDistance = (hitPoint - attackCenter).sqrMagnitude;

            _candidates.Add(new CandidateTarget
            {
                Id = targetId,
                Damageable = damageable,
                HitPoint = hitPoint,
                SqrDistance = sqrDistance
            });

            _processedIds.Add(targetId);
        }

        // Limpiar el buffer de colliders procesados
        Array.Clear(_colliderBuffer, 0, hitCount);

        if (_candidates.Count == 0)
        {
            return _results;
        }

        // Ordenamiento por menor distancia cuadrada al attackCenter. Desempate estable usando EntityId.
        _candidates.Sort(CompareCandidates);

        // Limitar la cantidad máxima de objetivos seleccionados
        int targetsToTake = Mathf.Min(_candidates.Count, query.MaximumTargets);
        for (int i = 0; i < targetsToTake; i++)
        {
            CandidateTarget candidate = _candidates[i];
            _results.Add(new AttackTarget(candidate.Id, candidate.HitPoint));
        }

        return _results;
    }

    private static int CompareCandidates(CandidateTarget a, CandidateTarget b)
    {
        int distanceCompare = a.SqrDistance.CompareTo(b.SqrDistance);
        if (distanceCompare != 0)
        {
            return distanceCompare;
        }
        return a.Id.Value.CompareTo(b.Id.Value);
    }
}
