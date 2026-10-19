using System.Collections;
using UnityEngine;
using TDTS.Data;

namespace TDTS.Managers
{
    /// <summary>
    /// PLACEHOLDER until online multiplayer: takes the turns of every seat except
    /// LocalPlayer with simple random choices, so a full Day and Night keep moving.
    /// GameManager adds this automatically when "Bots Play Other Seats" is ticked.
    /// </summary>
    public class BotController : MonoBehaviour
    {
        [SerializeField] private float thinkSeconds = 1.2f;
        [Range(0f, 1f)]
        [SerializeField] private float acceptTradeChance = 0.6f;

        private GameManager gm;
        private int handledTurn = -1;

        private void Start()
        {
            gm = GameManager.Instance;
            if (gm == null) return;

            gm.OnTurnChanged += HandleTurnChanged;
            gm.OnTradeOffered += HandleTradeOffered;
            HandleTurnChanged(); // the first turn may have started before we subscribed
        }

        private void OnDestroy()
        {
            if (gm == null) return;
            gm.OnTurnChanged -= HandleTurnChanged;
            gm.OnTradeOffered -= HandleTradeOffered;
        }

        private void HandleTurnChanged()
        {
            PlayerData active = gm.ActivePlayer;
            if (active == null || active == gm.LocalPlayer || handledTurn == gm.TurnNumber) return;

            handledTurn = gm.TurnNumber;
            StartCoroutine(TakeTurn(active, gm.TurnNumber));
        }

        private IEnumerator TakeTurn(PlayerData bot, int turn)
        {
            while (gm.TurnNumber == turn)
            {
                yield return new WaitForSeconds(thinkSeconds);
                if (gm.TurnNumber != turn) yield break;

                if (!TryOneAction(bot))
                {
                    gm.PassTurn(bot); // nothing worked, so don't hold up the table
                    yield break;
                }
            }
        }

        private bool TryOneAction(PlayerData bot)
        {
            if (gm.CurrentState == GameState.DayPhase)
            {
                int morning = bot.hand.FindIndex(c => c.HasMorningFront);
                float roll = Random.value;

                if (morning >= 0 && roll < 0.25f) return gm.PlayCard(bot, morning);
                if (gm.DeckCount > 0 && bot.hand.Count < GameManager.MaxHandSize && roll < 0.85f) return gm.DrawCard(bot);
                return gm.PassTurn(bot);
            }

            // Real Night actions (peek, steal, swap) come in Phase 4
            if (gm.CurrentState == GameState.NightPhase) return gm.SkipAction(bot);

            return false;
        }

        private void HandleTradeOffered()
        {
            GameManager.TradeOffer trade = gm.PendingTrade;
            if (trade == null || trade.to == gm.LocalPlayer) return;
            StartCoroutine(AnswerTrade(trade));
        }

        private IEnumerator AnswerTrade(GameManager.TradeOffer trade)
        {
            yield return new WaitForSeconds(thinkSeconds);
            if (gm.PendingTrade != trade) yield break;

            bool accept = Random.value < acceptTradeChance && trade.to.hand.Count > 0;
            gm.RespondToTrade(accept, accept ? Random.Range(0, trade.to.hand.Count) : -1);
        }
    }
}
