#nullable disable

using Boilerplate.Server.Extensions.Anonymous;

namespace Boilerplate.Server.Extensions.Anonymous
{
    public class AnonymousFeature : IAnonymousFeature
    {
        public string AnonymousId { get; set; }
    }
}