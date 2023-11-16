namespace Immutable.Passport.Model
{
    public class CreateTransferResponseV1
    {
        public string sent_signature { get; }

        public string receiver { get; }

        public string status { get; }

        public long time { get; }

        public long transfer_id { get; }
    }
}
