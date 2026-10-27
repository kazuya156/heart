using TMPro;
using UnityEngine;

namespace SeaOfMemories
{
    // TMPの実際の行組みからページを取得する。本文を切り取らず文字インデックスを保持する。
    public sealed class PagedText
    {
        private TMP_Text text;
        private int[] starts = new int[0];
        private int[] ends = new int[0];
        private float visibleCharacters;
        public int PageNumber { get; private set; }
        public int PageCount => ends.Length;
        public bool HasNextPage => PageNumber > 0 && PageNumber < PageCount;
        public bool IsPageRevealed => text == null || PageNumber == 0
            || text.maxVisibleCharacters >= ends[PageNumber - 1];

        public void Show(TMP_Text target, string value, bool revealImmediately = false)
        {
            Clear();
            text = target;
            text.text = value;
            text.overflowMode = TextOverflowModes.Page;
            text.pageToDisplay = 1;
            text.maxVisibleCharacters = int.MaxValue;
            // 非表示パネルを準備してから表示する呼び出しにも対応する。
            text.ForceMeshUpdate(true);
            TMP_TextInfo info = text.textInfo;
            int count = Mathf.Max(1, info.pageCount);
            starts = new int[count];
            ends = new int[count];
            for (int i = 0; i < count; i++)
            {
                if (info.characterCount == 0) continue;
                starts[i] = Mathf.Clamp(info.pageInfo[i].firstCharacterIndex, 0, info.characterCount);
                ends[i] = i == count - 1 ? info.characterCount
                    : Mathf.Clamp(info.pageInfo[i].lastCharacterIndex + 1, starts[i], info.characterCount);
            }
            SelectPage(1, revealImmediately);
        }

        public void Tick(float deltaTime, float charactersPerSecond)
        {
            if (IsPageRevealed) return;
            visibleCharacters = Mathf.Min(ends[PageNumber - 1],
                visibleCharacters + Mathf.Max(1f, charactersPerSecond) * Mathf.Max(0f, deltaTime));
            text.maxVisibleCharacters = Mathf.FloorToInt(visibleCharacters);
        }

        public void RevealPage()
        {
            if (text == null || PageNumber == 0) return;
            visibleCharacters = ends[PageNumber - 1];
            text.maxVisibleCharacters = ends[PageNumber - 1];
        }

        public bool TryNextPage(bool revealImmediately = false)
        {
            if (!IsPageRevealed || !HasNextPage) return false;
            SelectPage(PageNumber + 1, revealImmediately);
            return true;
        }

        private void SelectPage(int number, bool revealImmediately)
        {
            PageNumber = number;
            text.pageToDisplay = number;
            visibleCharacters = revealImmediately ? ends[number - 1] : starts[number - 1];
            text.maxVisibleCharacters = Mathf.FloorToInt(visibleCharacters);
        }

        public void Clear()
        {
            text = null;
            starts = new int[0];
            ends = new int[0];
            visibleCharacters = 0f;
            PageNumber = 0;
        }
    }
}
