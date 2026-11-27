// ШІ: ChatGPT – Створив CombatAdapterBootstrap.
// Змінено: фікс кода. Автоматично додає PlayerCharacter на гравця та Melee/RangedEnemy на ворогів. Видалено дублюючий enum EnemyType.
using UnityEngine;

namespace Coursework2D
{
    public class CombatAdapterBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // Гравець
            foreach (var p in FindObjectsByType<Player>(FindObjectsSortMode.None))
            {
                if (p.GetComponent<PlayerCharacter>() == null)
                    p.gameObject.AddComponent<PlayerCharacter>();

                if (p.GetComponent<PlayerCombatAdapter>() == null)
                    p.gameObject.AddComponent<PlayerCombatAdapter>();
            }

            // Вороги
            foreach (var e in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (e.GetComponent<EnemyCombatAdapter>() == null)
                {
                    var marker = e.GetComponent<EnemyTypeMarker>();
                    
                    if (marker != null && marker.type == EnemyType.Ranged)
                    {
                        if (e.GetComponent<RangedEnemy>() == null)
                            e.gameObject.AddComponent<RangedEnemy>();
                    }
                    else
                    {
                        if (e.GetComponent<MeleeEnemy>() == null)
                            e.gameObject.AddComponent<MeleeEnemy>();
                    }

                    e.gameObject.AddComponent<EnemyCombatAdapter>();
                }
            }
        }
    }

    public class EnemyTypeMarker : MonoBehaviour
    {
        public EnemyType type = EnemyType.Melee;
    }
}