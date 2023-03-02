﻿using Boilerplate.Core.Utilities;
using FluentValidation;
using FluentValidation.Results;
using Humanizer;
using PhoneNumbers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Boilerplate.Core.Utilities
{
    public static class ValidationHelper
    {
        public static bool IsEmail(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Trim().Length == 0) return false;

            return !Regex.IsMatch(value.ToLowerInvariant(), "^[-+0-9() ]+$");
        }

        public static bool IsPhoneNumber(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Trim().Length == 0) return false;

            return Regex.IsMatch(value.ToLowerInvariant(), "^[-+0-9() ]+$");
        }

        public static bool TryParseEmail(string value, [NotNullWhen(true)] out MailAddress? email)
        {
            try
            {
                email = ParseEmail(value);
                return true;
            }
            catch (FormatException)
            {
                email = null;
                return false;
            }
        }

        public static bool TryParsePhoneNumber(string value, [NotNullWhen(true)] out PhoneNumber? phoneNumber)
        {
            try
            {
                phoneNumber = ParsePhoneNumber(value);
                return true;
            }
            catch (FormatException)
            {
                phoneNumber = null;
                return false;
            }
        }

        public static MailAddress ParseEmail(string value)
        {
            ArgumentException.ThrowIfNullOrEmpty(value?.Trim(), nameof(value));

            if (!value.EndsWith("."))
            {
                var emailAddress = new MailAddress(value);
                if (emailAddress.Address == value)
                {
                    return emailAddress;
                }
            }

            throw new FormatException($"Input '{value}' was not recognized as a valid MailAddress.");
        }

        public static PhoneNumber ParsePhoneNumber(string value)
        {
            ArgumentException.ThrowIfNullOrEmpty(value?.Trim(), nameof(value));

            var phoneNumberHelper = PhoneNumberUtil.GetInstance();
            var phoneNumber = phoneNumberHelper.ParseAndKeepRawInput(value, null);

            if (phoneNumberHelper.IsValidNumber(phoneNumber) && phoneNumber.RawInput == value)
            {
                return phoneNumber;
            }

            throw new FormatException($"Input '{value}' was not recognized as a valid phone number.");
        }
    }

    public static class ValidationExtensions
    {
        public static IRuleBuilder<T, string> Username<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            ruleBuilder.Custom((value, context) =>
            {
                var propertyName = context.PropertyName;
                var propertyNamePrefix = !propertyName.StartsWith(nameof(Username)) ? propertyName.TrimEnd(nameof(Username)) : string.Empty;

                if (ValidationHelper.IsEmail(value))
                {
                    propertyName = (propertyNamePrefix + "EmailAddress").Humanize();

                    if (!ValidationHelper.TryParseEmail(value, out var _))
                        context.AddFailure($"'{propertyName}' is not valid.");
                }
                else if (ValidationHelper.IsPhoneNumber(value))
                {
                    propertyName = (propertyNamePrefix + "PhoneNumber").Humanize();

                    if (!ValidationHelper.TryParsePhoneNumber(value, out var _))
                        context.AddFailure($"'{propertyName}' is not valid.");
                }
                else throw new InvalidOperationException();
            });

            return ruleBuilder;
        }

        // How can I create strong passwords with FluentValidation?
        // source: https://stackoverflow.com/questions/63864594/how-can-i-create-strong-passwords-with-fluentvalidation
        public static IRuleBuilder<T, string> Password<T>(this IRuleBuilder<T, string> ruleBuilder, int minimumLength = 6)
        {
            var options = ruleBuilder
                .MinimumLength(minimumLength)
                .Matches("[A-Z]").WithMessage("'{PropertyName}' must contain at least 1 upper case.")
                .Matches("[a-z]").WithMessage("'{PropertyName}' must contain at least 1 lower case.")
                .Matches("[0-9]").WithMessage("'{PropertyName}' must contain at least 1 digit.")
                .Matches("[^a-zA-Z0-9]").WithMessage("'{PropertyName}' must contain at least 1 special character.");

            return options;
        }

        public static IDictionary<string, string[]> ToDictionary(this IEnumerable<ValidationFailure> errors)
        {
            return errors
              .GroupBy(x => x.PropertyName)
              .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage).ToArray()
              );
        }
    }
}
