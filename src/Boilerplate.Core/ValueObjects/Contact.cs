using Boilerplate.Core.Helpers;
using Boilerplate.Core.Helpers.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.ValueObjects
{
    public class Contact : ValueObject
    {
        public Contact(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) ArgumentException.ThrowIfNullOrEmpty(value);

            if (ValidationHelper.IsEmail(value) && ValidationHelper.TryParseEmail(value, out var email))
            {
                Value = email.Address;
                Type = ContactType.EmailAddress;
            }
            else if (ValidationHelper.IsPhoneNumber(value) && ValidationHelper.TryParsePhoneNumber(value, out var phoneNumber))
            {
                Value = phoneNumber.RawInput;
                Type = ContactType.PhoneNumber;
            }
            else throw new InvalidOperationException("Email or phone number is not valid.");
        }

        public string Value { get; set; }

        public ContactType Type { get; set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }

    public enum ContactType
    {
        EmailAddress,
        PhoneNumber
    }
}
