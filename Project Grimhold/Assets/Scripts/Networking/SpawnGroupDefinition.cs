using System;
using UnityEngine;

namespace Spawning
{
    [Serializable]
    public sealed class SpawnGroupDefinition
    {
        public SpawnGroupType Group;
        public Transform[] SpawnPoints;
        public int Amount;

        public bool IsValid()
        {
            if (Amount < 0) return false;
            if (SpawnPoints == null) return false;
            foreach (var sp in SpawnPoints)
            {
                if (sp == null) return false;
            }
            return true;
        }
    }
}
