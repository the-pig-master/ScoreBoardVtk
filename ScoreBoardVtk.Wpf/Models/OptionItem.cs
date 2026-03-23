namespace ScoreBoardVtk.Wpf.Models;

public sealed record OptionItem<T>(T Value, string Label)
{
    public override string ToString()
    {
        return Label;
    }
}
