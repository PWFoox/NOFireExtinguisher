using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace NOFireExtinguisher
{
    internal static class FireExtinguisher
    {
        private static readonly ConditionalWeakTable<Aircraft, ChargeCounter> chargesTable =
            new ConditionalWeakTable<Aircraft, ChargeCounter>();

        private class ChargeCounter
        {
            public int Remaining;
        }

        public static void TryExtinguishOwnAircraft()
        {
            try
            {
                Aircraft aircraft = SceneSingleton<CombatHUD>.i != null
                    ? SceneSingleton<CombatHUD>.i.aircraft
                    : null;

                if (aircraft == null)
                {
                    Plugin.Log?.LogWarning("[FireExtinguisher] Не найден текущий самолёт игрока (CombatHUD.aircraft == null).");
                    return;
                }

                Plugin.Log?.LogInfo($"[FireExtinguisher] Проверяю самолёт: '{aircraft.gameObject.name}' (instanceId={aircraft.GetInstanceID()})");

                ChargeCounter counter = chargesTable.GetValue(aircraft, _ => new ChargeCounter { Remaining = Plugin.MaxCharges.Value });

                if (counter.Remaining <= 0)
                {
                    Plugin.Log?.LogInfo("[FireExtinguisher] Заряды огнетушителя закончились.");
                    ReportToHud("Fire extinguisher empty");
                    return;
                }

                int extinguishedCount = ExtinguishAllFires(aircraft);

                if (extinguishedCount > 0)
                {
                    counter.Remaining--;
                    Plugin.Log?.LogInfo(
                        $"[FireExtinguisher] Потушено очагов: {extinguishedCount}. Осталось зарядов: {counter.Remaining}/{Plugin.MaxCharges.Value}"
                    );
                    ReportToHud($"Fire suppressed ({counter.Remaining} charges left)");
                }
                else
                {
                    Plugin.Log?.LogInfo("[FireExtinguisher] Активных пожаров не найдено, заряд не потрачен.");
                    ReportToHud("No fire detected");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[FireExtinguisher] Ошибка: {ex}");
            }
        }

        private static int ExtinguishAllFires(Aircraft aircraft)
        {
            int extinguishedCount = 0;

            // Ищем вообще ВСЕ объекты DamageParticles в активной сцене, где бы они ни находились
            DamageParticles[] allParticles = UnityEngine.Object.FindObjectsOfType<DamageParticles>();

            Plugin.Log?.LogInfo($"[FireExtinguisher] Всего DamageParticles в мире: {allParticles.Length}");

            foreach (DamageParticles dp in allParticles)
            {
                // Проверяем дистанцию от пожара до центра нашего самолета
                // 60 метров — хороший радиус, чтобы захватить даже отвалившиеся горящие куски
                float distance = Vector3.Distance(aircraft.transform.position, dp.transform.position);
                
                if (distance > 60f)
                {
                    continue; // Пропускаем пожары, которые горят слишком далеко (чужие самолеты)
                }

                var traverse = Traverse.Create(dp);
                float fireDamage = traverse.Field("fireDamage").GetValue<float>();
                float fireLifetime = traverse.Field("fireLifetime").GetValue<float>();

                Plugin.Log?.LogInfo(
                    $"[FireExtinguisher]   -> '{dp.gameObject.name}' рядом (dist={distance:F1}, fireDamage={fireDamage:F2}, fireLifetime={fireLifetime:F2})"
                );

                // Тушим, если очаг активен
                if (fireLifetime > 0f || fireDamage > 0f)
                {
                    Light fireLight = traverse.Field("fireLight").GetValue<Light>();
                    if (fireLight != null)
                    {
                        UnityEngine.Object.Destroy(fireLight.gameObject);
                    }

                    // Просто и надежно уничтожаем сам игровой объект пожара
                    UnityEngine.Object.Destroy(dp.gameObject);

                    extinguishedCount++;
                    Plugin.Log?.LogInfo($"[FireExtinguisher]      ПОТУШЕН И УДАЛЕН: '{dp.gameObject.name}'");
                }
            }

            return extinguishedCount;
        }

        private static string GetFullPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        private static void ReportToHud(string message)
        {
            if (SceneSingleton<AircraftActionsReport>.i != null)
            {
                SceneSingleton<AircraftActionsReport>.i.ReportText(message, 4f);
            }
        }
    }
}