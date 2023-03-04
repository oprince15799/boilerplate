using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Extensions.ViewRenderer
{
    public interface IViewRenderer
    {
        Task<string> RenderToHtmlAsync<TModel>(string name, TModel model, CancellationToken cancellationToken = default);

        Task<string> RenderToTextAsync<TModel>(string name, TModel model, CancellationToken cancellationToken = default);
    }
}
