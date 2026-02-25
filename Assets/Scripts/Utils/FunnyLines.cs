using UnityEngine;

/// <summary>
/// Central repository of all funny in-game lines.
/// Call FunnyLines.Get(LineType) to get a random line.
/// </summary>
public static class FunnyLines
{
    public enum LineType
    {
        PooDiscovery, EggHatch, BattleWin, BattleLoss,
        PooDryOut, DistrictCapture, DistrictLoss,
        LevelUp, BoosterActivate, TrashCollect
    }

    private static readonly string[][] _lines =
    {
        // PooDiscovery
        new[] {
            "Holy dung, what a beauty!",
            "My nose never lies. Fresh delivery, boys!",
            "That smell... that's the smell of opportunity.",
            "Jackpot. This one's a 10/10 on the stench scale.",
            "Nature's gift. Unwrapped and ready.",
            "They left this for US. How thoughtful.",
            "I've seen poos. This is a POO.",
        },
        // EggHatch
        new[] {
            "Welcome to the swarm, little stinker!",
            "Another one! The clan grows stronger.",
            "Born sticky, born ready.",
            "Fresh wings, fresh ambitions.",
            "The poo giveth, and the poo provideth.",
            "Another fly to carry on our smelly legacy.",
        },
        // BattleWin
        new[] {
            "That's what you get for buzzing on our turf!",
            "Nobody out-stinks the Fly Lifers!",
            "Wiped the floor with them. Or the poo. Same thing.",
            "Pack up, boys. This block is OURS.",
            "They called us flies. We showed them we're WARRIORS.",
        },
        // BattleLoss
        new[] {
            "Retreat! We'll be back with more poo power!",
            "A tactical withdrawal. Very tactical.",
            "This isn't over. We know where you breed.",
            "Today we lose. Tomorrow we lay eggs.",
            "Fine. You can have it. FOR NOW.",
        },
        // PooDryOut
        new[] {
            "She's gone dry... a moment of silence.",
            "The poo hath left us. As all great things must.",
            "Nothing lasts forever. Not even poo.",
            "Dust to dust. Dung to... nothing.",
            "Lost a good one today.",
        },
        // DistrictCapture
        new[] {
            "This whole block smells like us now!",
            "From alley to alley, it's ALL FLY LIFE territory.",
            "The city doesn't know it yet, but WE run this district.",
            "Planted our stink flag. It's official.",
            "Six poos and a dream. Now it's our dream.",
        },
        // DistrictLoss
        new[] {
            "They took our district. Every last poo.",
            "Betrayed by our own fermentation.",
            "Unacceptable. Assemble the soldiers.",
            "They want a poo war? They've GOT a poo war.",
        },
        // LevelUp
        new[] {
            "The clan evolves! Sharper wings, stronger stench!",
            "Level up! We're getting dangerously good at this.",
            "Power level increasing. Deodorant companies fear us.",
            "New abilities unlocked. Old enemies: be afraid.",
        },
        // BoosterActivate
        new[] {
            "ACTIVATED. Maximum stink protocol engaged.",
            "Booster online. Let the chaos begin.",
            "Oh, they're not ready for this.",
            "Science has been applied to our filth.",
        },
        // TrashCollect
        new[] {
            "Treasure in the trash! Classic.",
            "One man's garbage is a fly's goldmine.",
            "They threw this away. Their loss, our gain.",
            "The bins are generous today.",
        },
    };

    public static string Get(LineType type)
    {
        var arr = _lines[(int)type];
        return arr[Random.Range(0, arr.Length)];
    }
}
