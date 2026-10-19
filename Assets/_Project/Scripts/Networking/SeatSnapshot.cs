using Unity.Netcode;
using TDTS.Data;
using TDTS.Managers;

namespace TDTS.Networking
{
    /// <summary>
    /// One card in your own hand, as the host sends it to you. The front is always there;
    /// the back only once you have peeked at it (spec V8), so a client never learns a back early.
    /// </summary>
    public struct CardSnapshot : INetworkSerializable
    {
        public FrontType front;
        public SupplyKind frontSupply;
        public MorningActionType morningAction;
        public int keyDesignIndex;

        public bool peeked;
        public BackType back;
        public SupplyKind backSupply;
        public NightActionType nightAction;
        public int batteries;

        public static CardSnapshot From(CardData card)
        {
            var snap = new CardSnapshot
            {
                front = card.front,
                frontSupply = card.frontSupply,
                morningAction = card.morningAction,
                keyDesignIndex = card.keyDesignIndex,
                peeked = card.peeked,
            };
            if (card.peeked)
            {
                snap.back = card.back;
                snap.backSupply = card.backSupply;
                snap.nightAction = card.nightAction;
                snap.batteries = card.batteries;
            }
            return snap;
        }

        public CardData ToCard() => new CardData
        {
            front = front,
            frontSupply = frontSupply,
            morningAction = morningAction,
            keyDesignIndex = keyDesignIndex,
            peeked = peeked,
            back = back,
            backSupply = backSupply,
            nightAction = nightAction,
            batteries = batteries,
        };

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref front);
            serializer.SerializeValue(ref frontSupply);
            serializer.SerializeValue(ref morningAction);
            serializer.SerializeValue(ref keyDesignIndex);
            serializer.SerializeValue(ref peeked);
            serializer.SerializeValue(ref back);
            serializer.SerializeValue(ref backSupply);
            serializer.SerializeValue(ref nightAction);
            serializer.SerializeValue(ref batteries);
        }
    }

    /// <summary>
    /// Everything one seat may know about the table, sent by the host to that seat's device only.
    /// Other players appear as a name and a card count; their cards never leave the host.
    /// Seats are indexes into the turn order.
    /// </summary>
    public struct SeatSnapshot : INetworkSerializable
    {
        public int yourSeat;

        public GameState state;
        public int day;
        public int sundownDay;
        public int bunkerCapacity;
        public int deckCount;
        public bool limitActions;
        public int actionsLeft;
        public int peeksThisTurn;
        public int activeSeat;   // -1 between phases
        public int turnNumber;
        public DayRule dayRule;
        public int rondo;        // -1 = no Rondo today, else a RondoResult
        public bool eyesClosed;

        public string[] names;
        public int[] handCounts;
        public bool[] seated;          // Sundown results
        public string[] resultReasons;

        public CardSnapshot[] hand;    // your own hand only

        public int tradeFrom;          // -1 = no trade waiting
        public int tradeTo;
        public float tradeSecondsLeft;

        public string[] log;           // already filtered for this seat (spec C1)
        public int peekIndex;          // -1, or the card you just peeked at

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref yourSeat);
            serializer.SerializeValue(ref state);
            serializer.SerializeValue(ref day);
            serializer.SerializeValue(ref sundownDay);
            serializer.SerializeValue(ref bunkerCapacity);
            serializer.SerializeValue(ref deckCount);
            serializer.SerializeValue(ref limitActions);
            serializer.SerializeValue(ref actionsLeft);
            serializer.SerializeValue(ref peeksThisTurn);
            serializer.SerializeValue(ref activeSeat);
            serializer.SerializeValue(ref turnNumber);
            serializer.SerializeValue(ref dayRule);
            serializer.SerializeValue(ref rondo);
            serializer.SerializeValue(ref eyesClosed);

            SerializeStrings(serializer, ref names);
            SerializeArray(serializer, ref handCounts);
            SerializeArray(serializer, ref seated);
            SerializeStrings(serializer, ref resultReasons);
            SerializeCards(serializer, ref hand);

            serializer.SerializeValue(ref tradeFrom);
            serializer.SerializeValue(ref tradeTo);
            serializer.SerializeValue(ref tradeSecondsLeft);

            SerializeStrings(serializer, ref log);
            serializer.SerializeValue(ref peekIndex);
        }

        private static void SerializeStrings<T>(BufferSerializer<T> serializer, ref string[] values) where T : IReaderWriter
        {
            int count = values?.Length ?? 0;
            serializer.SerializeValue(ref count);
            if (serializer.IsReader) values = new string[count];
            for (int i = 0; i < count; i++)
            {
                string s = values[i] ?? "";
                serializer.SerializeValue(ref s);
                values[i] = s;
            }
        }

        private static void SerializeArray<T>(BufferSerializer<T> serializer, ref int[] values) where T : IReaderWriter
        {
            int count = values?.Length ?? 0;
            serializer.SerializeValue(ref count);
            if (serializer.IsReader) values = new int[count];
            for (int i = 0; i < count; i++) serializer.SerializeValue(ref values[i]);
        }

        private static void SerializeArray<T>(BufferSerializer<T> serializer, ref bool[] values) where T : IReaderWriter
        {
            int count = values?.Length ?? 0;
            serializer.SerializeValue(ref count);
            if (serializer.IsReader) values = new bool[count];
            for (int i = 0; i < count; i++) serializer.SerializeValue(ref values[i]);
        }

        private static void SerializeCards<T>(BufferSerializer<T> serializer, ref CardSnapshot[] cards) where T : IReaderWriter
        {
            int count = cards?.Length ?? 0;
            serializer.SerializeValue(ref count);
            if (serializer.IsReader) cards = new CardSnapshot[count];
            for (int i = 0; i < count; i++) serializer.SerializeValue(ref cards[i]);
        }
    }
}
