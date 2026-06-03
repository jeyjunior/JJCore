using JJCore.Domain.Enums;

namespace JJCore.Domain.Messages
{
    /// <summary>
    /// Provides reusable default error messages for applications that reference JJCore.
    /// </summary>
    public static class ErrorMessages
    {
        /// <summary>
        /// Default message for unexpected server failures.
        /// </summary>
        public const string ServerFailure = "An internal error occurred while processing your request. Try again later.";

        /// <summary>
        /// Default message for invalid Google login tokens.
        /// </summary>
        public const string GoogleLoginFailure = "Invalid Google token.";

        /// <summary>
        /// Default message for a user not found.
        /// </summary>
        public const string UserNotFound = "User not found.";

        /// <summary>
        /// Default message for authorization failures.
        /// </summary>
        public const string UserHasNoPermission = "User does not have permission to perform this operation.";

        /// <summary>
        /// Default message for empty query results.
        /// </summary>
        public const string RecordNotFound = "No records found.";

        /// <summary>
        /// Default message for save failures.
        /// </summary>
        public const string SaveFailure = "Could not save the record.";

        /// <summary>
        /// Default message for insert failures.
        /// </summary>
        public const string AddFailure = "Could not add the record.";

        /// <summary>
        /// Default message for update failures.
        /// </summary>
        public const string UpdateFailure = "Could not update the record.";

        /// <summary>
        /// Default message for delete failures.
        /// </summary>
        public const string DeleteFailure = "Could not delete the record.";

        /// <summary>
        /// Default message for invalid identifiers.
        /// </summary>
        public const string InvalidId = "The informed identifier is invalid.";

        /// <summary>
        /// Default message for records that cannot be deleted because they are in use.
        /// </summary>
        public const string RecordInUse = "This record cannot be deleted because it is in use.";

        /// <summary>
        /// Default message for records that do not exist.
        /// </summary>
        public const string RecordDoesNotExist = "The informed record does not exist.";

        /// <summary>
        /// Default message for required account fields.
        /// </summary>
        public const string AccountRequired = "Account is required.";

        /// <summary>
        /// Default message for required category fields.
        /// </summary>
        public const string CategoryRequired = "Category is required.";

        /// <summary>
        /// Default message for required transaction code fields.
        /// </summary>
        public const string TransactionRequired = "Transaction code is required.";

        /// <summary>
        /// Builds a name validation error message.
        /// </summary>
        /// <param name="format">The expected text format.</param>
        /// <param name="minLength">The minimum accepted length.</param>
        /// <returns>A formatted validation message.</returns>
        public static string InvalidName(TextFormat format, int minLength = 0)
        {
            string formatRule = format switch
            {
                TextFormat.Letters => "use only letters",
                TextFormat.LettersAndNumbers => "use only letters and numbers",
                _ => "invalid format"
            };

            string lengthRule = minLength > 0
                ? $" and have at least {minLength} characters"
                : "";

            return $"The name is invalid. Please, {formatRule}{lengthRule}.";
        }

        /// <summary>
        /// Builds a required field validation message.
        /// </summary>
        /// <param name="field">The field name.</param>
        /// <returns>A formatted validation message.</returns>
        public static string RequiredField(string field)
        {
            return string.IsNullOrEmpty(field)
                ? "This field is required."
                : $"The field '{field}' is required.";
        }
    }
}
