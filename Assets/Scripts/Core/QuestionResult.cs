namespace SeaOfMemories
{
    public enum QuestionStatus { Accepted, NotOwned, AlreadyAsked, LimitReached }
    public enum EndingType { None, Bad, True }

    // 拒否結果も含め、その時点の数値を返す。UIや会話の進行は担当しない。
    public readonly struct QuestionResult
    {
        public readonly QuestionStatus status;
        public readonly int delta;
        public readonly int comboAfter;
        public readonly int agitationAfter;
        public readonly int questionsAfter;
        public readonly bool isFinalQuestion;
        public readonly EndingType ending;

        public QuestionResult(QuestionStatus status, int delta, int combo, int agitation, int questions,
            bool final, EndingType ending)
        {
            this.status = status;
            this.delta = delta;
            comboAfter = combo;
            agitationAfter = agitation;
            questionsAfter = questions;
            isFinalQuestion = final;
            this.ending = ending;
        }
    }
}
