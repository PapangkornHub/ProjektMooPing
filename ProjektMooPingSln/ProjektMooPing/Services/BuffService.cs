using ProjektMooPing.Models;

namespace ProjektMooPing.Services
{
    public static class BuffService
    {
        // ลัคกี้แห่งการเจรจา  – Location 2 – 25% โอกาส +1 ไม้ฟรีต่อการขาย
        public static bool HasNegotiationBuff(PlayerProfile p) => p.UnlockedLocationIds.Contains(2);

        // ลัคกี้แห่งความทรหด – Location 3 – ยกเว้นโทษขาดสต็อก
        public static bool HasResilienceBuff(PlayerProfile p) => p.UnlockedLocationIds.Contains(3);

        // ลัคกี้แห่งความซุกซน – Location 4 – การันตีซื้อขั้นต่ำ 1 ไม้ต่อลูกค้า
        public static bool HasMischiefBuff(PlayerProfile p) => p.UnlockedLocationIds.Contains(4);

        // ลัคกี้แห่งความศิวิไลซ์ – Location 5 – ต้นทุนวัตถุดิบลด 10% ในการคำนวณ popularity
        public static bool HasCivilizationBuff(PlayerProfile p) => p.UnlockedLocationIds.Contains(5);

        // ลัคกี้แห่งความอดทน – Location 6 – stock +3 ต่อสูตรต่อวัน (สูตรที่มี stock > 0)
        public static bool HasEnduranceBuff(PlayerProfile p) => p.UnlockedLocationIds.Contains(6);

        // ลัคกี้แห่งการโหยหา – Location 7 – ลูกค้าประจำ +3 คนต่อวัน
        public static bool HasLongingBuff(PlayerProfile p) => p.UnlockedLocationIds.Contains(7);

        // ลัคกี้แห่งความหรรษา – Location 8 – Daily Rating ×1.2
        public static bool HasEuphoriaBuff(PlayerProfile p) => p.UnlockedLocationIds.Contains(8);

        // ลัคกี้แห่งการปล่อยวาง – Location 9 – Synergy penalty ไม่ส่งผล
        public static bool HasLettingGoBuff(PlayerProfile p) => p.UnlockedLocationIds.Contains(9);

        // ลัคกี้แห่งความสำเร็จ – Location 10 – Profit Score ×2
        public static bool HasSuccessBuff(PlayerProfile p) => p.UnlockedLocationIds.Contains(10);

        // --- คำอธิบาย Buff ตาม Location ID ---
        public static (string titleTh, string titleEn, string descTh, string descEn) GetBuffDescription(int locationId)
            => locationId switch
            {
                2 => ("ลัคกี้แห่งการเจรจา",
                      "Lucky of Negotiation",
                      "ลูกค้าทุกคนมีโอกาส 25% ที่จะซื้อเพิ่ม 1 ไม้ฟรี!",
                      "Every customer has a 25% chance to buy 1 extra skewer for free!"),

                3 => ("ลัคกี้แห่งความทรหด",
                      "Lucky of Resilience",
                      "หมดสต็อกระหว่างวันจะไม่โดนหักคะแนน Rating อีกต่อไป!",
                      "Running out of stock mid-day no longer deducts Rating points!"),

                4 => ("ลัคกี้แห่งความซุกซน",
                      "Lucky of Mischief",
                      "ลูกค้าทุกคนการันตีซื้ออย่างน้อย 1 ไม้เสมอ!",
                      "Every customer is guaranteed to buy at least 1 skewer!"),

                5 => ("ลัคกี้แห่งความศิวิไลซ์",
                      "Lucky of Civilization",
                      "ต้นทุนวัตถุดิบลดลง 10% ในการคำนวณความนิยม ขายแพงขึ้นได้โดยไม่เสีย Pop!",
                      "Ingredient costs are 10% lower in popularity calculation. Price higher without losing Pop!"),

                6 => ("ลัคกี้แห่งความอดทน",
                      "Lucky of Endurance",
                      "ทุกสูตรที่มีสต็อกอยู่จะได้รับ +3 ไม้ฟรีในตอนเริ่มต้นของทุกวัน!",
                      "Every recipe with stock receives +3 free skewers at the start of each day!"),

                7 => ("ลัคกี้แห่งการโหยหา",
                      "Lucky of Longing",
                      "ลูกค้าประจำ 3 คนจะแวะมาเพิ่มทุกวัน!",
                      "3 loyal customers visit every day on top of normal traffic!"),

                8 => ("ลัคกี้แห่งความหรรษา",
                      "Lucky of Euphoria",
                      "คะแนน Rating รายวันเพิ่มขึ้น ×1.2 เท่า!",
                      "Daily Rating score is multiplied by ×1.2!"),

                9 => ("ลัคกี้แห่งการปล่อยวาง",
                      "Lucky of Letting Go",
                      "การผสมส่วนผสมที่เข้ากันไม่ได้จะไม่โดนหักคะแนน Popularity อีกต่อไป!",
                      "Incompatible ingredient combinations no longer cause Popularity penalties!"),

                10 => ("ลัคกี้แห่งความสำเร็จ",
                       "Lucky of Success",
                       "คะแนน Profit ในการคำนวณ Rating รายวันเพิ่มขึ้น ×2 เท่า!",
                       "Profit Score in daily Rating calculation is doubled ×2!"),

                _ => (string.Empty, string.Empty, string.Empty, string.Empty)
            };
    }
}
