namespace ExtractorOLE.Exceptions
{
    // [ydk:error:extraction/password-protected]
    public sealed class PasswordProtectedException : ExtractorOleException
    {
        public override string Code => "PASSWORD_PROTECTED";
        public override int StatusCode => 422;

        public PasswordProtectedException()
            : base("File is password-protected/encrypted and cannot be extracted without the password.")
        {
        }
    }
}
