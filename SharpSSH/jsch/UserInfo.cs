using System;

namespace SharpSsh.jsch
{
/*    public interface UserInfo
    {
        string Passphrase { get; }
        string Password { get; }
        bool promptPassword(string message);
        bool promptPassphrase(string message);
        bool promptYesNo(string message);
        void showMessage(string message);
    }
*/
    public abstract class UserInfo
    {
        protected string m_passwd;

        public virtual string Passphrase { get { return null; } }
        public virtual string Password { get { return m_passwd; } }

        /// <summary>
        /// Prompt the user for a password
        /// </summary>
        public virtual bool promptPassword(string message) { return false; }

        /// <summary>
        /// Prompt the user for a passphrase (passwd for the private key file)
        /// </summary>
        public virtual bool promptPassphrase(string message) { return true; }

        public virtual bool promptYesNo(string message) { return false; }
        public virtual void showMessage(string message) { }
    }
}
