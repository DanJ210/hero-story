namespace HeroStory.Core.Enums;

public sealed class ArtworkPolicyException : InvalidOperationException
{
    public ArtworkPolicyException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}