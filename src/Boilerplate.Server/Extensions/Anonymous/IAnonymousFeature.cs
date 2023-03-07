#nullable disable

using Boilerplate.Server.Extensions.Anonymous;

namespace Boilerplate.Server.Extensions.Anonymous
{
    public interface IAnonymousFeature
    {
        string AnonymousId { get; set; }
    }
}