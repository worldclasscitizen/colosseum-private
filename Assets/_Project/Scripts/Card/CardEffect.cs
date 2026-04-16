using UnityEngine;
using Fusion;

namespace Colosseum.Card
{
    /// <summary>
    /// 플레이어에 부착. 카드로 획득한 총알/이동 강화를 누적 저장.
    /// Gun이 총알을 생성할 때 이 값들을 참조.
    /// </summary>
    public class CardEffect : NetworkBehaviour
    {
        // 총알 강화 누적
        [Networked] public float DamageMultiplier { get; set; } = 1f;
        [Networked] public float BulletSpeedMultiplier { get; set; } = 1f;
        [Networked] public float BulletSizeMultiplier { get; set; } = 1f;
        [Networked] public int BonusBounce { get; set; } = 0;
        [Networked] public float GravityModifier { get; set; } = 0f;

        // 이동 강화 누적
        [Networked] public float MoveSpeedMultiplier { get; set; } = 1f;
        [Networked] public float JumpForceMultiplier { get; set; } = 1f;

        // 상태이상
        [Networked] public int CurrentStatusEffect { get; set; } = 0;
        [Networked] public float StatusDuration { get; set; } = 0f;

        // 특수 효과
        [Networked] public int CurrentSpecialEffect { get; set; } = 0;
        [Networked] public float SpecialValue { get; set; } = 0f;

        public void ApplyCard(CardData card)
        {
            // 곱연산 누적
            DamageMultiplier *= card.damageMultiplier;
            BulletSpeedMultiplier *= card.speedMultiplier;
            BulletSizeMultiplier *= card.sizeMultiplier;
            MoveSpeedMultiplier *= card.moveSpeedMultiplier;
            JumpForceMultiplier *= card.jumpForceMultiplier;

            // 합연산 누적
            BonusBounce += card.extraBounce;
            GravityModifier += card.gravityModifier;

            // 상태이상/특수효과는 최신 카드가 덮어씀
            if (card.statusEffect != StatusEffectType.None)
            {
                CurrentStatusEffect = (int)card.statusEffect;
                StatusDuration = card.statusDuration;
            }

            if (card.specialEffect != SpecialEffect.None)
            {
                CurrentSpecialEffect = (int)card.specialEffect;
                SpecialValue = card.specialValue;
            }

            Debug.Log($"[Colosseum] CardEffect updated - DMG:{DamageMultiplier}x, SPD:{BulletSpeedMultiplier}x, Bounce:{BonusBounce}");
        }

        /// <summary>
        /// 강화 초기화 (필요 시)
        /// </summary>
        public void ResetAll()
        {
            DamageMultiplier = 1f;
            BulletSpeedMultiplier = 1f;
            BulletSizeMultiplier = 1f;
            BonusBounce = 0;
            GravityModifier = 0f;
            MoveSpeedMultiplier = 1f;
            JumpForceMultiplier = 1f;
            CurrentStatusEffect = 0;
            StatusDuration = 0f;
            CurrentSpecialEffect = 0;
            SpecialValue = 0f;
        }
    }
}
