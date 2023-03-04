using Boilerplate.Core.Helpers;
using FluentValidation.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Exceptions
{
    [Serializable]
    public class ValidationException : InvalidOperationException
    {
        public IDictionary<string, string[]> Errors { get; private set; }

        public string Title { get; private set; }

        public ValidationException(string title, IDictionary<string, string[]> errors)
        {
            Title = title;
            Errors = errors;
        }

        public ValidationException(string title) 
            : this(title, new Dictionary<string, string[]>())
        {
        }

        public ValidationException(IDictionary<string, string[]> errors) 
            : this("One or more validation errors occurred.", errors)
        {
        }

        public ValidationException(IDictionary<LambdaExpression, string[]> errors) 
            : this(BuldErrors(errors))
        {
        }

        public ValidationException((string PropertyName, string Message) error)
            : this(new Dictionary<string, string[]> { { error.PropertyName, new string[] { error.Message } } }) { }

        public ValidationException(string title, (string PropertyName, string Message) error)
            : this(title, new Dictionary<string, string[]> { { error.PropertyName, new string[] { error.Message } } }) { }

        public ValidationException((LambdaExpression PropertyName, string Message) error)
            : this(BuldErrors(new[] { error })) {}

        public ValidationException(string title, (LambdaExpression PropertyName, string Message) error)
            : this(title, BuldErrors(new[] { error })) { }

        public ValidationException(IEnumerable<ValidationFailure> errors) : this(BuldErrors(errors)) { }

        public ValidationException(string title, IEnumerable<ValidationFailure> errors) : this(title, BuldErrors(errors)) { }

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

        public ValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
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
