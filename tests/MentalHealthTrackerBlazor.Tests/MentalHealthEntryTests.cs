using System;
using MentalHealthTrackerBlazor.Data;
using Xunit;

namespace MentalHealthTrackerBlazor.Tests;

public class MentalHealthEntryTests
{
    private static MentalHealthEntry Sample() => new()
    {
        EntryId = 7,
        UserId = 42,
        Date = new DateTime(2026, 1, 15),
        Mood = 4,
        Notes = "slept badly",
        Triggers = "deadline",
    };

    [Fact]
    public void Clone_ReturnsANewInstance()
    {
        var original = Sample();
        var copy = original.Clone();

        Assert.NotSame(original, copy);
        Assert.IsType<MentalHealthEntry>(copy);
    }

    [Fact]
    public void Clone_CopiesEveryField()
    {
        var original = Sample();
        var copy = (MentalHealthEntry)original.Clone();

        Assert.Equal(original.EntryId, copy.EntryId);
        Assert.Equal(original.UserId, copy.UserId);
        Assert.Equal(original.Date, copy.Date);
        Assert.Equal(original.Mood, copy.Mood);
        Assert.Equal(original.Notes, copy.Notes);
        Assert.Equal(original.Triggers, copy.Triggers);
    }

    [Fact]
    public void Clone_IsIndependentOfTheOriginal()
    {
        // Inline editing works on a clone so that Cancel can discard the change;
        // editing the copy must never mutate the row it came from.
        var original = Sample();
        var copy = (MentalHealthEntry)original.Clone();

        copy.Mood = 1;
        copy.Notes = "edited";

        Assert.Equal(4, original.Mood);
        Assert.Equal("slept badly", original.Notes);
    }
}
