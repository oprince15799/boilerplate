using Boilerplate.Core.Helpers;
using FluentValidation.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Exceptions
{
    [Serializable]
    public class ProblemException : InvalidOperationException
    {
        public IDictionary<string, string[]> Errors { get; private set; }

        public string Title { get; private set; }

        public int StatusCode { get; set; } 

        public ProblemException(string title, IDictionary<string, string[]> errors)
        {
            Title = title;
            Errors = errors;
            StatusCode = 400;
        }

        public ProblemException(string title, int statusCode) 
            : this(title, new Dictionary<string, string[]>())
        {
            StatusCode = statusCode;
        }

        public ProblemException(IDictionary<string, string[]> errors) 
            : this("One or more validation errors occurred.", errors)
        {
        }

        public ProblemException(IDictionary<LambdaExpression, string[]> errors) 
            : this(BuldErrors(errors))
        {
        }

        public ProblemException((string PropertyName, string Message) error)
            : this(new Dictionary<string, string[]> { { error.PropertyName, new string[] { error.Message } } }) { }

        public ProblemException(string title, (string PropertyName, string Message) error)
            : this(title, new Dictionary<string, string[]> { { error.PropertyName, new string[] { error.Message } } }) { }

        public ProblemException((LambdaExpression PropertyName, string Message) error)
            : this(BuldErrors(new[] { error })) {}

        public ProblemException(string title, (LambdaExpression PropertyName, string Message) error)
            : this(title, BuldErrors(new[] { error })) { }

        public ProblemException(IEnumerable<ValidationFailure> errors) : this(BuldErrors(errors)) { }

        public ProblemException(string title, IEnumerable<ValidationFailure> errors) : this(title, BuldErrors(errors)) { }

        private static IDictionary<string, string[]> BuldErrors(IDictionary<LambdaExpression, string[]> errors)
        {
            return errors.ToDictionary(_ => ExpressionHelper.GetName(_.Key), _ => _.Value);
        }

        private static IDictionary<string, string[]> BuldErrors(IEnumerable<(LambdaExpression PropertyName, string Message)> errors)
        {
            return errors.GroupBy(_ => ExpressionHelper.GetName(_.PropertyName).SubstringAfter(".")).ToDictionary(_ => _.Key, _ => _.Select(_ => _.Message).ToArray());
        }

        private static IDictionary<string, string[]> BuldErrors(IEnumerable<ValidationFailure> errors)
        {
            return errors.GroupBy(_ => _.PropertyName).ToDictionary(_ => _.Key, _ => _.Select(_ => _.ErrorMessage).ToArray());
        }

        public ProblemException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Title = "One or more validation errors occurred.";
            Errors = info.GetValue("errors", typeof(IDictionary<string, string[]>)) as IDictionary<string, string[]> ?? new Dictionary<string, string[]>();
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException("info");

            info.AddValue("errors", Errors);
            base.GetObjectData(info, context);
        }

        public override string ToString()
        {
            //var errorMessages = errors.SelectMany(pair => pair.Value.Select(value => new Tuple<string, string>(pair.Key, value)))
            //    .Select((name, message) => $"{Environment.NewLine} -- {name}: {message}")
            //      .ToList();

            //return "Validation failed: " + string.Join(string.Empty, errorMessages);
            return base.ToString();
        }
    }
}
