using NUnit.Framework;
using UnityEngine;

public class SignatureClearComboTest
{
    [Test]
    public void GetRandomSweeperOption_ReturnsNull_WhenArrayIsNullOrEmpty()
    {
        var combo = new SignatureClearCombo();

        combo.sweepers = null;
        Assert.IsNull(combo.GetRandomSweeperOption());

        combo.sweepers = new SweeperOption[0];
        Assert.IsNull(combo.GetRandomSweeperOption());
    }

    [Test]
    public void GetRandomSweeperOption_ReturnsOnlyOption_WhenSingleOptionHasPositiveWeight()
    {
        var combo = new SignatureClearCombo();
        var opt = new SweeperOption { variantId = "single", weightPercent = 50 };
        combo.sweepers = new[] { opt };

        Assert.AreSame(opt, combo.GetRandomSweeperOption());
    }

    [Test]
    public void GetRandomSweeperOption_SkipsZeroWeightOption_WhenPositiveOptionExists()
    {
        var combo = new SignatureClearCombo();
        var zeroOpt = new SweeperOption { variantId = "zero", weightPercent = 0 };
        var posOpt = new SweeperOption { variantId = "pos", weightPercent = 30 };
        combo.sweepers = new[] { zeroOpt, posOpt };

        for (int i = 0; i < 50; i++)
        {
            var selected = combo.GetRandomSweeperOption();
            Assert.AreSame(posOpt, selected);
        }
    }

    [Test]
    public void GetRandomSweeperOption_FallsBackToNonNullOption_WhenAllWeightsAreZero()
    {
        var combo = new SignatureClearCombo();
        var zeroOpt1 = new SweeperOption { variantId = "zero1", weightPercent = 0 };
        var zeroOpt2 = new SweeperOption { variantId = "zero2", weightPercent = 0 };
        combo.sweepers = new[] { zeroOpt1, zeroOpt2 };

        var selected = combo.GetRandomSweeperOption();
        Assert.IsNotNull(selected);
        Assert.IsTrue(selected == zeroOpt1 || selected == zeroOpt2);
    }

    [Test]
    public void GetRandomSweeperOption_ApproximatesConfiguredRatio_WithDeterministicSeed()
    {
        var combo = new SignatureClearCombo();
        var optA = new SweeperOption { variantId = "optA", weightPercent = 20 };
        var optB = new SweeperOption { variantId = "optB", weightPercent = 80 };
        combo.sweepers = new[] { optA, optB };

        Random.State previousState = Random.state;
        try
        {
            Random.InitState(12345);
            int countA = 0;
            int totalIterations = 2000;

            for (int i = 0; i < totalIterations; i++)
            {
                var selected = combo.GetRandomSweeperOption();
                if (selected == optA)
                {
                    countA++;
                }
            }

            float ratioA = (float)countA / totalIterations;
            // 20% expected, tolerance of +/- 5% (0.15 to 0.25)
            Assert.AreEqual(0.20f, ratioA, 0.05f);
        }
        finally
        {
            Random.state = previousState;
        }
    }

    [Test]
    public void GetRandomSweeperOption_IgnoresNullEntries()
    {
        var combo = new SignatureClearCombo();

        combo.sweepers = new SweeperOption[] { null, null };
        Assert.IsNull(combo.GetRandomSweeperOption());

        var validOpt = new SweeperOption { variantId = "valid", weightPercent = 100 };
        combo.sweepers = new SweeperOption[] { null, validOpt, null };
        Assert.AreSame(validOpt, combo.GetRandomSweeperOption());
    }
}
