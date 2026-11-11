namespace ExtractorOLE.Exceptions
{
    // [ydk:error:extraction/invalid-password]. The detail is fixed and no constructor takes the
    // password, so the secret can never reach the message ([ydk:req:extraction/password-confidentiality]).
    public sealed class InvalidPasswordException : ExtractorOleException
    {
        public override string Code => "INVALID_PASSWORD";
        public override int StatusCode => 422;

        public InvalidPasswordException()
            : base("The supplied password does not unlock this encrypted file.")
        {
        }
    }
}
