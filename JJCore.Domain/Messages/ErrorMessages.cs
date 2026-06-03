using JJCore.Domain.Enums;

namespace JJCore.Domain.Messages
{
    public static class ErrorMessages
    {
        public const string ServerFailure = "An internal error occurred while processing your request. Try again later.";
        public const string GoogleLoginFailure = "Invalid Google token.";
        public const string UserNotFound = "User not found.";
        public const string UserHasNoPermission = "User does not have permission to perform this operation.";
        public const string RecordNotFound = "No records found.";
        public const string SaveFailure = "Could not save the record.";
        public const string AddFailure = "Could not add the record.";
        public const string UpdateFailure = "Could not update the record.";
        public const string DeleteFailure = "Could not delete the record.";
        public const string InvalidId = "The informed identifier is invalid.";
        public const string RecordInUse = "This record cannot be deleted because it is in use.";
        public const string RecordDoesNotExist = "The informed record does not exist.";
        public const string AccountRequired = "Account is required.";
        public const string CategoryRequired = "Category is required.";
        public const string TransactionRequired = "Transaction code is required.";

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

        public static string RequiredField(string field)
        {
            return string.IsNullOrEmpty(field)
                ? "This field is required."
                : $"The field '{field}' is required.";
        }
    }
}
