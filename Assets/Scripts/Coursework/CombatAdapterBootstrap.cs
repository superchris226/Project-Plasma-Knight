using UnityEngine;

namespace Coursework2D
{
    // Вішає PlayerCombatAdapter на Player і EnemyCombatAdapter на всі EnemyHealth.
    // Ставити на будь-який об'єкт сцени (не на префаб).
    public class CombatAdapterBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // Гравець
            foreach (var p in FindObjectsByType<Player>(FindObjectsSortMode.None))
            {
                if (p.GetComponent<PlayerCombatAdapter>() == null)
                    p.gameObject.AddComponent<PlayerCombatAdapter>();
            }

            // Вороги
            foreach (var e in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (e.GetComponent<EnemyCombatAdapter>() == null)
                {
                    var marker = e.GetComponent<EnemyTypeMarker>();
                    var adapter = e.gameObject.AddComponent<EnemyCombatAdapter>();
                    if (marker != null)
                    {
                        adapter.SetArchetype(marker.type == EnemyType.Ranged
                            ? EnemyCombatAdapter.EnemyArchetype.Ranged
                            : EnemyCombatAdapter.EnemyArchetype.Melee);
                    }
                }
            }
        }
    }

    // Опційний маркер, щоб розрізняти Melee/Ranged на сцені
    public class EnemyTypeMarker : MonoBehaviour
    {
        public EnemyType type = EnemyType.Melee;
    }
}