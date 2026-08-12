namespace JameJafari.Infrastructure.Security;

public interface IAppPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash, out bool needsRehash);
}
