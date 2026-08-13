using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using HarmonyLib;
using UnityEngine;

namespace Shock12.Client
{
    internal static class ShockHealthEffects
    {
        internal static void AddTremor(ActiveHealthController controller)
        {
            controller.AddEffect<ActiveHealthController.Tremor>(
                EBodyPart.Head,
                0f,
                Shock12Constants.TremorDuration,
                0f,
                5f,
                null);
        }

        internal static void AddPanic(ActiveHealthController controller)
        {
            controller.AddEffect<ActiveHealthController.PanicEffect>(
                EBodyPart.Head,
                0f,
                Shock12Constants.PanicDuration,
                null,
                null,
                null);

            // Tarkov's own forced-jam event pairs PanicEffect with MisfireEffect.
            // Panic supplies the status condition; Misfire guarantees the next
            // attempted shot jams while the five-second panic window is active.
            controller.AddMisfireEffect(Shock12Constants.PanicDuration, true);
        }
    }

    [HarmonyPatch(
        typeof(ActiveHealthController),
        "ApplyDamage",
        new[] { typeof(EBodyPart), typeof(float), typeof(DamageInfo) })]
    internal static class ImpactEffectsPatch
    {
        private static void Postfix(
            ActiveHealthController __instance,
            EBodyPart bodyPart,
            float damage,
            DamageInfo damageInfo)
        {
            if (__instance == null ||
                damageInfo.SourceId != Shock12Constants.TemplateId ||
                damage <= 0f ||
                !__instance.IsAlive)
            {
                return;
            }

            __instance.DoPain(
                bodyPart,
                0f,
                Shock12Constants.PainDuration,
                0f,
                1f);
            __instance.DoContusion(Shock12Constants.ConcussionDuration, 1f);
            ShockHealthEffects.AddTremor(__instance);
            ShockHealthEffects.AddPanic(__instance);

            Player player = __instance.Player;
            if (player == null)
            {
                return;
            }

            if (player.Physical != null)
            {
                if (player.Physical.Stamina != null)
                {
                    player.Physical.Stamina.UpdateStamina(0f);
                }

                if (player.Physical.HandsStamina != null)
                {
                    player.Physical.HandsStamina.UpdateStamina(0f);
                }
            }

            ExtremeTremor.Begin(player, Shock12Constants.TremorDuration);
        }
    }

    internal sealed class ExtremeTremor : MonoBehaviour
    {
        private Player _player;
        private float _startedAt;
        private float _endsAt;
        private Vector2 _lastOffset;

        internal static void Begin(Player player, float duration)
        {
            ExtremeTremor tremor = player.GetComponent<ExtremeTremor>();
            if (tremor == null)
            {
                tremor = player.gameObject.AddComponent<ExtremeTremor>();
                tremor._player = player;
                tremor._startedAt = Time.time;
            }

            tremor._endsAt = Mathf.Max(tremor._endsAt, Time.time + duration);
        }

        private void Update()
        {
            if (_player == null || !_player.HealthController.IsAlive)
            {
                Destroy(this);
                return;
            }

            if (!_player.IsYourPlayer)
            {
                if (Time.time >= _endsAt)
                {
                    Destroy(this);
                }
                return;
            }

            if (Time.time >= _endsAt)
            {
                SetAimOffset(Vector2.zero);
                Destroy(this);
                return;
            }

            float elapsed = Time.time - _startedAt;
            float fade = Mathf.Clamp01((_endsAt - Time.time) / 3f);

            // Several uneven frequencies avoid a predictable left-right wobble.
            // The resulting view movement is bounded, but violent enough that
            // holding a precise sight picture is extremely difficult.
            float yaw =
                Mathf.Sin(elapsed * 10.7f) * 1.8f +
                Mathf.Sin(elapsed * 19.3f + 1.2f) * 1.15f +
                (Mathf.PerlinNoise(elapsed * 5.2f, 0.31f) - 0.5f) * 2.1f;
            float pitch =
                Mathf.Sin(elapsed * 13.1f + 0.7f) * 1.45f +
                Mathf.Sin(elapsed * 23.7f) * 0.9f +
                (Mathf.PerlinNoise(0.73f, elapsed * 6.1f) - 0.5f) * 1.6f;

            SetAimOffset(new Vector2(yaw, pitch) * fade);
        }

        private void SetAimOffset(Vector2 offset)
        {
            if (_player.MovementContext == null)
            {
                return;
            }

            Vector2 change = offset - _lastOffset;
            _player.MovementContext.SetRotation(
                _player.MovementContext.Rotation + change);
            _lastOffset = offset;
        }

        private void OnDestroy()
        {
            if (_player != null && _player.IsYourPlayer && _lastOffset.sqrMagnitude > 0f)
            {
                SetAimOffset(Vector2.zero);
            }
        }
    }
}
