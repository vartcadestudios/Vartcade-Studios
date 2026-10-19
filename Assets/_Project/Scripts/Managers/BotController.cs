using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TDTS.Data;

namespace TDTS.Managers
{
    /// <summary>
    /// Takes the turns of every seat no person is sitting in (offline: everyone but you;
    /// online: the empty seats) with simple random choices, so a whole game keeps moving.
    /// Runs only where GameManager runs the rules: offline, or on the online host.
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
            if (active == null || gm.IsHuman(active) || handledTurn == gm.TurnNumber) return;

            handledTurn = gm.TurnNumber;
            StartCoroutine(TakeTurn(active, gm.TurnNumber));
        }

        private IEnumerator TakeTurn(PlayerData bot, int turn)
        {
            while (gm.TurnNumber == turn)
            {
                yield return new WaitForSeconds(GameSettings.BotThinkSeconds(thinkSeconds));
                if (gm.TurnNumber != turn) yield break;
                if (gm.PendingTrade != null) continue; // wait for the answer

                bool acted = gm.CurrentState == GameState.DayPhase ? DayAction(bot) : NightAction(bot);
                if (!acted && gm.TurnNumber == turn)
                {
                    gm.PassTurn(bot); // nothing worked, so don't hold up the table
                    if (gm.TurnNumber == turn) yield break; // couldn't even pass; leave it
                }
            }
        }

        private bool DayAction(PlayerData bot)
        {
            float roll = Random.value;
            bool tradingOnly = gm.CurrentDayRule == DayRule.TradingOnly;

            // Never offer a person a trade with eyes closed: they couldn't see it
            var partners = Others(bot).Where(p => p.hand.Count > 0 && (!gm.EyesClosed || !gm.IsHuman(p))).ToList();
            if (bot.hand.Count > 0 && partners.Count > 0 && (tradingOnly || roll < 0.15f))
                return gm.OfferTrade(bot, Random.Range(0, bot.hand.Count), Pick(partners));

            if (tradingOnly) return gm.PassTurn(bot);

            int morning = bot.hand.FindIndex(c => c.HasMorningFront);
            if (morning >= 0 && roll < 0.40f)
                return gm.PlayCard(bot, morning, Pick(Others(bot)));

            if (gm.DeckCount > 0 && bot.hand.Count < GameManager.MaxHandSize && roll < 0.90f)
                return gm.DrawCard(bot);

            return gm.PassTurn(bot);
        }

        private bool NightAction(PlayerData bot)
        {
            // First action: always peek at a card it hasn't looked at yet (R4)
            if (gm.PeeksThisTurn == 0 && bot.hand.Count > 0)
            {
                int unseen = bot.hand.FindIndex(c => !c.peeked);
                return gm.PeekCard(bot, unseen >= 0 ? unseen : Random.Range(0, bot.hand.Count));
            }

            float roll = Random.value;
            var victims = Others(bot).Where(p => p.hand.Count > 0).ToList();

            int nightCard = bot.hand.FindIndex(c => c.HasNightBack && c.peeked);
            if (nightCard >= 0 && victims.Count > 0 && roll < 0.5f)
                return gm.PlayCard(bot, nightCard, Pick(victims));

            if (victims.Count > 0 && bot.hand.Count < GameManager.MaxHandSize && roll < 0.65f)
            {
                PlayerData victim = Pick(victims);
                return gm.StealFrom(bot, victim, Random.Range(0, victim.hand.Count));
            }

            // Swap away a card it knows is Trash
            int trash = bot.hand.FindIndex(c => c.peeked && c.back == BackType.Trash);
            if (trash >= 0 && victims.Count > 0 && roll < 0.85f)
                return gm.SwapWith(bot, trash, Pick(victims));

            return gm.PassTurn(bot);
        }

        private List<PlayerData> Others(PlayerData bot) => gm.Players.Where(p => p != bot).ToList();

        private static PlayerData Pick(List<PlayerData> list) => list.Count == 0 ? null : list[Random.Range(0, list.Count)];

        private void HandleTradeOffered()
        {
            GameManager.TradeOffer trade = gm.PendingTrade;
            if (trade == null || gm.IsHuman(trade.to)) return;
            StartCoroutine(AnswerTrade(trade));
        }

        private IEnumerator AnswerTrade(GameManager.TradeOffer trade)
        {
            yield return new WaitForSeconds(GameSettings.BotThinkSeconds(thinkSeconds));
            if (gm.PendingTrade != trade) yield break;

            bool accept = Random.value < acceptTradeChance && trade.to.hand.Count > 0;
            gm.RespondToTrade(accept, accept ? Random.Range(0, trade.to.hand.Count) : -1);
        }
    }
}
