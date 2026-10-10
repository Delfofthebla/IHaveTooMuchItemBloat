using System;
using RoR2;

namespace IHaveTooMuchItemBloat;

internal static class StageRng
{
    public static Xoroshiro128Plus WishRolls { get; private set; }

    public static Xoroshiro128Plus ShrineSeeds { get; private set; }

    public static void Install() => Hooks.Install(typeof(Run), nameof(Run.GenerateStageRNG), [], DeriveAfterGame);

    private static void DeriveAfterGame(Action<Run> orig, Run self)
    {
        orig(self);

        Failsafe.Run("deriving the stage's wish RNG", () => DeriveFrom(self.stageRng));
    }

    private static void DeriveFrom(Xoroshiro128Plus gameStageRng)
    {
        // Copying leaves the game's stageRng unconsumed; Jump moves the copy to a stream that never overlaps the game's draws.
        var stageRng = new Xoroshiro128Plus(gameStageRng);
        stageRng.Jump();

        WishRolls = new Xoroshiro128Plus(stageRng.nextUlong);
        ShrineSeeds = new Xoroshiro128Plus(stageRng.nextUlong);
    }
}
